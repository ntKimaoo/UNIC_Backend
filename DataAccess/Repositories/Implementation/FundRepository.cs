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
            int clubId, int pageNumber, int pageSize)
        {
            var query = _context.ClubFunds
                .AsNoTracking()
                .Where(cf => cf.ClubId == clubId)
                .OrderBy(cf => cf.FundName);

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
    }
}
