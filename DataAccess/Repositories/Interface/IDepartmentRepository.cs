using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNIC.DataAccess.Repositories.Interface
{
    public interface IDepartmentRepository
    {
        Task<IEnumerable<Department>> GetAllAsync(int clubId);
        Task<Department?> GetByIdAsync(int departmentId, int clubId);
        Task<Department> CreateAsync(Department department);
        Task<bool> UpdateAsync(Department department, int clubId);
        Task<bool> DeleteAsync(int departmentId, int clubId);
    }
}
