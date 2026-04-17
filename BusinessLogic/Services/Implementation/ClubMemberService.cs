using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementation
{
    public class ClubMemberService : IClubMemberService
    {
        private readonly IClubMemberRepository _memberRepository;
        private readonly IClubRepository _clubRepository;
        private readonly IUserRepository _userRepository;

        public ClubMemberService(
            IClubMemberRepository memberRepository,
            IClubRepository clubRepository,
            IUserRepository userRepository)
        {
            _memberRepository = memberRepository;
            _clubRepository = clubRepository;
            _userRepository = userRepository;
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
            return member == null ? null : MapToResponseDto(member);
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
                ClubRoleId = dto.ClubRoleId,
                JoinDate = DateTime.UtcNow,
                Status = "ACTIVE",
                AssignedBy = assignedBy
            };

            var created = await _memberRepository.AddMemberAsync(member);

            // Load lại để có navigation properties
            var result = await _memberRepository.GetMemberByIdAsync(created.ClubMemberId);
            return MapToResponseDto(result!);
        }

        public async Task<ClubMemberResponseDto?> UpdateMemberRoleAsync(int clubMemberId, UpdateMemberRoleDto dto)
        {
            var member = await _memberRepository.GetMemberByIdAsync(clubMemberId);
            if (member == null) return null;

            member.ClubRoleId = dto.ClubRoleId;
            await _memberRepository.UpdateMemberAsync(member);

            var result = await _memberRepository.GetMemberByIdAsync(clubMemberId);
            return MapToResponseDto(result!);
        }

        public async Task<bool> RemoveMemberAsync(int clubMemberId)
        {
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
                ClubRoleId = m.ClubRoleId,
                RoleName = m.ClubRole?.RoleName,
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
