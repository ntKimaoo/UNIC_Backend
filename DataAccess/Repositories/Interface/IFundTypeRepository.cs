using DataAccess.Models;

namespace DataAccess.Repositories.Interface;

public interface IFundTypeRepository
{
    Task<IReadOnlyList<FundType>> ListActiveAsync(CancellationToken cancellationToken = default);
    Task<FundType?> GetByIdAsync(int fundTypeId, CancellationToken cancellationToken = default);
}

