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

        public ClubMemberService(
            IClubMemberRepository memberRepository,
            IClubRepository clubRepository,
            IUserRepository userRepository,
            IClubRoleRepository clubRoleRepo,
            IDepartmentRepository departmentRepository)
        {
            _memberRepository = memberRepository;
            _clubRepository = clubRepository;
            _userRepository = userRepository;
            _clubRoleRepo = clubRoleRepo;
            _departmentRepository = departmentRepository;
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
            var pageSize = pagination ?? totalCount; // if no pagination, pageSize logic defaults to total
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
            var member_role= await _clubRoleRepo.GetRolesOfMemberAsync(clubMemberId);
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
            // Kiểm tra club có tồn tại không
            var club = await _clubRepository.GetByIdAsync(clubId);
            if (club == null)
                throw new KeyNotFoundException($"Club with ID {clubId} not found.");

            // Kiểm tra user có tồn tại không
            var user = await _userRepository.GetByIdAsync(dto.UserId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {dto.UserId} not found.");

            // Kiểm tra user đã là member chưa
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

            // Load lại để có navigation properties
            var result = await _memberRepository.GetMemberByIdAsync(created.ClubMemberId);
            return MapToResponseDto(result!);
        }

        public async Task<ClubMemberResponseDto?> UpdateMemberRoleAsync(int clubMemberId, UpdateMemberRoleDto dto)
        {
            var member = await _memberRepository.GetMemberByIdAsync(clubMemberId);
            if (member == null) return null;
            
            if (dto.ClubRoleIds == null || !dto.ClubRoleIds.Any())
            {
                await _clubRoleRepo.SetMemberRolesAsync(clubMemberId, new List<int>());
                var result1 = await _memberRepository.GetMemberByIdAsync(clubMemberId);
                return MapToResponseDto(result1!);
            }

            foreach (var roleId in dto.ClubRoleIds)
            {
                var clubRole = await _clubRoleRepo.GetByIdAsync(roleId, member.ClubId);
                if (clubRole != null)
                {
                    if (clubRole.Level == 0)
                    {
                        if (await _memberRepository.HasClubManager(member.ClubId))
                        {
                            var isCurrentManager = member.RoleAssignments.Any(ra => ra.ClubRole != null && ra.ClubRole.Level == 0);
                            if (!isCurrentManager)
                            {
                                throw new Exception("Each club has only one Club Manager");
                            }
                        }
                    }

                    if (clubRole.DepartmentId.HasValue)
                    {
                        var departmentId = clubRole.DepartmentId.Value;
                        var department = await _departmentRepository.GetByIdAsync(departmentId);

                        if (department != null && department.ManagerRoleId == clubRole.ClubRoleId)
                        {
                            var isCurrentDeptManager = member.RoleAssignments.Any(ra => ra.ClubRoleId == clubRole.ClubRoleId);
                            if (!isCurrentDeptManager)
                            {
                                var allMembers = await _memberRepository.GetMembersByClubIdAsync(member.ClubId);
                                var existingManager = allMembers.FirstOrDefault(m => 
                                    m.RoleAssignments.Any(ra => ra.ClubRole != null && ra.ClubRole.ClubRoleId == clubRole.ClubRoleId) &&
                                    m.ClubMemberId != clubMemberId);

                                if (existingManager != null)
                                {
                                    throw new Exception("1 department manager role chỉ có thể có ít hơn 2( chỉ có thể có 1 hoặc là không có)");
                                }
                            }
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
            }

            await _clubRoleRepo.SetMemberRolesAsync(clubMemberId, dto.ClubRoleIds);

            var result = await _memberRepository.GetMemberByIdAsync(clubMemberId);
            return MapToResponseDto(result!);
        }

        public async Task<bool> RemoveMemberAsync(int clubMemberId)
        {
            // Remove all role assignments of the member before removing the member
            await _clubRoleRepo.SetMemberRolesAsync(clubMemberId, new List<int>());
            return await _memberRepository.RemoveMemberAsync(clubMemberId);
        }

        public async Task<IEnumerable<ClubMemberResponseDto>> GetMyClubsAsync(Guid userId)
        {
            var memberships = await _memberRepository.GetClubsByUserIdAsync(userId);
            return memberships.Select(MapToResponseDto);
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
                AssignedBy = m.AssignedBy,
            };
        }

        public async Task<bool> IsMemberActiveAsync(Guid userId, int clubId)
        {
            return await _memberRepository.isMemberActive(userId, clubId);
        }
    }
}
