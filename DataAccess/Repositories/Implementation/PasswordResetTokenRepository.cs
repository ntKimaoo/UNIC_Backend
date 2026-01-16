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
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly UnicAuthenticateContext _context;

        public PasswordResetTokenRepository(UnicAuthenticateContext context)
        {
            _context = context;
        }

        public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash)
        {
            return await _context.PasswordResetTokens
                .Include(t => t.Member)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash
                                       && !t.IsUsed
                                       && t.ExpiresAt > DateTime.UtcNow);
        }

        public async Task<PasswordResetToken> CreateAsync(PasswordResetToken token)
        {
            await _context.PasswordResetTokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<bool> MarkAsUsedAsync(int tokenId)
        {
            try
            {
                var token = await _context.PasswordResetTokens.FindAsync(tokenId);
                if (token == null) return false;

                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> InvalidateAllByMemberIdAsync(Guid memberId)
        {
            try
            {
                var tokens = await _context.PasswordResetTokens
                    .Where(t => t.MemberId == memberId && !t.IsUsed)
                    .ToListAsync();

                foreach (var token in tokens)
                {
                    token.IsUsed = true;
                    token.UsedAt = DateTime.UtcNow;
                }

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
