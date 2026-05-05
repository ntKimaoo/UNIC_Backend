using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
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
                .Where(rc => rc.Club.IsActive)
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

        public async Task<(IEnumerable<RecruitmentCampaign> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, string? filterBy, bool ascending)
        {
            var query = _context.RecruitmentCampaigns
                .Include(rc => rc.Club)
                .Where(rc => rc.Club.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(rc =>
                    rc.CampaignName.Contains(search) ||
                    (rc.Description != null && rc.Description.Contains(search)));

            if (!string.IsNullOrWhiteSpace(filterBy))
                query = query.Where(rc => rc.Status.ToLower() == filterBy.ToLower());

            query = ascending
                ? query.OrderBy(rc => rc.CreatedAt)
                : query.OrderByDescending(rc => rc.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(IEnumerable<RecruitmentCampaign> Items, int TotalCount)> GetPagedByClubIdAsync(
            int clubId, int page, int pageSize, string? search, string? filterBy, bool ascending)
        {
            var query = _context.RecruitmentCampaigns
                .Include(rc => rc.Club)
                .Where(rc => rc.ClubId == clubId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(rc =>
                    rc.CampaignName.Contains(search) ||
                    (rc.Description != null && rc.Description.Contains(search)));

            if (!string.IsNullOrWhiteSpace(filterBy))
                query = query.Where(rc => rc.Status.ToLower() == filterBy.ToLower());

            query = ascending
                ? query.OrderBy(rc => rc.CreatedAt)
                : query.OrderByDescending(rc => rc.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
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

        public async Task<bool> HasOverlappingCampaignAsync(int clubId, DateTime startDate, DateTime endDate, int? excludeCampaignId = null)
        {
            return await _context.RecruitmentCampaigns
                .Where(c =>
                    c.ClubId == clubId &&
                    c.Status != "CLOSED" &&
                    (excludeCampaignId == null || c.CampaignId != excludeCampaignId) &&
                    c.StartDate.HasValue && c.EndDate.HasValue &&
                    c.StartDate.Value <= endDate &&
                    c.EndDate.Value >= startDate)
                .AnyAsync();
        }

        public async Task<int> BulkCloseExpiredAsync()
        {
            var today = DateTime.Now.Date;
            var expired = await _context.RecruitmentCampaigns
                .Where(c => c.Status == "OPEN" && c.EndDate.HasValue && c.EndDate.Value.Date < today)
                .ToListAsync();

            if (!expired.Any()) return 0;

            foreach (var c in expired)
                c.Status = "CLOSED";

            await _context.SaveChangesAsync();
            return expired.Count;
        }

        public async Task<RecruitmentCampaign?> GetByFormIdAsync(int formId)
        {
            return await _context.RecruitmentCampaigns
                .Include(rc => rc.Club)
                .FirstOrDefaultAsync(rc => rc.ApplicationForms.Any(f => f.FormId == formId));
        }
    }
}
