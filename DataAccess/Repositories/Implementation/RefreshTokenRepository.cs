using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Implementation
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly UnicContext _context;

        public RefreshTokenRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
        }

        public async Task<RefreshToken?> GetByTokenHashWithUserAsync(string tokenHash)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                    .ThenInclude(u => u.UserRoles)
                .Include(rt => rt.User)
                    .ThenInclude(u => u.ClubMembers)
                    .ThenInclude(cm => cm.ClubRole)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
        }

        public async Task<bool> AddAsync(RefreshToken refreshToken)
        {
            try
            {
                await _context.RefreshTokens.AddAsync(refreshToken);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateAsync(RefreshToken refreshToken)
        {
            try
            {
                _context.RefreshTokens.Update(refreshToken);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RevokeAllByUserIdAsync(Guid userId)
        {
            try
            {
                var tokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && rt.IsRevoked == false)
                    .ToListAsync();

                foreach (var token in tokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId
                          && rt.IsRevoked == false
                          && rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
        }
        public async Task<IEnumerable<RefreshToken>> GetExpiredTokensAsync(DateTime olderThan)
        {
            return await _context.RefreshTokens
                .Where(t => (t.ExpiresAt < DateTime.UtcNow || t.IsRevoked == true)
                            && t.CreatedAt < olderThan)
                .ToListAsync();
        }

        public async Task<bool> DeleteRangeAsync(IEnumerable<RefreshToken> tokens)
        {
            try
            {
                _context.RefreshTokens.RemoveRange(tokens);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
