using BusinessLogic.DTOs;
using DataAccess.Models;
using DataAccess.Repositories.Implementation;
using DataAccess.Repositories.Interface;
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
        private readonly IClubRepository _clubRepository;
        private readonly IClubRoleRepository _clubRoleRepository;
        private readonly IClubMemberRepository _clubMemberRepository;
        public DepartmentService(
            IDepartmentRepository departmentRepository,
            IClubRepository clubRepository,
            IClubRoleRepository clubRoleRepository,
            IClubMemberRepository clubMemberRepository)
        {
            _departmentRepository = departmentRepository;
            _clubRepository = clubRepository;
            _clubRoleRepository = clubRoleRepository;
            _clubMemberRepository = clubMemberRepository;
        }

        private DepartmentResponseDto MapToDto(Department department)
        {
            return new DepartmentResponseDto
            {
                DepartmentId = department.DepartmentId,
                ClubId = department.ClubId,
                Name = department.DepartmentName,
                Description = department.Description,
                ManagerRoleId = department.ManagerRoleId,
                ManagerRole = department.ManagerRole != null ? new DepartmentManagerRoleDto
                {
                    ClubRoleId = department.ManagerRole.ClubRoleId,
                    RoleName = department.ManagerRole.RoleName,
                    Description = department.ManagerRole.Description,
                    Level = department.ManagerRole.Level
                } : null,
                CreatedAt = department.CreatedAt,
                UpdatedAt = department.UpdatedAt
            };
        }

        public async Task<DepartmentResponseDto> CreateDepartmentAsync(int clubId, CreateDepartmentDto request)
        {
            // Verify club exists
            var clubExists = await _clubRepository.ExistsAsync(clubId);
            if (!clubExists)
            {
                throw new InvalidOperationException("Club not found");
            }

            // Check if department name already exists in this club
            var nameExists = await _departmentRepository.DepartmentNameExistsInClubAsync(request.Name, clubId);
            if (nameExists)
            {
                throw new InvalidOperationException("Department name already exists in this club");
            }

            // Step 1: Create the department (ManagerRoleId will be set after role creation)
            var department = new Department
            {
                DepartmentName = request.Name,
                Description = request.Description,
                ClubId = clubId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdDepartment = await _departmentRepository.CreateAsync(department);

            // Step 2: Automatically create the Department Manager role for this department
            var managerRole = new ClubRole
            {
                RoleName = $"{request.Name} 's Manager",
                Description = $"Manager role for {request.Name} department",
                ClubId = clubId,
                DepartmentId = createdDepartment.DepartmentId,
                Level = request.ManagerRoleLevel
            };

            var createdRole = await _clubRoleRepository.CreateAsync(managerRole);

            // Step 3: Link the manager role back to the department
            createdDepartment.ManagerRoleId = createdRole.ClubRoleId;
            createdDepartment.ManagerRole = createdRole;
            createdDepartment.UpdatedAt = DateTime.UtcNow;
            await _departmentRepository.UpdateAsync(createdDepartment);

            return MapToDto(createdDepartment);
        }

        public async Task<bool> DeleteDepartmentAsync(int clubId, int id)
        {
            var department = await _departmentRepository.GetByIdAsync(id);
            if (department == null || department.ClubId != clubId)
            {
                return false;
            }

            return await _departmentRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<DepartmentResponseDto>> GetAllDepartmentsAsync()
        {
            var departments = await _departmentRepository.GetAllAsync();
            return departments.Select(MapToDto);
        }

        public async Task<IEnumerable<DepartmentResponseDto>> GetDepartmentsByClubIdAsync(int clubId)
        {
            var departments = await _departmentRepository.GetByClubIdAsync(clubId);
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

        public async Task<DepartmentResponseDto?> UpdateDepartmentAsync(int clubId, int id, UpdateDepartmentDto request)
        {
            var existingDepartment = await _departmentRepository.GetByIdAsync(id);
            if (existingDepartment == null || existingDepartment.ClubId != clubId)
            {
                return null;
            }

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.Name))
            {
                // Check if new name already exists (excluding current department)
                var nameExists = await _departmentRepository.DepartmentNameExistsInClubAsync(
                    request.Name, clubId, id);
                if (nameExists)
                {
                    throw new InvalidOperationException("Department name already exists in this club");
                }
                existingDepartment.DepartmentName = request.Name;
            }

            if (request.Description != null)
            {
                existingDepartment.Description = request.Description;
            }

            if (request.ManagerRoleId.HasValue)
            {
                existingDepartment.ManagerRoleId = request.ManagerRoleId;
            }

            existingDepartment.UpdatedAt = DateTime.UtcNow;

            var updated = await _departmentRepository.UpdateAsync(existingDepartment);
            if (!updated)
            {
                return null;
            }

            return MapToDto(existingDepartment);
        }

        public async Task<IEnumerable<DepartmentMemberDto>?> GetDepartmentMembersAsync(
            int clubId, int departmentId)
        {
            // Verify the department belongs to this club
            var department = await _departmentRepository.GetByIdAsync(departmentId);
            if (department == null || department.ClubId != clubId)
                return null;

            var members = await _departmentRepository
                .GetMembersWithRolesByDepartmentAsync(clubId, departmentId);

            return members.Select(ucr => new DepartmentMemberDto
            {
                ClubMemberId = ucr.ClubMemberId,
                UserId       = ucr.UserId,
                FullName     = ucr.User?.FullName ?? string.Empty,
                Email        = ucr.User?.Email ?? string.Empty,
                Avatar       = ucr.User?.Avatar,
                StudentId    = ucr.User?.StudentId,
                Status       = ucr.Status,
                JoinDate     = ucr.JoinDate,
                DepartmentRole    = (ucr.ClubRole != null && ucr.ClubRole.DepartmentId == departmentId)
                    ? new DepartmentMemberRoleDto
                    {
                        ClubRoleId  = ucr.ClubRole.ClubRoleId,
                        RoleName    = ucr.ClubRole.RoleName,
                        Description = ucr.ClubRole.Description,
                        Level       = ucr.ClubRole.Level
                    }
                    : null
            });
        }
        public async Task<UserClubRoleDepartment> AddMemberTodepartment(int clubId,Guid userId,int departmentId)
        {
            var clubMember = await _clubMemberRepository.GetMemberAsync(userId, clubId);
            if (clubMember == null) throw new KeyNotFoundException("User not a club member!");
            var member = new UserClubRoleDepartment
            {
                ClubMemberId=clubMember.ClubMemberId,
                DepartmentId=departmentId
            };

            var created = await _departmentRepository.AddMemberTodepartment(member);
            return created;
        }
        public async Task<UserClubRoleDepartment> RemoveMemberFromDepartment(int clubId, Guid userId, int departmentId)
        {
            var clubMember = await _clubMemberRepository.GetMemberAsync(userId, clubId);
            if (clubMember == null) throw new KeyNotFoundException("User not a club member!");
            var member = new UserClubRoleDepartment
            {
                ClubMemberId = clubMember.ClubMemberId,
                DepartmentId = departmentId
            };
            var deleted= await _departmentRepository.RemoveMemberFromDepartment(member);
            return deleted;
        }
    }
}
