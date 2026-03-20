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
                .Include(t => t.ClubFund)
                .Include(t => t.Creator)
                .FirstOrDefaultAsync(t => t.TransactionId == id);
        }

        public async Task<ClubFund?> GetFundByIdAsync(int id)
        {
            return await _context.ClubFunds.FindAsync(id);
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

        public async Task<IEnumerable<ClubFund>> GetFundsByClubIdAsync(int clubId)
        {
            return await _context.ClubFunds
                .Where(cf => cf.ClubId == clubId)
                .OrderBy(cf => cf.FundName)
                .ToListAsync();
        }

        public async Task<(IEnumerable<ClubFund> Items, int TotalCount)> GetFundsByClubIdPagedAsync(
            int clubId, int pageNumber, int pageSize)
        {
            var query = _context.ClubFunds
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
                .Include(t => t.Creator)
                .Where(t => t.FundId == fundId);

            if (!string.IsNullOrEmpty(status))
            {
                var st = status.ToUpperInvariant();
                query = query.Where(t => t.Status != null && t.Status.ToUpper() == st);
            }

            if (memberContributionsOnly)
            {
                query = query.Where(t =>
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