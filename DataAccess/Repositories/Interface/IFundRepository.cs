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
        Task<bool> ExistsNonRejectedFundNameInClubAsync(int clubId, string fundNameNormalized);
        Task<ClubFund> AddFundAsync(ClubFund fund);
        Task AddTransactionAsync(FundTransaction transaction);
        Task UpdateTransactionAsync(FundTransaction transaction);
        Task UpdateClubFundAsync(ClubFund fund);
        Task UpdateTransactionAndFundAsync(FundTransaction transaction, ClubFund fund);
        Task DeleteTransactionByIdAsync(int transactionId);
        Task<FundCategory?> GetFundCategoryByIdAsync(int categoryId);
        Task<IReadOnlyList<FundCategory>> GetFundCategoriesForClubAsync(int clubId);
        Task<bool> TryApproveMemberContributionAsync(int transactionId);
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

        Task<(IEnumerable<FundTransaction> Items, int TotalCount)> GetTransactionsByClubIdPagedAsync(
            int clubId,
            int? fundId,
            string? status,
            bool memberContributionsOnly,
            Guid? createdByUserId,
            DateTime? fromUtc,
            DateTime? toUtc,
            int pageNumber,
            int pageSize);
        Task<IEnumerable<ClubFund>> GetFundsByClubIdAsync(int clubId);
        Task<(IEnumerable<ClubFund> Items, int TotalCount)> GetFundsByClubIdPagedAsync(
            int clubId,
            string? status,
            string? search,
            string sort,
            int pageNumber,
            int pageSize);
        Task<(IEnumerable<ClubFund> Items, int TotalCount)> GetMyFundsByClubIdPagedAsync(
            int clubId,
            Guid currentUserId,
            string mineType,
            string? status,
            string? search,
            string sort,
            int pageNumber,
            int pageSize);
        Task<(int PendingFundCount, int ApprovedFundCount, int RejectedFundCount, decimal TotalBalanceApprovedFunds, decimal TotalApprovedIncome, decimal TotalApprovedExpense)> GetClubFundReportAggregatesAsync(
            int clubId,
            DateTime? fromUtc,
            DateTime? toUtc);
    }
}
