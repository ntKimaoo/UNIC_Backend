using BusinessLogic.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IRecruitmentCampaignService
    {
        Task<RecruitmentCampaignResponseDto?> GetByIdAsync(int campaignId);
        Task<IEnumerable<RecruitmentCampaignResponseDto>> GetAllAsync();
        Task<IEnumerable<RecruitmentCampaignResponseDto>> GetByClubIdAsync(int clubId);
        Task<PagedResultDto<RecruitmentCampaignResponseDto>> GetPagedAsync(
            int page, int pageSize, string? search, string? filterBy, bool ascending);
        Task<PagedResultDto<RecruitmentCampaignResponseDto>> GetPagedByClubIdAsync(
            int clubId, int page, int pageSize, string? search, string? filterBy, bool ascending);
        Task<RecruitmentCampaignResponseDto> CreateAsync(CreateRecruitmentCampaignDto dto);
        Task<RecruitmentCampaignResponseDto?> UpdateAsync(int campaignId, UpdateRecruitmentCampaignDto dto);
        Task<bool> DeleteAsync(int campaignId);
    }
}
