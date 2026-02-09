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
        Task<RecruitmentCampaignResponseDto> CreateAsync(CreateRecruitmentCampaignDto dto);
        Task<RecruitmentCampaignResponseDto?> UpdateAsync(int campaignId, UpdateRecruitmentCampaignDto dto);
        Task<bool> DeleteAsync(int campaignId);
    }
}
