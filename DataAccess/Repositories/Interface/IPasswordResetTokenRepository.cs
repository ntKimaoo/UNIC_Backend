using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IPasswordResetTokenRepository
    {
        Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash);
        Task<PasswordResetToken> CreateAsync(PasswordResetToken token);
        Task<bool> MarkAsUsedAsync(int tokenId);
        Task<bool> InvalidateAllByUserIdAsync(Guid userId);
        Task<IEnumerable<PasswordResetToken>> GetExpiredTokensAsync(DateTime olderThan);
        Task<bool> DeleteRangeAsync(IEnumerable<PasswordResetToken> tokens);
    }
}
