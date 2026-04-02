using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
                    .ThenInclude(ucr => ucr.ClubRole)
                .Select(ud => ud.ClubMember)
                .ToListAsync();
        }
    }
}
