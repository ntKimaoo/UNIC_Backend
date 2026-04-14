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

        Task AddRefundRequestAsync(FundRefundRequest request);
        Task<bool> ExistsPendingRefundForOriginalTransactionAsync(int originalTransactionId);
        Task<decimal> GetTotalRefundedAmountForOriginalTransactionAsync(int originalTransactionId);
        Task<FundRefundRequest?> GetRefundRequestByIdAsync(int refundRequestId);
        Task<(IEnumerable<FundRefundRequest> Items, int TotalCount)> GetRefundRequestsForMemberPagedAsync(
            int clubId, Guid requestedBy, int pageNumber, int pageSize);
        Task<(IEnumerable<FundRefundRequest> Items, int TotalCount)> GetRefundRequestsForClubPagedAsync(
            int clubId, string? status, int pageNumber, int pageSize);
        Task<bool> TryCompleteRefundRequestAsync(
            int refundRequestId, Guid managerId, string? transferReference, string? managerNote);
        Task<bool> TryRejectRefundRequestAsync(int refundRequestId, Guid managerId, string rejectionReason);
        Task<bool> TryCancelRefundRequestAsync(int refundRequestId, Guid memberUserId);

        Task<(int TransactionId, decimal NewCurrentBalance)?> TryRecordApprovedManagerRefundExpenseAsync(
            int clubId,
            int fundId,
            int originalTransactionId,
            Guid managerId,
            decimal amount,
            string? reason,
            string? transferReference,
            string? managerNote);

        Task<(int TransactionId, decimal NewCurrentBalance)?> TryRecordApprovedCashIncomeAsync(
            int clubId,
            int fundId,
            Guid contributorUserId,
            Guid recordedByUserId,
            decimal amount,
            string description,
            DateTime transactionDateUtc,
            int? categoryId,
            string contributionSource);
    }
}
