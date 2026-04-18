using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Implementation;
using DataAccess.Repositories.Interface;
using DocumentFormat.OpenXml.Bibliography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.DataAccess.Models;
using UNIC.DataAccess.Repositories.Interface;

namespace BusinessLogic.Services.Implementation
{
    public class ClubRoleService : IClubRoleService
    {
        private readonly IClubRoleRepository _repository;
        private readonly IDepartmentRepository _departmentRepository;

        public ClubRoleService(
            IClubRoleRepository repository,
            IDepartmentRepository departmentRepository)
        {
            _repository = repository;
            _departmentRepository = departmentRepository;
        }

        public async Task<ClubRoleResponseDto?> GetByIdAsync(int clubRoleId, int clubId)
        {
            var clubRole = await _repository.GetByIdAsync(clubRoleId,clubId);
            return clubRole == null ? null : MapToResponseDto(clubRole);
        }

        public async Task<IEnumerable<ClubRoleResponseDto>> GetAllAsync(int clubId)
        {
            var roles = await _repository.GetAllAsync(clubId);
            return roles.Select(MapToResponseDto);
        }

        public async Task<ClubRoleResponseDto> CreateAsync(CreateClubRoleDto dto, int clubId)
        {
            if (await _repository.RoleNameExistsAsync(dto.RoleName,clubId))
                throw new InvalidOperationException($"Role name '{dto.RoleName}' already exists.");

            var clubRole = new ClubRole
            {
                RoleName = dto.RoleName,
                Description = dto.Description,
                Level = dto.Level,
                ClubId = clubId,
                DepartmentId = dto.DepartmentId
            };

            var created = await _repository.CreateAsync(clubRole);

            // Gán policies nếu có
            if (dto.policies.Count > 0)
                await _repository.SetPoliciesAsync(created.ClubRoleId, dto.policies);

            // Load lại để có policies trong response
            var result = await _repository.GetByIdAsync(created.ClubRoleId,created.ClubId);
            return MapToResponseDto(result!);
        }

        public async Task<ClubRoleResponseDto?> UpdateAsync(int clubRoleId, UpdateClubRoleDto dto,int clubId)
        {
            var clubRole = await _repository.GetByIdAsync(clubRoleId,clubId);
            if (clubRole == null)
                return null;

            if (!string.IsNullOrEmpty(dto.RoleName) && dto.RoleName != clubRole.RoleName)
            {
                if (await _repository.RoleNameExistsAsync(dto.RoleName,clubId))
                    throw new InvalidOperationException($"Role name '{dto.RoleName}' already exists.");
                clubRole.RoleName = dto.RoleName;
            }

            if (dto.Description != null)
                clubRole.Description = dto.Description;

            if (dto.Level.HasValue)
                clubRole.Level = dto.Level.Value;

            if (dto.DepartmentId.HasValue)
                clubRole.DepartmentId = dto.DepartmentId;
            else
                clubRole.DepartmentId = null;

            await _repository.UpdateAsync(clubRole);

            // Cập nhật policies nếu được cung cấp
            if (dto.PolicyIds != null)
                await _repository.SetPoliciesAsync(clubRoleId, dto.PolicyIds);

            // Load lại để có policies mới trong response
            var result = await _repository.GetByIdAsync(clubRoleId, clubId);
            return MapToResponseDto(result!);
        }
        public async Task UpdatePoliciesAsync(int clubRoleId, List<int> policyIds)
        {
            await _repository.SetPoliciesAsync(clubRoleId, policyIds);
        }
        public async Task<bool> DeleteAsync(int clubRoleId)
        {
            // Check if this role is a department manager
            var department = await _departmentRepository.GetByManagerRoleIdAsync(clubRoleId);
            if (department != null)
            {
                // Step 1: Nullify the ManagerRoleId to break circular FK reference
                department.ManagerRoleId = null;
                await _departmentRepository.UpdateAsync(department);

                // Step 2: Delete all roles belonging to this department
                var departmentRoles = await _repository.GetByDepartmentIdAsync(department.DepartmentId);
                foreach (var role in departmentRoles)
                {
                    await _repository.DeleteAsync(role.ClubRoleId);
                }

                // Step 3: Delete the department itself
                await _departmentRepository.DeleteAsync(department.DepartmentId);
                return true;
            }

            // Normal delete for non-manager roles
            return await _repository.DeleteAsync(clubRoleId);
        }

        public async Task<ClubStructureResponseDto> GetClubStructureAsync(int clubId)
        {
            var allRoles = (await _repository.GetAllAsync(clubId)).ToList();
            var departments = (await _departmentRepository.GetByClubIdAsync(clubId)).ToList();

            // Roles with no department → standalone
            var standaloneRoles = allRoles
                .Where(r => r.DepartmentId == null)
                .Select(MapToStructureRoleDto)
                .ToList();

            // Group remaining roles by department
            var departmentDtos = departments.Select(dept =>
            {
                var deptRoles = allRoles.Where(r => r.DepartmentId == dept.DepartmentId).ToList();
                var managerRole = deptRoles.FirstOrDefault(r => r.ClubRoleId == dept.ManagerRoleId);
                var otherRoles  = deptRoles.Where(r => r.ClubRoleId != dept.ManagerRoleId).ToList();

                return new ClubStructureDepartmentDto
                {
                    DepartmentId   = dept.DepartmentId,
                    DepartmentName = dept.DepartmentName,
                    Description    = dept.Description,
                    Manager        = managerRole != null ? MapToStructureRoleDto(managerRole) : null,
                    Roles          = otherRoles.Select(MapToStructureRoleDto).ToList()
                };
            }).ToList();

            return new ClubStructureResponseDto
            {
                StandaloneRoles = standaloneRoles,
                Departments     = departmentDtos
            };
        }

        private static ClubStructureRoleDto MapToStructureRoleDto(ClubRole clubRole) =>
            new ClubStructureRoleDto
            {
                ClubRoleId   = clubRole.ClubRoleId,
                RoleName     = clubRole.RoleName,
                Description  = clubRole.Description,
                Level        = clubRole.Level,
                MemberCount  = clubRole.MemberAssignments?.Count ?? 0,
                Policies     = clubRole.ClubRolePolicies?
                    .Select(crp => new PolicyResponseDto
                    {
                        Id            = crp.Policy.Id,
                        Title         = crp.Policy.Title,
                        Description   = crp.Policy.Description,
                        PolicyGroupId = crp.Policy.PolicyGroupId
                    })
                    .ToList() ?? new()
            };

        private static ClubRoleResponseDto MapToResponseDto(ClubRole clubRole)
        {
            return new ClubRoleResponseDto
            {
                ClubRoleId = clubRole.ClubRoleId,
                RoleName = clubRole.RoleName,
                Description = clubRole.Description,
                Level = clubRole.Level,
                MemberCount = clubRole.MemberAssignments?.Count ?? 0,
                Policies = clubRole.ClubRolePolicies?
                    .Select(crp => new PolicyResponseDto
                    {
                        Id = crp.Policy.Id,
                        Title = crp.Policy.Title,
                        Description = crp.Policy.Description,
                        PolicyGroupId = crp.Policy.PolicyGroupId,
                    })
                    .ToList() ?? new(),
                clubId = clubRole.ClubId ?? 0,
                DepartmentId = clubRole.DepartmentId
            };
        }
        public async Task<bool> AssignRoleAsync(AssignClubRoleDto dto)
        {
            var exist = await _repository
                .GetUserClubRoleAsync(dto.UserId, dto.ClubId);

            if (exist != null)
            {
                await _repository.SetMemberRolesAsync(exist.ClubMemberId, dto.ClubRoleIds);
                foreach(var roleId in dto.ClubRoleIds)
                {
                    if (await _repository.CheckDepartementRole(roleId))
                    {
                        var clubRole = await _repository.GetByIdAsync(roleId, dto.ClubId);
                        var departMember = new UserClubRoleDepartment
                        {
                            ClubMemberId = exist.ClubMemberId,
                            DepartmentId = clubRole?.DepartmentId ?? 0,
                        };
                        await _departmentRepository.AddMemberTodepartment(departMember);
                    }
                }

                return true;
            }

            var member = new UserClubRole
            {
                UserId = dto.UserId,
                ClubId = dto.ClubId,
                JoinDate = DateTime.UtcNow,
                Status = "ACTIVE"
            };

            var assigned = await _repository.AddUserClubRoleAsync(member);
            if(assigned)
            {
                await _repository.SetMemberRolesAsync(member.ClubMemberId, dto.ClubRoleIds);
            }
            return assigned;
        }

        public Task<UserClubRole?> GetUserClubRoleAsync(Guid userId, int clubId)
        {
            return _repository.GetUserClubRoleAsync(userId, clubId);
        }
        public async Task<List<ClubResponseDto>> GetManagedClubsAsync(Guid userId)
        {
            var clubs = await _repository.GetManagedClubsAsync(userId);

            return clubs.Select(c => new ClubResponseDto
            {
                ClubId = c.ClubId,
                ClubName = c.ClubName,
                Description = c.Description
            }).ToList();
        }
    }
}
