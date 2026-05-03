using BusinessLogic.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IClubPostService
    {
        Task<ClubPostResponseDto?> GetByIdAsync(int postId);
        Task<IEnumerable<ClubPostResponseDto>> GetAllAsync();
        Task<IEnumerable<ClubPostResponseDto>> GetByClubIdAsync(int clubId);
        Task<IEnumerable<ClubPostResponseDto>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<ClubPostResponseDto>> GetByEventIdAsync(int eventId);
        Task<IEnumerable<ClubPostResponseDto>> GetByCampaignIdAsync(int campaignId);
        Task<ClubPostResponseDto> CreateAsync(CreateClubPostDto dto, IFormFile? imageFile);
        Task<ClubPostResponseDto?> UpdateAsync(int postId, UpdateClubPostDto dto, IFormFile? imageFile);
        Task<bool> DeleteAsync(int postId);
    }
}
