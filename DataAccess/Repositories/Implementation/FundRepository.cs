using System.Data;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using UNIC.DataAccess.Repositories.Interface;

namespace DataAccess.Repositories.Implementation
{
    public class FundRepository : IFundRepository
    {
        private readonly UnicContext _context;

        public FundRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<FundTransaction?> GetTransactionByIdAsync(int id)
        {
            return await _context.FundTransactions
                .AsNoTracking()
                .Include(t => t.ClubFund)
                .Include(t => t.Creator)
                .FirstOrDefaultAsync(t => t.TransactionId == id);
        }

        public async Task<FundTransaction?> GetTransactionForExternalCheckoutCompletionAsync(long externalOrLegacyOrderCode)
        {
            var byExternal = await _context.FundTransactions
                .AsNoTracking()
                .Include(t => t.ClubFund)
                .Include(t => t.Creator)
                .FirstOrDefaultAsync(t => t.ExternalOrderCode == externalOrLegacyOrderCode);
            if (byExternal != null)
                return byExternal;

            if (externalOrLegacyOrderCode < 1 || externalOrLegacyOrderCode > int.MaxValue)
                return null;

            var legacyId = (int)externalOrLegacyOrderCode;
            return await _context.FundTransactions
                .AsNoTracking()
                .Include(t => t.ClubFund)
                .Include(t => t.Creator)
                .FirstOrDefaultAsync(t =>
                    t.TransactionId == legacyId
                    && t.ExternalOrderCode == null
                    && t.IsMemberContribution
                    && t.TransactionType != null
                    && t.TransactionType.ToUpper() == "INCOME");
        }

        public async Task<ClubFund?> GetFundByIdAsync(int id, bool includeDeleted = false)
        {
            return await _context.ClubFunds
                .AsNoTracking()
                .Include(f => f.FundType)
                .FirstOrDefaultAsync(f => f.FundId == id && (includeDeleted || !f.IsDeleted));
        }

        public async Task<ClubFund?> GetFundByPublicIdAsync(Guid publicId, bool includeDeleted = false)
        {
            return await _context.ClubFunds
                .AsNoTracking()
                .Include(f => f.FundType)
                .FirstOrDefaultAsync(f => f.PublicId == publicId && (includeDeleted || !f.IsDeleted));
        }

        public async Task<bool> ExistsNonRejectedFundNameInClubAsync(int clubId, string fundNameNormalized)
        {
            var normalized = (fundNameNormalized ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(normalized))
                return false;

            return await _context.ClubFunds
                .AsNoTracking()
                .AnyAsync(f =>
                    !f.IsDeleted &&
                    f.ClubId == clubId &&
                    f.FundName != null &&
                    f.FundName.Trim().ToUpper() == normalized &&
                    (f.Status == null || f.Status.ToUpper() != "REJECTED"));
        }

        public async Task<ClubFund> AddFundAsync(ClubFund fund)
        {
            await _context.ClubFunds.AddAsync(fund);
            await _context.SaveChangesAsync();
            return fund;
        }

        public async Task AddTransactionAsync(FundTransaction transaction)
        {
            await _context.FundTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTransactionAsync(FundTransaction transaction)
        {
            _context.FundTransactions.Update(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateClubFundAsync(ClubFund fund)
        {
            _context.ClubFunds.Update(fund);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTransactionAndFundAsync(FundTransaction transaction, ClubFund fund)
        {
            _context.FundTransactions.Update(transaction);
            _context.ClubFunds.Update(fund);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTransactionByIdAsync(int transactionId)
        {
            var entity = await _context.FundTransactions.FindAsync(transactionId);
            if (entity != null)
            {
                _context.FundTransactions.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<FundCategory?> GetFundCategoryByIdAsync(int categoryId)
        {
            return await _context.FundCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        }

        public async Task<IReadOnlyList<FundCategory>> GetFundCategoriesForClubAsync(int clubId)
        {
            return await _context.FundCategories
                .AsNoTracking()
                .Where(c => c.ClubId == null || c.ClubId == clubId)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<bool> TryApproveMemberContributionAsync(int transactionId)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var entity = await _context.FundTransactions
                    .Include(t => t.ClubFund)
                    .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

                if (entity == null
                    || !entity.IsMemberContribution
                    || entity.TransactionType == null
                    || !string.Equals(entity.TransactionType, "INCOME", StringComparison.OrdinalIgnoreCase)
                    || entity.Status == null
                    || !string.Equals(entity.Status, "PENDING", StringComparison.OrdinalIgnoreCase)
                    || entity.ClubFund == null
                    || entity.ClubFund.IsDeleted)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                var utc = DateTime.UtcNow;
                entity.Status = "APPROVED";
                entity.TransactionDate = utc;
                entity.UpdatedAt = utc;
                entity.ClubFund.CurrentBalance += entity.Amount;
                entity.ClubFund.TotalAmount += entity.Amount;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<ClubFund>> GetFundsByClubIdAsync(int clubId)
        {
            return await _context.ClubFunds
                .AsNoTracking()
                .Include(f => f.FundType)
                .Where(cf => cf.ClubId == clubId && !cf.IsDeleted)
                .OrderBy(cf => cf.FundName)
                .ToListAsync();
        }

        public async Task<(IEnumerable<ClubFund> Items, int TotalCount)> GetFundsByClubIdPagedAsync(
            int clubId,
            string? status,
            string? search,
            string sort,
            int pageNumber,
            int pageSize,
            bool includeSoftDeletedFunds = false)
        {
            var query = _context.ClubFunds
                .AsNoTracking()
                .Include(f => f.FundType)
                .Where(cf => cf.ClubId == clubId && (!cf.IsDeleted || includeSoftDeletedFunds));

            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim().ToUpperInvariant();
                if (st == "PENDING")
                {
                    query = query.Where(cf => cf.Status == null || cf.Status.ToUpper() == st);
                }
                else
                {
                    query = query.Where(cf => cf.Status != null && cf.Status.ToUpper() == st);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(cf => cf.FundName != null && cf.FundName.Contains(keyword));
            }

            query = sort switch
            {
                "OLDEST" => query
                    .OrderBy(cf => cf.CreatedAt)
                    .ThenBy(cf => cf.FundId),
                "NAME_ASC" => query
                    .OrderBy(cf => cf.FundName)
                    .ThenBy(cf => cf.FundId),
                "NAME_DESC" => query
                    .OrderByDescending(cf => cf.FundName)
                    .ThenByDescending(cf => cf.FundId),
                _ => query
                    .OrderByDescending(cf => cf.CreatedAt)
                    .ThenByDescending(cf => cf.FundId)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<ClubFund> Items, int TotalCount)> GetMyFundsByClubIdPagedAsync(
            int clubId,
            Guid currentUserId,
            string mineType,
            string? status,
            string? search,
            string sort,
            int pageNumber,
            int pageSize,
            bool includeSoftDeletedFunds = false)
        {
            var query = _context.ClubFunds
                .AsNoTracking()
                .Include(f => f.FundType)
                .Where(cf => cf.ClubId == clubId && (!cf.IsDeleted || includeSoftDeletedFunds));

            if (mineType == "CREATED")
            {
                query = query.Where(cf => cf.CreatedBy == currentUserId);
            }
            else if (mineType == "RESPONSIBLE")
            {
                query = query.Where(cf => cf.ApprovedBy == currentUserId);
            }
            else
            {
                query = query.Where(cf =>
                    cf.CreatedBy == currentUserId
                    || cf.ApprovedBy == currentUserId
                    || _context.FundTransactions.Any(t =>
                        t.FundId == cf.FundId
                        && (t.CreatedBy == currentUserId || t.ApprovedBy == currentUserId)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim().ToUpperInvariant();
                if (st == "PENDING")
                {
                    query = query.Where(cf => cf.Status == null || cf.Status.ToUpper() == st);
                }
                else
                {
                    query = query.Where(cf => cf.Status != null && cf.Status.ToUpper() == st);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(cf => cf.FundName != null && cf.FundName.Contains(keyword));
            }

            query = sort switch
            {
                "OLDEST" => query
                    .OrderBy(cf => cf.CreatedAt)
                    .ThenBy(cf => cf.FundId),
                "NAME_ASC" => query
                    .OrderBy(cf => cf.FundName)
                    .ThenBy(cf => cf.FundId),
                "NAME_DESC" => query
                    .OrderByDescending(cf => cf.FundName)
                    .ThenByDescending(cf => cf.FundId),
                _ => query
                    .OrderByDescending(cf => cf.CreatedAt)
                    .ThenByDescending(cf => cf.FundId)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<FundTransaction>> GetTransactionsByFundIdAsync(
            int fundId,
            string? status = null,
            bool memberContributionsOnly = false,
            Guid? createdByUserId = null)
        {
            var query = BuildFundTransactionQuery(fundId, status, memberContributionsOnly, createdByUserId);
            return await query
                .OrderByDescending(t => t.UpdatedAt)
                .ThenByDescending(t => t.TransactionId)
                .ToListAsync();
        }

        public async Task<(IEnumerable<FundTransaction> Items, int TotalCount)> GetTransactionsByFundIdPagedAsync(
            int fundId,
            string? status,
            bool memberContributionsOnly,
            Guid? createdByUserId,
            int pageNumber,
            int pageSize)
        {
            var query = BuildFundTransactionQuery(fundId, status, memberContributionsOnly, createdByUserId);
            query = query
                .OrderByDescending(t => t.UpdatedAt)
                .ThenByDescending(t => t.TransactionId);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<FundTransaction> Items, int TotalCount)> GetTransactionsByClubIdPagedAsync(
            int clubId,
            int? fundId,
            string? status,
            bool memberContributionsOnly,
            Guid? createdByUserId,
            DateTime? fromUtc,
            DateTime? toUtc,
            int pageNumber,
            int pageSize)
        {
            var query = _context.FundTransactions
                .AsNoTracking()
                .Include(t => t.Creator)
                .Include(t => t.FundCategory)
                .Include(t => t.ClubFund)
                .Where(t => t.ClubFund != null && t.ClubFund.ClubId == clubId && !t.ClubFund.IsDeleted);

            if (fundId.HasValue)
                query = query.Where(t => t.FundId == fundId.Value);

            if (!string.IsNullOrEmpty(status))
            {
                var st = status.ToUpperInvariant();
                query = query.Where(t => t.Status != null && t.Status.ToUpper() == st);
            }

            if (memberContributionsOnly)
            {
                query = query.Where(t =>
                    t.IsMemberContribution &&
                    t.TransactionType != null &&
                    t.TransactionType.ToUpper() == "INCOME");
            }

            if (createdByUserId.HasValue)
                query = query.Where(t => t.CreatedBy == createdByUserId.Value);

            if (fromUtc.HasValue)
                query = query.Where(t => t.TransactionDate >= fromUtc.Value);
            if (toUtc.HasValue)
                query = query.Where(t => t.TransactionDate <= toUtc.Value);

            query = query
                .OrderByDescending(t => t.UpdatedAt)
                .ThenByDescending(t => t.TransactionId);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        private IQueryable<FundTransaction> BuildFundTransactionQuery(
            int fundId,
            string? status,
            bool memberContributionsOnly,
            Guid? createdByUserId)
        {
            var query = _context.FundTransactions
                .AsNoTracking()
                .Include(t => t.Creator)
                .Include(t => t.FundCategory)
                .Where(t => t.FundId == fundId);

            if (!string.IsNullOrEmpty(status))
            {
                var st = status.ToUpperInvariant();
                query = query.Where(t => t.Status != null && t.Status.ToUpper() == st);
            }

            if (memberContributionsOnly)
            {
                query = query.Where(t =>
                    t.IsMemberContribution &&
                    t.TransactionType != null &&
                    t.TransactionType.ToUpper() == "INCOME");
            }

            if (createdByUserId.HasValue)
            {
                query = query.Where(t => t.CreatedBy == createdByUserId.Value);
            }

            return query;
        }

        public async Task<(int PendingFundCount, int ApprovedFundCount, int RejectedFundCount, decimal TotalBalanceApprovedFunds, decimal TotalApprovedIncome, decimal TotalApprovedExpense)> GetClubFundReportAggregatesAsync(
            int clubId,
            DateTime? fromUtc,
            DateTime? toUtc)
        {
            var statuses = await _context.ClubFunds
                .AsNoTracking()
                .Where(f => f.ClubId == clubId && !f.IsDeleted)
                .Select(f => f.Status)
                .ToListAsync();

            var pending = 0;
            var approved = 0;
            var rejected = 0;
            foreach (var s in statuses)
            {
                var u = (s ?? "PENDING").Trim().ToUpperInvariant();
                if (u == "APPROVED")
                    approved++;
                else if (u == "REJECTED")
                    rejected++;
                else
                    pending++;
            }

            var totalBalanceApproved = await _context.ClubFunds
                .AsNoTracking()
                .Where(f => f.ClubId == clubId && !f.IsDeleted && f.Status != null && f.Status.ToUpper() == "APPROVED")
                .SumAsync(f => (decimal?)f.CurrentBalance) ?? 0m;

            var txQuery =
                from t in _context.FundTransactions.AsNoTracking()
                join f in _context.ClubFunds.AsNoTracking() on t.FundId equals f.FundId
                where f.ClubId == clubId && !f.IsDeleted
                    && t.Status != null
                    && t.Status.ToUpper() == "APPROVED"
                    && t.TransactionType != null
                select t;

            if (fromUtc.HasValue)
                txQuery = txQuery.Where(t => t.TransactionDate >= fromUtc.Value);
            if (toUtc.HasValue)
                txQuery = txQuery.Where(t => t.TransactionDate <= toUtc.Value);

            var income = await txQuery
                .Where(t => t.TransactionType!.ToUpper() == "INCOME")
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var expense = await txQuery
                .Where(t => t.TransactionType!.ToUpper() == "EXPENSE")
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            return (pending, approved, rejected, totalBalanceApproved, income, expense);
        }

        public async Task AddRefundRequestAsync(FundRefundRequest request)
        {
            await _context.FundRefundRequests.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsPendingRefundForOriginalTransactionAsync(int originalTransactionId)
        {
            return await _context.FundRefundRequests
                .AsNoTracking()
                .AnyAsync(r =>
                    r.OriginalTransactionId == originalTransactionId
                    && r.Status != null
                    && r.Status.ToUpper() == "PENDING");
        }

        public async Task<decimal> GetTotalRefundedAmountForOriginalTransactionAsync(int originalTransactionId)
        {
            return await _context.FundTransactions
                .AsNoTracking()
                .Where(t =>
                    t.RefundForTransactionId == originalTransactionId
                    && t.TransactionType != null
                    && t.TransactionType.ToUpper() == "EXPENSE"
                    && t.Status != null
                    && t.Status.ToUpper() == "APPROVED")
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;
        }

        public async Task<FundRefundRequest?> GetRefundRequestByIdAsync(int refundRequestId)
        {
            return await _context.FundRefundRequests
                .AsNoTracking()
                .Include(r => r.OriginalTransaction)
                .ThenInclude(t => t.ClubFund)
                .Include(r => r.ClubFund)
                .FirstOrDefaultAsync(r => r.RefundRequestId == refundRequestId);
        }

        public async Task<(IEnumerable<FundRefundRequest> Items, int TotalCount)> GetRefundRequestsForMemberPagedAsync(
            int clubId, Guid requestedBy, int pageNumber, int pageSize)
        {
            var query = _context.FundRefundRequests
                .AsNoTracking()
                .Include(r => r.OriginalTransaction)
                .ThenInclude(t => t.ClubFund)
                .Where(r => r.ClubId == clubId && r.RequestedBy == requestedBy)
                .OrderByDescending(r => r.CreatedAtUtc)
                .ThenByDescending(r => r.RefundRequestId);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<FundRefundRequest> Items, int TotalCount)> GetRefundRequestsForClubPagedAsync(
            int clubId, string? status, int pageNumber, int pageSize)
        {
            var query = _context.FundRefundRequests
                .AsNoTracking()
                .Include(r => r.OriginalTransaction)
                .ThenInclude(t => t.ClubFund)
                .Include(r => r.ClubFund)
                .Where(r => r.ClubId == clubId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim().ToUpperInvariant();
                query = query.Where(r => r.Status != null && r.Status.ToUpper() == st);
            }

            query = query
                .OrderByDescending(r => r.CreatedAtUtc)
                .ThenByDescending(r => r.RefundRequestId);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> TryCompleteRefundRequestAsync(
            int refundRequestId, Guid managerId, string? transferReference, string? managerNote)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var req = await _context.FundRefundRequests
                    .Include(r => r.OriginalTransaction)
                    .ThenInclude(t => t!.ClubFund)
                    .FirstOrDefaultAsync(r => r.RefundRequestId == refundRequestId);

                if (req == null
                    || req.OriginalTransaction == null
                    || req.OriginalTransaction.ClubFund == null)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                if (req.Status == null || !string.Equals(req.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return false;
                }

                var orig = req.OriginalTransaction;
                var fund = orig.ClubFund;

                if (fund.IsDeleted)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                if (!orig.IsMemberContribution
                    || orig.TransactionType == null
                    || !string.Equals(orig.TransactionType, "INCOME", StringComparison.OrdinalIgnoreCase)
                    || orig.Status == null
                    || !string.Equals(orig.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return false;
                }

                if (orig.CreatedBy != req.RequestedBy || orig.FundId != req.FundId || fund.ClubId != req.ClubId)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                var alreadyRefunded = await _context.FundTransactions
                    .Where(t =>
                        t.RefundForTransactionId == orig.TransactionId
                        && t.TransactionType != null
                        && t.TransactionType.ToUpper() == "EXPENSE"
                        && t.Status != null
                        && t.Status.ToUpper() == "APPROVED")
                    .SumAsync(t => (decimal?)t.Amount) ?? 0m;

                if (req.Amount + alreadyRefunded > orig.Amount || req.Amount <= 0m)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                if (fund.CurrentBalance < req.Amount)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                var utc = DateTime.UtcNow;
                var tr = (transferReference ?? string.Empty).Trim();
                if (tr.Length > 100)
                    tr = tr[..100];
                var note = (managerNote ?? string.Empty).Trim();
                if (note.Length > 500)
                    note = note[..500];

                var expense = new FundTransaction
                {
                    FundId = orig.FundId,
                    TransactionType = "EXPENSE",
                    Status = "APPROVED",
                    Amount = req.Amount,
                    Description = $"Hoàn tiền nộp quỹ (giao dịch #{orig.TransactionId})",
                    TransactionDate = utc,
                    CreatedAt = utc,
                    UpdatedAt = utc,
                    CreatedBy = managerId,
                    ApprovedBy = managerId,
                    IsMemberContribution = false,
                    RefundForTransactionId = orig.TransactionId
                };

                await _context.FundTransactions.AddAsync(expense);

                fund.CurrentBalance -= req.Amount;

                req.Status = "COMPLETED";
                req.CompletedAtUtc = utc;
                req.CompletedBy = managerId;
                req.TransferReference = string.IsNullOrEmpty(tr) ? null : tr;
                req.ManagerNote = string.IsNullOrEmpty(note) ? null : note;
                req.UpdatedAtUtc = utc;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> TryRejectRefundRequestAsync(int refundRequestId, Guid managerId, string rejectionReason)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var req = await _context.FundRefundRequests
                    .FirstOrDefaultAsync(r => r.RefundRequestId == refundRequestId);

                if (req == null
                    || req.Status == null
                    || !string.Equals(req.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return false;
                }

                var utc = DateTime.UtcNow;
                req.Status = "REJECTED";
                req.RejectedAtUtc = utc;
                req.RejectedBy = managerId;
                req.RejectionReason = rejectionReason;
                req.UpdatedAtUtc = utc;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> TryCancelRefundRequestAsync(int refundRequestId, Guid memberUserId)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var req = await _context.FundRefundRequests
                    .FirstOrDefaultAsync(r => r.RefundRequestId == refundRequestId);

                if (req == null
                    || req.RequestedBy != memberUserId
                    || req.Status == null
                    || !string.Equals(req.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return false;
                }

                var utc = DateTime.UtcNow;
                req.Status = "CANCELLED";
                req.UpdatedAtUtc = utc;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<(int TransactionId, decimal NewCurrentBalance)?> TryRecordApprovedManagerRefundExpenseAsync(
            int clubId,
            int fundId,
            int originalTransactionId,
            Guid managerId,
            decimal amount,
            string? reason,
            string? transferReference,
            string? managerNote)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                if (amount <= 0m)
                {
                    await tx.RollbackAsync();
                    return null;
                }

                var orig = await _context.FundTransactions
                    .Include(t => t.ClubFund)
                    .FirstOrDefaultAsync(t => t.TransactionId == originalTransactionId);

                if (orig == null || orig.ClubFund == null)
                {
                    await tx.RollbackAsync();
                    return null;
                }

                var fund = orig.ClubFund;
                if (fund.ClubId != clubId || orig.FundId != fundId || fund.IsDeleted)
                {
                    await tx.RollbackAsync();
                    return null;
                }

                if (!orig.IsMemberContribution
                    || orig.TransactionType == null
                    || !string.Equals(orig.TransactionType, "INCOME", StringComparison.OrdinalIgnoreCase)
                    || orig.Status == null
                    || !string.Equals(orig.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return null;
                }

                var alreadyRefunded = await _context.FundTransactions
                    .Where(t =>
                        t.RefundForTransactionId == orig.TransactionId
                        && t.TransactionType != null
                        && t.TransactionType.ToUpper() == "EXPENSE"
                        && t.Status != null
                        && t.Status.ToUpper() == "APPROVED")
                    .SumAsync(t => (decimal?)t.Amount) ?? 0m;

                if (amount + alreadyRefunded > orig.Amount)
                {
                    await tx.RollbackAsync();
                    return null;
                }

                if (fund.CurrentBalance < amount)
                {
                    await tx.RollbackAsync();
                    return null;
                }

                var utc = DateTime.UtcNow;
                var tr = (transferReference ?? string.Empty).Trim();
                if (tr.Length > 100) tr = tr[..100];
                var note = (managerNote ?? string.Empty).Trim();
                if (note.Length > 500) note = note[..500];
                var rs = (reason ?? string.Empty).Trim();
                if (rs.Length > 2000) rs = rs[..2000];

                var desc = string.IsNullOrEmpty(rs)
                    ? $"Hoàn tiền nộp quỹ (giao dịch #{orig.TransactionId})"
                    : $"Hoàn tiền nộp quỹ (giao dịch #{orig.TransactionId}): {rs}";

                var expense = new FundTransaction
                {
                    FundId = orig.FundId,
                    TransactionType = "EXPENSE",
                    Status = "APPROVED",
                    Amount = amount,
                    Description = desc,
                    TransactionDate = utc,
                    CreatedAt = utc,
                    UpdatedAt = utc,
                    CreatedBy = managerId,
                    ApprovedBy = managerId,
                    IsMemberContribution = false,
                    RefundForTransactionId = orig.TransactionId,
                    ContributionSource = "MANAGER_REFUND"
                };

                await _context.FundTransactions.AddAsync(expense);

                fund.CurrentBalance -= amount;
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return (expense.TransactionId, fund.CurrentBalance);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<(int TransactionId, decimal NewCurrentBalance)?> TryRecordApprovedCashIncomeAsync(
            int clubId,
            int fundId,
            Guid contributorUserId,
            Guid recordedByUserId,
            decimal amount,
            string description,
            DateTime transactionDateUtc,
            int? categoryId,
            string contributionSource)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var fund = await _context.ClubFunds
                    .FirstOrDefaultAsync(f => f.FundId == fundId);

                if (fund == null || fund.ClubId != clubId || fund.IsDeleted)
                {
                    await tx.RollbackAsync();
                    return null;
                }

                var st = string.IsNullOrWhiteSpace(fund.Status) ? "PENDING" : fund.Status.Trim();
                if (!string.Equals(st, "APPROVED", StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return null;
                }

                if (fund.ExpiresAt.HasValue && DateTime.UtcNow.Date > fund.ExpiresAt.Value.Date)
                {
                    await tx.RollbackAsync();
                    return null;
                }

                var utc = DateTime.UtcNow;
                var entity = new FundTransaction
                {
                    FundId = fundId,
                    CategoryId = categoryId,
                    TransactionType = "INCOME",
                    Status = "APPROVED",
                    Amount = amount,
                    Description = description,
                    TransactionDate = transactionDateUtc,
                    CreatedAt = utc,
                    UpdatedAt = utc,
                    CreatedBy = contributorUserId,
                    ApprovedBy = recordedByUserId,
                    IsMemberContribution = true,
                    PaymentLinkId = null,
                    ContributionSource = contributionSource,
                    RefundForTransactionId = null
                };

                await _context.FundTransactions.AddAsync(entity);

                fund.CurrentBalance += amount;
                fund.TotalAmount += amount;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return (entity.TransactionId, fund.CurrentBalance);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> SoftDeleteFundAsync(int fundId, int clubId, Guid deletedByUserId)
        {
            var fund = await _context.ClubFunds.FirstOrDefaultAsync(f => f.FundId == fundId && f.ClubId == clubId && !f.IsDeleted);
            if (fund == null)
                return false;

            fund.IsDeleted = true;
            fund.DeletedAtUtc = DateTime.UtcNow;
            fund.DeletedBy = deletedByUserId;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
