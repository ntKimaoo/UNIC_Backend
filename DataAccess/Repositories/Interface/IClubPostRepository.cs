using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IClubPostRepository
    {
        Task<ClubPost?> GetByIdAsync(int postId);
        Task<IEnumerable<ClubPost>> GetAllAsync();
        Task<IEnumerable<ClubPost>> GetByClubIdAsync(int clubId);
        Task<IEnumerable<ClubPost>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<ClubPost>> GetByEventIdAsync(int eventId);
        Task<IEnumerable<ClubPost>> GetByCampaignIdAsync(int campaignId);
        Task<ClubPost> CreateAsync(ClubPost post);
        Task<bool> UpdateAsync(ClubPost post);
        Task<bool> DeleteAsync(int postId);
        Task<bool> ExistsAsync(int postId);
    }
}
