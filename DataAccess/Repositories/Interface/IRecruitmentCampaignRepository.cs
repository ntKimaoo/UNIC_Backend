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
        Task<(IEnumerable<RecruitmentCampaign> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, string? filterBy, bool ascending);
        Task<(IEnumerable<RecruitmentCampaign> Items, int TotalCount)> GetPagedByClubIdAsync(
            int clubId, int page, int pageSize, string? search, string? filterBy, bool ascending);
        Task<RecruitmentCampaign> CreateAsync(RecruitmentCampaign campaign);
        Task<bool> UpdateAsync(RecruitmentCampaign campaign);
        Task<bool> DeleteAsync(int campaignId);
        Task<bool> ExistsAsync(int campaignId);
        Task<int> BulkCloseExpiredAsync();
        Task<bool> HasOverlappingCampaignAsync(int clubId, DateTime startDate, DateTime endDate, int? excludeCampaignId = null);
    }
}
