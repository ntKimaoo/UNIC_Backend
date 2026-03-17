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
        public async Task<IEnumerable<Department>> GetAllAsync(int clubId)
        {
            return await _context.Departments
                .Where(d => d.ClubId == clubId)
                .ToListAsync();
        }

        public async Task<Department?> GetByIdAsync(int departmentId, int clubId)
        {
            return await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == departmentId && d.ClubId == clubId);
        }

        public async Task<Department> CreateAsync(Department department)
        {
            department.CreatedAt = DateTime.UtcNow;
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return department;
        }

        public async Task<bool> UpdateAsync(Department department, int clubId)
        {
            var existing = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == department.DepartmentId && d.ClubId == clubId);

            if (existing == null) return false;

            existing.DepartmentName = department.DepartmentName;
            existing.Description = department.Description;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int departmentId, int clubId)
        {
            var existing = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == departmentId && d.ClubId == clubId);

            if (existing == null) return false;

            _context.Departments.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
