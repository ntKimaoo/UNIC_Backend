using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UNIC.BusinessLogic.DTOs;

namespace UNIC.BusinessLogic.Services.Interface
{
    public interface IDepartmentService
    {
        Task<IEnumerable<DepartmentResponseDto>> GetAllDepartmentsAsync(int clubId);
        Task<DepartmentResponseDto?> GetDepartmentByIdAsync(int id, int clubId);
        Task<DepartmentResponseDto> CreateDepartmentAsync(CreateDepartmentDto request);
        Task<bool> UpdateDepartmentAsync(int id, DepartmentResponseDto department, int clubId);
        Task<bool> DeleteDepartmentAsync(int id, int clubId);
    }
}
