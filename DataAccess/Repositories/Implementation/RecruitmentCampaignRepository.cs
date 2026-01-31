using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Implementation
{
    public class RecruitmentCampaignRepository : IRecruitmentCampaignRepository
    {
        private readonly UnicContext _context;

        public RecruitmentCampaignRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<RecruitmentCampaign?> GetByIdAsync(int campaignId)
        {
            return await _context.RecruitmentCampaigns
                .Include(rc => rc.Club)
                .FirstOrDefaultAsync(rc => rc.CampaignId == campaignId);
        }

        public async Task<IEnumerable<RecruitmentCampaign>> GetAllAsync()
        {
            return await _context.RecruitmentCampaigns
                .Include(rc => rc.Club)
                .OrderByDescending(rc => rc.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<RecruitmentCampaign>> GetByClubIdAsync(int clubId)
        {
            return await _context.RecruitmentCampaigns
                .Include(rc => rc.Club)
                .Where(rc => rc.ClubId == clubId)
                .OrderByDescending(rc => rc.CreatedAt)
                .ToListAsync();
        }

        public async Task<RecruitmentCampaign> CreateAsync(RecruitmentCampaign campaign)
        {
            await _context.RecruitmentCampaigns.AddAsync(campaign);
            await _context.SaveChangesAsync();
            return campaign;
        }

        public async Task<bool> UpdateAsync(RecruitmentCampaign campaign)
        {
            try
            {
                _context.RecruitmentCampaigns.Update(campaign);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int campaignId)
        {
            try
            {
                var campaign = await GetByIdAsync(campaignId);
                if (campaign == null)
                    return false;

                _context.RecruitmentCampaigns.Remove(campaign);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExistsAsync(int campaignId)
        {
            return await _context.RecruitmentCampaigns
                .AnyAsync(rc => rc.CampaignId == campaignId);
        }
    }
}
