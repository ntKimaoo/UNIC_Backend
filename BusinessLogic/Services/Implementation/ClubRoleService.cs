using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.DataAccess.Models;

namespace BusinessLogic.Services.Implementation
{
    public class ClubRoleService : IClubRoleService
    {
        private readonly IClubRoleRepository _repository;

        public ClubRoleService(IClubRoleRepository repository)
        {
            _repository = repository;
        }

        public async Task<ClubRoleResponseDto?> GetByIdAsync(int clubRoleId)
        {
            var clubRole = await _repository.GetByIdAsync(clubRoleId);
            return clubRole == null ? null : MapToResponseDto(clubRole);
        }

        public async Task<IEnumerable<ClubRoleResponseDto>> GetAllAsync()
        {
            var roles = await _repository.GetAllAsync();
            return roles.Select(MapToResponseDto);
        }

        public async Task<ClubRoleResponseDto> CreateAsync(CreateClubRoleDto dto)
        {
            if (await _repository.RoleNameExistsAsync(dto.RoleName))
                throw new InvalidOperationException($"Role name '{dto.RoleName}' already exists.");

            var clubRole = new ClubRole
            {
                RoleName = dto.RoleName,
                Description = dto.Description,
                Level = dto.Level,
                ClubId = dto.ClubId
            };

            var created = await _repository.CreateAsync(clubRole);

            // Gán policies nếu có
            if (dto.policies.Count > 0)
                await _repository.SetPoliciesAsync(created.ClubRoleId, dto.policies);

            // Load lại để có policies trong response
            var result = await _repository.GetByIdAsync(created.ClubRoleId);
            return MapToResponseDto(result!);
        }

        public async Task<ClubRoleResponseDto?> UpdateAsync(int clubRoleId, UpdateClubRoleDto dto)
        {
            var clubRole = await _repository.GetByIdAsync(clubRoleId);
            if (clubRole == null)
                return null;

            if (!string.IsNullOrEmpty(dto.RoleName) && dto.RoleName != clubRole.RoleName)
            {
                if (await _repository.RoleNameExistsAsync(dto.RoleName))
                    throw new InvalidOperationException($"Role name '{dto.RoleName}' already exists.");
                clubRole.RoleName = dto.RoleName;
            }

            if (dto.Description != null)
                clubRole.Description = dto.Description;

            if (dto.Level.HasValue)
                clubRole.Level = dto.Level.Value;

            await _repository.UpdateAsync(clubRole);

            // Cập nhật policies nếu được cung cấp
            if (dto.PolicyIds != null)
                await _repository.SetPoliciesAsync(clubRoleId, dto.PolicyIds);

            // Load lại để có policies mới trong response
            var result = await _repository.GetByIdAsync(clubRoleId);
            return MapToResponseDto(result!);
        }
        public async Task UpdatePoliciesAsync(int clubRoleId, List<int> policyIds)
        {
            await _repository.SetPoliciesAsync(clubRoleId, policyIds);
        }
        public async Task<bool> DeleteAsync(int clubRoleId)
        {
            return await _repository.DeleteAsync(clubRoleId);
        }

        private static ClubRoleResponseDto MapToResponseDto(ClubRole clubRole)
        {
            return new ClubRoleResponseDto
            {
                ClubRoleId = clubRole.ClubRoleId,
                RoleName = clubRole.RoleName,
                Description = clubRole.Description,
                Level = clubRole.Level,
                MemberCount = clubRole.ClubMembers?.Count ?? 0,
                Policies = clubRole.ClubRolePolicies?
                    .Select(crp => new PolicyResponseDto
                    {
                        Id = crp.Policy.Id,
                        Title = crp.Policy.Title,
                        Description = crp.Policy.Description,
                        PolicyGroupId=crp.Policy.PolicyGroupId,
                    })
                    .ToList() ?? new()
            };
        }
        public async Task<IEnumerable<Policy>> GetPoliciesByRoleAsync(int clubRoleId)
        {
                        return await _repository.GetPoliciesByRoleAsync(clubRoleId);
        }

        public async Task<IEnumerable<ClubRoleResponseDto>> GetRolesByClubIdAsync(int clubId)
        {
            var roles = await _repository.GetRolesByClubIdAsync(clubId);
            return roles.Select(MapToResponseDto);
        }
    }
}
