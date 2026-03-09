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

        public async Task<ClubPost?> GetByIdAsync(int postId)
        {
            return await _context.ClubPosts
                .Include(cp => cp.Club)
                .Include(cp => cp.User)
                .FirstOrDefaultAsync(cp => cp.PostId == postId);
        }

        public async Task<IEnumerable<ClubPost>> GetAllAsync()
        {
            return await _context.ClubPosts
                .Include(cp => cp.Club)
                .Include(cp => cp.User)
                .OrderByDescending(cp => cp.PostDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClubPost>> GetByClubIdAsync(int clubId)
        {
            return await _context.ClubPosts
                .Include(cp => cp.Club)
                .Include(cp => cp.User)
                .Where(cp => cp.ClubId == clubId)
                .OrderByDescending(cp => cp.PostDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClubPost>> GetByUserIdAsync(Guid userId)
        {
            return await _context.ClubPosts
                .Include(cp => cp.Club)
                .Include(cp => cp.User)
                .Where(cp => cp.UserId == userId)
                .OrderByDescending(cp => cp.PostDate)
                .ToListAsync();
        }

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
                var post = await GetByIdAsync(postId);
                if (post == null)
                    return false;

                _context.ClubPosts.Remove(post);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExistsAsync(int postId)
        {
            return await _context.ClubPosts
                .AnyAsync(cp => cp.PostId == postId);
        }
    }
}
