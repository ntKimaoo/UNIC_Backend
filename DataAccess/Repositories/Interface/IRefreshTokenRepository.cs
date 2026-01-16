using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task<RefreshToken?> GetByTokenHashWithMemberAsync(string tokenHash);
        Task<bool> AddAsync(RefreshToken refreshToken);
        Task<bool> UpdateAsync(RefreshToken refreshToken);
        Task<bool> RevokeAllByMemberIdAsync(Guid memberId);
        Task<List<RefreshToken>> GetActiveTokensByMemberIdAsync(Guid memberId);
    }
}
