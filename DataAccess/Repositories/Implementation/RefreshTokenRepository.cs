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
        private readonly UnicAuthenticateContext _context;

        public RefreshTokenRepository(UnicAuthenticateContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
        }

        public async Task<RefreshToken?> GetByTokenHashWithMemberAsync(string tokenHash)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.Member)
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

        public async Task<bool> RevokeAllByMemberIdAsync(Guid memberId)
        {
            try
            {
                var tokens = await _context.RefreshTokens
                    .Where(rt => rt.MemberId == memberId && rt.IsRevoked == false)
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

        public async Task<List<RefreshToken>> GetActiveTokensByMemberIdAsync(Guid memberId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.MemberId == memberId
                          && rt.IsRevoked == false
                          && rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
        }
    }
}
