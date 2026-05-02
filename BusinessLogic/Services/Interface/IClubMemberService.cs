using BusinessLogic.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IClubMemberService
    {
        Task<IEnumerable<ClubMemberResponseDto>> GetMembersByClubAsync(int clubId);
        Task<PagedResultDto<ClubMemberResponseDto>> GetMembersByClubAsync(
            int clubId, int? pagination, int? page, string? filter, bool? ascending, string? sortBy);
        Task<ClubMemberResponseDto?> GetMemberByIdAsync(int clubMemberId);
        Task<ClubMemberResponseDto> AddUserToClubAsync(int clubId, AddUserToClubDto dto, Guid? assignedBy);
        Task<ClubMemberResponseDto?> UpdateMemberRoleAsync(int clubMemberId, UpdateMemberRoleDto dto);
        Task<ClubMemberResponseDto?> UpdateMemberStatusAsync(int clubMemberId, bool isActive);
        Task<bool> RemoveMemberAsync(int clubMemberId);
        Task<IEnumerable<ClubMemberResponseDto>> GetMyClubsAsync(Guid userId);
        Task<bool> IsMemberAsync(Guid userId, int clubId);
        Task<bool> IsMemberActiveAsync(Guid userId, int clubId);
        Task<int> CountMembersAsync(int clubId);
    }
}
