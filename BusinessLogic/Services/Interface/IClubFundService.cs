using BusinessLogic.DTOs;
using DataAccess.Models;

namespace BusinessLogic.Services.Interface
{
    public interface IClubFundService
    {
        Task<FundTransaction> CreateRequestAsync(Guid userId, CreateFundRequestDto request);
        Task<bool> ProcessRequestAsync(Guid managerId, ProcessFundRequestDto request);
        Task<IEnumerable<FundTransaction>> GetFundHistoryAsync(int fundId, string? status);
    }
}