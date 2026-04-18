using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Implementation
{
    public class UserRepository : IUserRepository
    {
        private readonly UnicContext _context;

        public UserRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .Include(u => u.ClubMembers)
                    .ThenInclude(cm => cm.RoleAssignments).ThenInclude(ra => ra.ClubRole)
                .FirstOrDefaultAsync(m => m.Email == email);
        }

        public async Task<User?> GetByIdAsync(Guid userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == userId);
        }

        public async Task<User?> GetByStudentIdAsync(string studentId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(m => m.StudentId == studentId);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(m => m.Email == email);
        }

        public async Task<bool> StudentIdExistsAsync(string studentId)
        {
            if (string.IsNullOrEmpty(studentId))
                return false;

            return await _context.Users
                .AnyAsync(m => m.StudentId == studentId);
        }

        public async Task<User> CreateAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.UserRoles.AddAsync(new UserRole
            {
                UserId = user.UserId,
                RoleName = "User"
            });
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UpdateAsync(User user)
        {
            try
            {
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(Guid userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.Status = "Inactive";
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Where(u => u.Status == "Active")
                .ToListAsync();
        }

        public async Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.Users
                .Where(u => u.Status == "Active")
                .OrderByDescending(u => u.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        public async Task<IEnumerable<Club>> GetAllClubByUser(Guid userId)
        {
            bool isAdmin = await _context.UserRoles
                .AnyAsync(m => m.UserId == userId && m.RoleName == "Admin");
            if (isAdmin)
            {
                return await _context.Clubs.ToListAsync();
            }
            return await _context.UserClubRoles
                .Where(cm => cm.UserId == userId)
                .Select(cm => cm.Club)
                .ToListAsync();
        }

        public async Task<IEnumerable<(Department Department, ClubRole? DepartmentRole, int MemberCount)>>
            GetDepartmentsByUserAndClubAsync(Guid userId, int clubId)
        {
            bool isAdmin = await _context.UserRoles
               .AnyAsync(m => m.UserId == userId && (m.RoleName == "Admin"||m.RoleName == "Club Manager"));
            if (isAdmin)
            {
                var departments = await _context.Departments
                    .Where(d => d.ClubId == clubId)
                    .Include(d => d.ClubRoles)
                    .Select(d => new
                    {
                        Department = d,
                        MemberCount = _context.UserClubRoleDepartments
                            .Count(ud => ud.DepartmentId == d.DepartmentId)
                    })
                    .ToListAsync();

                return departments.Select(x => (x.Department, (ClubRole?)null, x.MemberCount));
            }

            // Find the member entry for this user in this club
            var member = await _context.UserClubRoles
                .FirstOrDefaultAsync(ucr => ucr.UserId == userId && ucr.ClubId == clubId);

            if (member == null)
                return Enumerable.Empty<(Department, ClubRole?, int)>();

            // Load all departments the member belongs to via the junction table,
            // including all ClubRoles that belong to each department + member count
            var rows = await _context.UserClubRoleDepartments
                .Where(ud => ud.ClubMemberId == member.ClubMemberId)
                .Include(ud => ud.Department)
                    .ThenInclude(d => d.ClubRoles)      // ← all roles in the department
                .Include(ud => ud.ClubMember)
                    .ThenInclude(ucr => ucr.RoleAssignments).ThenInclude(ra => ra.ClubRole)
                .Select(ud => new
                {
                    ud.Department,
                    DepartmentRole = ud.ClubMember.RoleAssignments
                        .Where(ra => ra.ClubRole != null && ra.ClubRole.DepartmentId == ud.DepartmentId)
                        .Select(ra => ra.ClubRole)
                        .FirstOrDefault(),
                    MemberCount = _context.UserClubRoleDepartments
                        .Count(x => x.DepartmentId == ud.DepartmentId)
                })
                .ToListAsync();

            return rows.Select(r => (r.Department, (ClubRole?)r.DepartmentRole, r.MemberCount));
        }
        public async Task<bool> isActiveAccount(Guid userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == userId);
            if (user == null)
                return false;
            return user.Status.ToLower() == "active";
        }
       

        public async Task<bool> AssignUserRole(Guid userId, string roleName)
        {
            var userRole = new UserRole
            {
                UserId = userId,
                RoleName = roleName,
                AssignedAt = DateTime.UtcNow
            };
            await _context.UserRoles.AddAsync(userRole);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<string> GetUserRole(Guid userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleName)
                .FirstOrDefaultAsync() ?? "User";
        }
    }
}