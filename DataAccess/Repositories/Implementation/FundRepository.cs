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

        public async Task<ClubFund?> GetFundByIdAsync(int id)
        {
            return await _context.ClubFunds
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FundId == id);
        }

        public async Task<bool> ExistsNonRejectedFundNameInClubAsync(int clubId, string fundNameNormalized)
        {
            var normalized = (fundNameNormalized ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(normalized))
                return false;

            return await _context.ClubFunds
                .AsNoTracking()
                .AnyAsync(f =>
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
                    || entity.ClubFund == null)
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
                .Where(cf => cf.ClubId == clubId)
                .OrderBy(cf => cf.FundName)
                .ToListAsync();
        }

        public async Task<(IEnumerable<ClubFund> Items, int TotalCount)> GetFundsByClubIdPagedAsync(
            int clubId,
            string? status,
            string? search,
            string sort,
            int pageNumber,
            int pageSize)
        {
            var query = _context.ClubFunds
                .AsNoTracking()
                .Where(cf => cf.ClubId == clubId);

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
            int pageSize)
        {
            var query = _context.ClubFunds
                .AsNoTracking()
                .Where(cf => cf.ClubId == clubId);

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
                .Where(t => t.ClubFund != null && t.ClubFund.ClubId == clubId);

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
                .Where(f => f.ClubId == clubId)
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
                .Where(f => f.ClubId == clubId && f.Status != null && f.Status.ToUpper() == "APPROVED")
                .SumAsync(f => (decimal?)f.CurrentBalance) ?? 0m;

            var txQuery =
                from t in _context.FundTransactions.AsNoTracking()
                join f in _context.ClubFunds.AsNoTracking() on t.FundId equals f.FundId
                where f.ClubId == clubId
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
    }
}
