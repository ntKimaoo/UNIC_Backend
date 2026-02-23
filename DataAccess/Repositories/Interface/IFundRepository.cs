using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNIC.DataAccess.Repositories.Interface
{
    public interface IFundRepository
    {
        Task<FundTransaction?> GetTransactionByIdAsync(int id);
        Task<ClubFund?> GetFundByIdAsync(int id);
        Task AddTransactionAsync(FundTransaction transaction);
        Task UpdateTransactionAsync(FundTransaction transaction);
        Task UpdateClubFundAsync(ClubFund fund);
        Task<IEnumerable<FundTransaction>> GetTransactionsByFundIdAsync(int fundId, string? status = null);
    }
}
