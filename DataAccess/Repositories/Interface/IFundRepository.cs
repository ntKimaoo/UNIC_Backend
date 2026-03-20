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
        Task<ClubFund> AddFundAsync(ClubFund fund);
        Task AddTransactionAsync(FundTransaction transaction);
        Task UpdateTransactionAsync(FundTransaction transaction);
        Task UpdateClubFundAsync(ClubFund fund);
        Task UpdateTransactionAndFundAsync(FundTransaction transaction, ClubFund fund);
        Task<IEnumerable<FundTransaction>> GetTransactionsByFundIdAsync(
            int fundId,
            string? status = null,
            bool memberContributionsOnly = false,
            Guid? createdByUserId = null);
        Task<(IEnumerable<FundTransaction> Items, int TotalCount)> GetTransactionsByFundIdPagedAsync(
            int fundId,
            string? status,
            bool memberContributionsOnly,
            Guid? createdByUserId,
            int pageNumber,
            int pageSize);
        Task<IEnumerable<ClubFund>> GetFundsByClubIdAsync(int clubId);
        Task<(IEnumerable<ClubFund> Items, int TotalCount)> GetFundsByClubIdPagedAsync(int clubId, int pageNumber, int pageSize);
    }
}
