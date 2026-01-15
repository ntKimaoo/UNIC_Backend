using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IEmailVerificationTokenRepository
    {
        Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash);
        Task<EmailVerificationToken> CreateAsync(EmailVerificationToken token);
        Task<bool> MarkAsUsedAsync(int tokenId);
        Task<bool> InvalidateAllByMemberIdAsync(int memberId);
    }
}
