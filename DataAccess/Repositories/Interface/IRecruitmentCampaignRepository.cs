using DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IRecruitmentCampaignRepository
    {
        Task<RecruitmentCampaign?> GetByIdAsync(int campaignId);
        Task<IEnumerable<RecruitmentCampaign>> GetAllAsync();
        Task<IEnumerable<RecruitmentCampaign>> GetByClubIdAsync(int clubId);
        Task<RecruitmentCampaign> CreateAsync(RecruitmentCampaign campaign);
        Task<bool> UpdateAsync(RecruitmentCampaign campaign);
        Task<bool> DeleteAsync(int campaignId);
        Task<bool> ExistsAsync(int campaignId);
        Task<int> BulkCloseExpiredAsync();
        Task<bool> HasOverlappingCampaignAsync(int clubId, DateTime startDate, DateTime endDate, int? excludeCampaignId = null);
    }
}
