using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.DataAccess.Repositories.Interface;

namespace BusinessLogic.Services.Implementation
{
    public class ClubMemberService : IClubMemberService
    {
        private readonly IClubMemberRepository _memberRepository;
        private readonly IClubRepository _clubRepository;
        private readonly IUserRepository _userRepository;
        private readonly IClubRoleRepository _clubRoleRepo;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IPolicyRepository _policyRepository;

        public ClubMemberService(
            IClubMemberRepository memberRepository,
            IClubRepository clubRepository,
            IUserRepository userRepository,
            IClubRoleRepository clubRoleRepo,
            IDepartmentRepository departmentRepository,
            IPolicyRepository policyRepository)
        {
            _memberRepository = memberRepository;
            _clubRepository = clubRepository;
            _userRepository = userRepository;
            _clubRoleRepo = clubRoleRepo;
            _departmentRepository = departmentRepository;
            _policyRepository = policyRepository;
        }

        public async Task<IEnumerable<ClubMemberResponseDto>> GetMembersByClubAsync(int clubId)
        {
            var members = await _memberRepository.GetMembersByClubIdAsync(clubId);
            return members.Select(MapToResponseDto);
        }

        public async Task<PagedResultDto<ClubMemberResponseDto>> GetMembersByClubAsync(
            int clubId, int? pagination, int? page, string? filter, bool? ascending, string? sortBy)
        {
            var (items, totalCount) = await _memberRepository.GetMembersByClubIdAsync(
                clubId, pagination, page, filter, ascending, sortBy);

            var dtos = items.Select(MapToResponseDto).ToList();
            var pageSize = pagination ?? totalCount;
            if (pageSize == 0) pageSize = 1;

            return new PagedResultDto<ClubMemberResponseDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = page ?? 1,
                PageSize = pagination ?? 0,
                TotalPages = pagination.HasValue && pagination.Value > 0 ? (int)Math.Ceiling((double)totalCount / pagination.Value) : 1,
                HasNextPage = pagination.HasValue && pagination.Value > 0 && (page ?? 1) * pagination.Value < totalCount,
                HasPreviousPage = pagination.HasValue && pagination.Value > 0 && (page ?? 1) > 1
            };
        }

        public async Task<ClubMemberResponseDto?> GetMemberByIdAsync(int clubMemberId)
        {
            var member = await _memberRepository.GetMemberByIdAsync(clubMemberId);
            if (member == null) return null;
            var member_response = MapToResponseDto(member);
            var member_role = await _clubRoleRepo.GetRolesOfMemberAsync(clubMemberId);
            member_response.Roles = member_role.Select(ra => new ClubRoleInfoDto
            {
                ClubRoleId = ra.ClubRoleId,
                RoleName = ra?.RoleName ?? "",
                Level = ra?.Level ?? 0,
            }).ToList() ?? new List<ClubRoleInfoDto>();
            return member_response;
        }

        public async Task<ClubMemberResponseDto> AddUserToClubAsync(int clubId, AddUserToClubDto dto, Guid? assignedBy)
        {
            var club = await _clubRepository.GetByIdAsync(clubId);
            if (club == null) throw new KeyNotFoundException($"Club with ID {clubId} not found.");

            var user = await _userRepository.GetByIdAsync(dto.UserId);
            if (user == null) throw new KeyNotFoundException($"User with ID {dto.UserId} not found.");

            if (await _memberRepository.IsMemberAsync(dto.UserId, clubId))
                throw new InvalidOperationException("User is already a member of this club.");

            var member = new UserClubRole
            {
                UserId = dto.UserId,
                ClubId = clubId,
                JoinDate = DateTime.UtcNow,
                Status = "ACTIVE",
                AssignedBy = assignedBy
            };

            var created = await _memberRepository.AddMemberAsync(member);
            if (dto.ClubRoleIds != null && dto.ClubRoleIds.Any())
            {
                await _clubRoleRepo.SetMemberRolesAsync(created.ClubMemberId, dto.ClubRoleIds);
            }

            var result = await _memberRepository.GetMemberByIdAsync(created.ClubMemberId);
            return MapToResponseDto(result!);
        }

        public async Task<ClubMemberResponseDto?> UpdateMemberRoleAsync(int clubMemberId, UpdateMemberRoleDto dto)
        {
            var member = await _memberRepository.GetMemberByIdAsync(clubMemberId);
            if (member == null) return null;
            
            var currentRoleIds = member.RoleAssignments.Select(ra => ra.ClubRoleId).ToHashSet();
            var newRoleIds = (dto.ClubRoleIds ?? new List<int>()).ToHashSet();
            var rolesToAdd = newRoleIds.Where(id => !currentRoleIds.Contains(id)).ToList();

            // Business checks chỉ cần chạy cho role mới thêm vào.
            foreach (var roleId in rolesToAdd)
            {
                var clubRole = await _clubRoleRepo.GetByIdAsync(roleId, member.ClubId);
                if (clubRole == null) continue;

                if (clubRole.Level == 0)
                {
                    if (await _memberRepository.HasClubManager(member.ClubId))
                        throw new Exception("Each club has only one Club Manager");
                }

                if (clubRole.DepartmentId.HasValue)
                {
                    var departmentId = clubRole.DepartmentId.Value;
                    var department = await _departmentRepository.GetByIdAsync(departmentId);

                    if (department != null && department.ManagerRoleId == clubRole.ClubRoleId)
                    {
                        var allMembers = await _memberRepository.GetMembersByClubIdAsync(member.ClubId);
                        var existingManager = allMembers.FirstOrDefault(m =>
                            m.RoleAssignments.Any(ra => ra.ClubRoleId == clubRole.ClubRoleId) &&
                            m.ClubMemberId != clubMemberId);

                        if (existingManager != null)
                            throw new Exception("1 department manager role chỉ có thể có ít hơn 2( chỉ có thể có 1 hoặc là không có)");
                    }

                    var isMemberInDept = member.MemberDepartments.Any(d => d.DepartmentId == departmentId);
                    if (!isMemberInDept)
                    {
                        await _departmentRepository.AddMemberTodepartment(new UserClubRoleDepartment
                        {
                            ClubMemberId = clubMemberId,
                            DepartmentId = departmentId
                        });
                    }
                }
            }

            await _clubRoleRepo.SetMemberRolesAsync(clubMemberId, newRoleIds);

            var result = await _memberRepository.GetMemberByIdAsync(clubMemberId);
            return MapToResponseDto(result!);
        }

        public async Task<ClubMemberResponseDto?> UpdateMemberStatusAsync(int clubMemberId, bool isActive)
        {
            var member = await _memberRepository.GetMemberByIdAsync(clubMemberId);
            if (member == null) return null;

            member.Status = isActive ? "ACTIVE" : "INACTIVE";
            var updated = await _memberRepository.UpdateMemberAsync(member);
            if (!updated) return null;

            var result = await _memberRepository.GetMemberByIdAsync(clubMemberId);
            return result == null ? null : MapToResponseDto(result);
        }

        public async Task<bool> RemoveMemberAsync(int clubMemberId)
        {
            await _clubRoleRepo.SetMemberRolesAsync(clubMemberId, new List<int>());
            return await _memberRepository.RemoveMemberAsync(clubMemberId);
        }

        public async Task<IEnumerable<ClubMemberResponseDto>> GetMyClubsAsync(Guid userId)
        {
            var memberships = await _memberRepository.GetClubsByUserIdAsync(userId);
            return memberships.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<UserClubDetailedInfoDto>> GetMyClubsDetailedAsync(Guid userId)
        {
            var memberships = await _memberRepository.GetClubsByUserIdAsync(userId);
            var systemRoles = await _policyRepository.GetUserRoleAsync(userId);
            var systemRole = systemRoles.FirstOrDefault() ?? "User";

            var result = new List<UserClubDetailedInfoDto>();

            foreach (var m in memberships)
            {
                var policies = await _policyRepository.GetPoliciesInClubAsync(userId, m.ClubId);
                var roles = m.RoleAssignments?
                    .Where(ra => ra.ClubRole != null)
                    .Select(ra => new ClubRoleInfoDto
                    {
                        ClubRoleId = ra.ClubRoleId,
                        RoleName = ra.ClubRole?.RoleName ?? "",
                        Level = ra.ClubRole?.Level ?? 99,
                        AssignedAt = ra.AssignedAt
                    }).ToList() ?? new List<ClubRoleInfoDto>();

                result.Add(new UserClubDetailedInfoDto
                {
                    ClubId = m.ClubId,
                    ClubName = m.Club?.ClubName ?? "",
                    GlobalRole = systemRole,
                    ClubRoles = roles,
                    Policies = policies.ToList()
                });
            }
            return result;
        }

        public async Task<bool> IsMemberAsync(Guid userId, int clubId)
        {
            return await _memberRepository.IsMemberAsync(userId, clubId);
        }

        private static ClubMemberResponseDto MapToResponseDto(UserClubRole m)
        {
            return new ClubMemberResponseDto
            {
                ClubMemberId = m.ClubMemberId,
                UserId = m.UserId,
                FullName = m.User?.FullName ?? "",
                Email = m.User?.Email ?? "",
                Avatar = m.User?.Avatar,
                StudentId = m.User?.StudentId,
                ClubId = m.ClubId,
                Roles = m.RoleAssignments?.Select(ra => new ClubRoleInfoDto
                {
                    ClubRoleId = ra.ClubRoleId,
                    RoleName = ra.ClubRole?.RoleName ?? "",
                    Level = ra.ClubRole?.Level ?? 0,
                    AssignedAt = ra.AssignedAt
                }).ToList() ?? new List<ClubRoleInfoDto>(),
                JoinDate = m.JoinDate,
                Status = m.Status,
                AssignedBy = m.AssignedBy
            };
        }

        public async Task<bool> IsMemberActiveAsync(Guid userId, int clubId)
        {
            return await _memberRepository.isMemberActive(userId, clubId);
        }

        public async Task<int> CountMembersAsync(int clubId)
        {
            return await _memberRepository.CountMembersAsync(clubId);
        }

        public async Task<bool> TransferClubAsync(int clubId, int newManagerMemberId, Guid? transferredBy)
        {
            // Validate new manager is a member of this club
            var newManager = await _memberRepository.GetMemberByIdAsync(newManagerMemberId);
            if (newManager == null || newManager.ClubId != clubId)
                throw new KeyNotFoundException("Thành viên không thuộc câu lạc bộ này.");

            // Find the Level 0 (Club Manager) role for the club
            var allRoles = await _clubRoleRepo.GetAllAsync(clubId);
            var managerRole = allRoles.FirstOrDefault(r => r.Level == 0);
            if (managerRole == null)
                throw new InvalidOperationException("Câu lạc bộ chưa có role Club Manager (Level 0).");

            // Prevent transferring to someone who is already the manager
            if (newManager.RoleAssignments.Any(ra => ra.ClubRole?.Level == 0))
                throw new InvalidOperationException("Thành viên này đã là Club Manager của câu lạc bộ.");

            // Find current Club Manager to strip the role from them
            var allMembers = await _memberRepository.GetMembersByClubIdAsync(clubId);
            var currentManager = allMembers.FirstOrDefault(m =>
                m.RoleAssignments.Any(ra => ra.ClubRole != null && ra.ClubRole.Level == 0));

            // Step 1: Remove Club Manager role from current manager, keep other roles
            if (currentManager != null)
            {
                var remainingRoles = currentManager.RoleAssignments
                    .Where(ra => ra.ClubRole?.Level != 0)
                    .Select(ra => ra.ClubRoleId)
                    .ToList();
                await _clubRoleRepo.SetMemberRolesAsync(currentManager.ClubMemberId, remainingRoles);
            }

            // Step 2: Add Club Manager role to new manager, keep their existing roles
            var newManagerRoles = newManager.RoleAssignments
                .Select(ra => ra.ClubRoleId)
                .ToList();
            if (!newManagerRoles.Contains(managerRole.ClubRoleId))
                newManagerRoles.Add(managerRole.ClubRoleId);
            await _clubRoleRepo.SetMemberRolesAsync(newManagerMemberId, newManagerRoles);

            return true;
        }
        public async Task<bool> IsUserInClubAsync(Guid userId, int clubId)
        {
            return await _memberRepository.IsMemberAsync(userId, clubId);
        }
    }
}
