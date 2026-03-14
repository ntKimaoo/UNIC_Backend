using BusinessLogic.DTOs;
using DataAccess.Models;

namespace BusinessLogic.Services.Interface
{
    public interface IClubFundService
    {
        Task<FundResponseDto> CreateFundAsync(Guid userId, CreateFundDto dto);
        Task<FundTransaction> CreateRequestAsync(Guid userId, CreateFundRequestDto request);
        Task<bool> ProcessRequestAsync(Guid managerId, ProcessFundRequestDto request);
        Task<FundResponseDto?> GetFundByIdAsync(int fundId);
        Task<IEnumerable<FundResponseDto>> GetFundsByClubIdAsync(int clubId);
        Task<IEnumerable<FundTransactionResponseDto>> GetFundHistoryAsync(int fundId, string? status);
        Task<bool> ApproveFundAsync(Guid managerId, ApproveFundDto dto);
    }
}