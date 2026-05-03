using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Implementation
{
    public class ClubPostRepository : IClubPostRepository
    {
        private readonly UnicContext _context;

        public ClubPostRepository(UnicContext context)
        {
            _context = context;
        }

        private IQueryable<ClubPost> ActivePosts() =>
            _context.ClubPosts
                .Where(cp => !cp.IsDeleted)
                .Include(cp => cp.Club)
                .Include(cp => cp.User)
                .Include(cp => cp.Event)
                .Include(cp => cp.RecruitmentCampaign);

        public async Task<ClubPost?> GetByIdAsync(int postId) =>
            await ActivePosts().FirstOrDefaultAsync(cp => cp.PostId == postId);

        public async Task<IEnumerable<ClubPost>> GetAllAsync() =>
            await ActivePosts().OrderByDescending(cp => cp.PostDate).ToListAsync();

        public async Task<IEnumerable<ClubPost>> GetByClubIdAsync(int clubId) =>
            await ActivePosts()
                .Where(cp => cp.ClubId == clubId)
                .OrderByDescending(cp => cp.PostDate)
                .ToListAsync();

        public async Task<IEnumerable<ClubPost>> GetByUserIdAsync(Guid userId) =>
            await ActivePosts()
                .Where(cp => cp.UserId == userId)
                .OrderByDescending(cp => cp.PostDate)
                .ToListAsync();

        public async Task<IEnumerable<ClubPost>> GetByEventIdAsync(int eventId) =>
            await ActivePosts()
                .Where(cp => cp.EventId == eventId)
                .OrderByDescending(cp => cp.PostDate)
                .ToListAsync();

        public async Task<IEnumerable<ClubPost>> GetByCampaignIdAsync(int campaignId) =>
            await ActivePosts()
                .Where(cp => cp.CampaignId == campaignId)
                .OrderByDescending(cp => cp.PostDate)
                .ToListAsync();

        public async Task<ClubPost> CreateAsync(ClubPost post)
        {
            await _context.ClubPosts.AddAsync(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<bool> UpdateAsync(ClubPost post)
        {
            try
            {
                _context.ClubPosts.Update(post);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int postId)
        {
            try
            {
                var post = await _context.ClubPosts.FindAsync(postId);
                if (post == null || post.IsDeleted)
                    return false;

                post.IsDeleted = true;
                post.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExistsAsync(int postId) =>
            await _context.ClubPosts.AnyAsync(cp => cp.PostId == postId && !cp.IsDeleted);
    }
}
