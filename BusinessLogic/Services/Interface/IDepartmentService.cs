using DataAccess.Models;
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

        /// <summary>
        /// Returns all members that belong to the department, with their club-wide role.
        /// Returns null when the department does not exist or belongs to a different club.
        /// </summary>
        Task<IEnumerable<DepartmentMemberDto>?> GetDepartmentMembersAsync(int clubId, int departmentId);
        Task<UserClubRoleDepartment> AddMemberToDepartment(int clubId, int clubMemberId, int departmentId);
        Task<UserClubRoleDepartment> RemoveMemberFromDepartment(int clubId, int clubMemberId, int departmentId);
        Task<IEnumerable<DepartmentMemberDto>?> GetClubMembersNotInDepartmentAsync(int clubId, int departmentId);
        Task<IEnumerable<DepartmentResponseDto>?> GetDepartmentsJoinedByMemberAsync(int clubId, int clubMemberId);
        Task<IEnumerable<DepartmentResponseDto>?> GetDepartmentsNotJoinedByMemberAsync(int clubId, int clubMemberId);
    }
}
