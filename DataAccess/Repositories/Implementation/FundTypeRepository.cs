using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implementation;

public sealed class FundTypeRepository : IFundTypeRepository
{
    private readonly UnicContext _context;

    public FundTypeRepository(UnicContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<FundType>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<FundType?> GetByIdAsync(int fundTypeId, CancellationToken cancellationToken = default)
    {
        if (fundTypeId <= 0)
            return null;

        return await _context.FundTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FundTypeId == fundTypeId, cancellationToken);
    }
}

