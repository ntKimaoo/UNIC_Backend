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
        Task<IEnumerable<DepartmentResponseDto>> GetAllDepartmentsAsync();
        Task<IEnumerable<DepartmentResponseDto>> GetDepartmentsByClubIdAsync(int clubId);
        Task<DepartmentResponseDto?> GetDepartmentByIdAsync(int id);
        Task<DepartmentResponseDto> CreateDepartmentAsync(int clubId, CreateDepartmentDto request);
        Task<DepartmentResponseDto?> UpdateDepartmentAsync(int clubId, int id, UpdateDepartmentDto request);
        Task<bool> DeleteDepartmentAsync(int clubId, int id);
    }
}
