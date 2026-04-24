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
                DepartmentRoles   = ucr.RoleAssignments
                    .Where(ra => ra.ClubRole != null && ra.ClubRole.DepartmentId == departmentId)
                    .Select(ra => new DepartmentMemberRoleDto
                    {
                        ClubRoleId  = ra.ClubRole!.ClubRoleId,
                        RoleName    = ra.ClubRole.RoleName,
                        Description = ra.ClubRole.Description,
                        Level       = ra.ClubRole.Level
                    })
                    .ToList()
            });
        }
        public async Task<UserClubRoleDepartment> AddMemberToDepartment(int clubId, int clubMemberId, int departmentId)
        {
            var clubMember = await _clubMemberRepository.GetMemberByIdAsync(clubMemberId);
            if (clubMember == null || clubMember.ClubId != clubId)
                throw new KeyNotFoundException("Member not found in this club!");
            var member = new UserClubRoleDepartment
            {
                ClubMemberId = clubMemberId,
                DepartmentId = departmentId
            };
            return await _departmentRepository.AddMemberTodepartment(member);
        }

        public async Task<UserClubRoleDepartment> RemoveMemberFromDepartment(int clubId, int clubMemberId, int departmentId)
        {
            var clubMember = await _clubMemberRepository.GetMemberByIdAsync(clubMemberId);
            if (clubMember == null || clubMember.ClubId != clubId)
                throw new KeyNotFoundException("Member not found in this club!");

            var departmentRoles = clubMember.RoleAssignments
                .Where(ra => ra.ClubRole != null && ra.ClubRole.DepartmentId == departmentId)
                .Select(ra => ra.ClubRoleId)
                .ToList();

            foreach (var roleId in departmentRoles)
            {
                await _clubRoleRepository.RemoveMemberRoleAsync(clubMemberId, roleId);
            }

            var memberDept = clubMember.MemberDepartments?.FirstOrDefault(d => d.DepartmentId == departmentId);
            if (memberDept == null)
            {
                memberDept = new UserClubRoleDepartment
                {
                    ClubMemberId = clubMemberId,
                    DepartmentId = departmentId
                };
            }
            return await _departmentRepository.RemoveMemberFromDepartment(memberDept);
        }

        public async Task<IEnumerable<DepartmentMemberDto>?> GetClubMembersNotInDepartmentAsync(int clubId, int departmentId)
        {
            var department = await _departmentRepository.GetByIdAsync(departmentId);
            if (department == null || department.ClubId != clubId) return null;

            var members = await _departmentRepository.GetClubMembersNotInDepartmentAsync(clubId, departmentId);
            return members.Select(ucr => new DepartmentMemberDto
            {
                ClubMemberId   = ucr.ClubMemberId,
                UserId         = ucr.UserId,
                FullName       = ucr.User?.FullName ?? string.Empty,
                Email          = ucr.User?.Email ?? string.Empty,
                Avatar         = ucr.User?.Avatar,
                StudentId      = ucr.User?.StudentId,
                Status         = ucr.Status,
                JoinDate       = ucr.JoinDate,
                DepartmentRoles = new List<DepartmentMemberRoleDto>()
            });
        }

        public async Task<IEnumerable<DepartmentResponseDto>?> GetDepartmentsJoinedByMemberAsync(int clubId, int clubMemberId)
        {
            var clubMember = await _clubMemberRepository.GetMemberByIdAsync(clubMemberId);
            if (clubMember == null || clubMember.ClubId != clubId) return null;

            var departments = await _departmentRepository.GetDepartmentsJoinedByMemberAsync(clubId, clubMemberId);
            return departments.Select(MapToDto);
        }

        public async Task<IEnumerable<DepartmentResponseDto>?> GetDepartmentsNotJoinedByMemberAsync(int clubId, int clubMemberId)
        {
            var clubMember = await _clubMemberRepository.GetMemberByIdAsync(clubMemberId);
            if (clubMember == null || clubMember.ClubId != clubId) return null;

            var departments = await _departmentRepository.GetDepartmentsNotJoinedByMemberAsync(clubId, clubMemberId);
            return departments.Select(MapToDto);
        }
    }
}
