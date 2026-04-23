using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UNIC.DataAccess.Repositories.Interface;

namespace UNIC.DataAccess.Repositories.Implementation
{
    public class DepartmentRepository : IDepartmentRepository
    {
        UnicContext _context;
        public DepartmentRepository(UnicContext context) { 
            _context = context;
        }

        public async Task<Department> CreateAsync(Department department)
        {
            await _context.Departments.AddAsync(department);
            await _context.SaveChangesAsync();
            return department;
        }

        public async Task<bool> DeleteAsync(int departmentId)
        {
            try
            {
                var department = await GetByIdAsync(departmentId);
                if (department == null)
                    return false;

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<Department>> GetAllAsync()
        {
            return await _context.Departments
                .Include(d => d.Club)
                .Include(d => d.ManagerRole)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Department>> GetByClubIdAsync(int clubId)
        {
            return await _context.Departments
                .Include(d => d.Club)
                .Include(d => d.ManagerRole)
                .Where(d => d.ClubId == clubId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<Department?> GetByIdAsync(int departmentId)
        {
            return await _context.Departments
                .Include(d => d.Club)
                .Include(d => d.ManagerRole)
                .FirstOrDefaultAsync(m => m.DepartmentId == departmentId);
        }

        public async Task<bool> UpdateAsync(Department department)
        {
            try
            {
                department.UpdatedAt = DateTime.UtcNow;
                _context.Departments.Update(department);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExistsAsync(int departmentId)
        {
            return await _context.Departments
                .AnyAsync(d => d.DepartmentId == departmentId);
        }

        public async Task<bool> DepartmentNameExistsInClubAsync(string departmentName, int clubId, int? excludeDepartmentId = null)
        {
            var query = _context.Departments
                .Where(d => d.DepartmentName == departmentName && d.ClubId == clubId);

            if (excludeDepartmentId.HasValue)
            {
                query = query.Where(d => d.DepartmentId != excludeDepartmentId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<Department?> GetByManagerRoleIdAsync(int managerRoleId)
        {
            return await _context.Departments
                .Include(d => d.ClubRoles)
                .FirstOrDefaultAsync(d => d.ManagerRoleId == managerRoleId);
        }

        public async Task<IEnumerable<UserClubRole>> GetMembersWithRolesByDepartmentAsync(
            int clubId, int departmentId)
        {
            return await _context.UserClubRoleDepartments
                .Where(ud => ud.DepartmentId == departmentId
                          && ud.ClubMember.ClubId == clubId)
                .Include(ud => ud.ClubMember)
                    .ThenInclude(ucr => ucr.User)
                .Include(ud => ud.ClubMember)
                    .ThenInclude(ucr => ucr.RoleAssignments).ThenInclude(ra => ra.ClubRole)
                .Select(ud => ud.ClubMember)
                .ToListAsync();
        }
        public async Task<UserClubRoleDepartment> AddMemberTodepartment(UserClubRoleDepartment departMember)
        {
            await _context.UserClubRoleDepartments.AddAsync(departMember);
            await _context.SaveChangesAsync();
            return departMember;
        }
        public async Task<UserClubRoleDepartment> RemoveMemberFromDepartment(UserClubRoleDepartment departMember)
        {
            _context.UserClubRoleDepartments.Remove(departMember);
            await _context.SaveChangesAsync();
            return departMember;
        }

        public async Task<IEnumerable<UserClubRole>> GetClubMembersNotInDepartmentAsync(int clubId, int departmentId)
        {
            var memberIdsInDepartment = await _context.UserClubRoleDepartments
                .Where(ud => ud.DepartmentId == departmentId)
                .Select(ud => ud.ClubMemberId)
                .ToListAsync();

            return await _context.UserClubRoles
                .Where(ucr => ucr.ClubId == clubId && !memberIdsInDepartment.Contains(ucr.ClubMemberId))
                .Include(ucr => ucr.User)
                .Include(ucr => ucr.RoleAssignments).ThenInclude(ra => ra.ClubRole)
                .ToListAsync();
        }

        public async Task<IEnumerable<Department>> GetDepartmentsJoinedByMemberAsync(int clubId, int clubMemberId)
        {
            return await _context.UserClubRoleDepartments
                .Where(ud => ud.ClubMemberId == clubMemberId && ud.Department.ClubId == clubId)
                .Include(ud => ud.Department)
                    .ThenInclude(d => d.ManagerRole)
                .Select(ud => ud.Department)
                .ToListAsync();
        }

        public async Task<IEnumerable<Department>> GetDepartmentsNotJoinedByMemberAsync(int clubId, int clubMemberId)
        {
            var joinedDepartmentIds = await _context.UserClubRoleDepartments
                .Where(ud => ud.ClubMemberId == clubMemberId)
                .Select(ud => ud.DepartmentId)
                .ToListAsync();

            return await _context.Departments
                .Where(d => d.ClubId == clubId && !joinedDepartmentIds.Contains(d.DepartmentId))
                .Include(d => d.ManagerRole)
                .ToListAsync();
        }
    }
}
