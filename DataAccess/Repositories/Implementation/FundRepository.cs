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
                .FirstOrDefaultAsync(t => t.TransactionId == id);
        }

        public async Task<ClubFund?> GetFundByIdAsync(int id)
        {
            return await _context.ClubFunds.FindAsync(id);
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
        public async Task<IEnumerable<FundTransaction>> GetTransactionsByFundIdAsync(int fundId, string? status = null)
        {
            var query = _context.FundTransactions
                .Where(t => t.FundId == fundId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }

            return await query
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }
    }
}