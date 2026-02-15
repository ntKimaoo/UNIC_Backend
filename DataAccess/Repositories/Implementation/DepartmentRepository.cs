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
            return await _context.Departments.ToListAsync();
        }

        public async Task<Department?> GetByIdAsync(int departmentId)
        {
            return await _context.Departments
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
    }
}
