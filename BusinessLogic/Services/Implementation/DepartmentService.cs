using BusinessLogic.DTOs;
using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UNIC.BusinessLogic.DTOs;
using UNIC.BusinessLogic.Services.Interface;
using UNIC.DataAccess.Repositories.Interface;

namespace UNIC.BusinessLogic.Services.Implementation
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IDepartmentRepository _departmentRepository;
        public DepartmentService(IDepartmentRepository departmentService)
        {
            _departmentRepository = departmentService;
        }

        private DepartmentResponseDto MapToDto(Department department)
        {
            return new DepartmentResponseDto
            {
                DepartmentId = department.DepartmentId,
                Name = department.DepartmentName,
                Description = department.Description
            };
        }

        public async Task<DepartmentResponseDto> CreateDepartmentAsync(CreateDepartmentDto request)
        {
            var department = new Department
            {
                DepartmentName = request.Name,
                Description = request.Description,
                ClubId = request.ClubId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var createdDepartment = await _departmentRepository.CreateAsync(department);
            return MapToDto(createdDepartment);
        }

        public async Task<bool> DeleteDepartmentAsync(int id)
        {
            return await _departmentRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<DepartmentResponseDto>> GetAllDepartmentsAsync()
        {
            var departments = await _departmentRepository.GetAllAsync();
            return departments.Select(MapToDto);
        }

        public async Task<DepartmentResponseDto?> GetDepartmentByIdAsync(int id)
        {
            var department = await _departmentRepository.GetByIdAsync(id);
            if (department == null)
            {
                return null;
            }
            return MapToDto(department);
        }

        public async Task<bool> UpdateDepartmentAsync(int id, DepartmentResponseDto department)
        {
            var existingDepartment = await _departmentRepository.GetByIdAsync(id);
            if (existingDepartment == null)
            {
                return false;
            }
            if (department.DepartmentId != existingDepartment.DepartmentId)
            {
                return false;
            }
            existingDepartment.DepartmentName = department.Name;
            existingDepartment.Description = department.Description;
            await _departmentRepository.UpdateAsync(existingDepartment);
            return true;
        }
    }
}
