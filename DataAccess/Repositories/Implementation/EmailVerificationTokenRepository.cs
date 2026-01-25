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
    public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
    {
        private readonly UnicAuthenticateContext _context;

        public EmailVerificationTokenRepository(UnicAuthenticateContext context)
        {
            _context = context;
        }

        public async Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash)
        {
            return await _context.EmailVerificationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash
                                       && t.IsUsed==false
                                       && t.ExpiresAt > DateTime.UtcNow);
        }

        public async Task<EmailVerificationToken> CreateAsync(EmailVerificationToken token)
        {
            await _context.EmailVerificationTokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<bool> MarkAsUsedAsync(int tokenId)
        {
            try
            {
                var token = await _context.EmailVerificationTokens.FindAsync(tokenId);
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

        public async Task<bool> InvalidateAllByUserIdAsync(Guid userId)
        {
            try
            {
                var tokens = await _context.EmailVerificationTokens
                    .Where(t => t.UserId == userId && t.IsUsed==false)
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
