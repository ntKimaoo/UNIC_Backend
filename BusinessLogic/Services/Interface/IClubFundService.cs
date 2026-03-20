using BusinessLogic.DTOs;

namespace BusinessLogic.Services.Interface
{
    public interface IClubFundService
    {
        Task<FundResponseDto> CreateFundAsync(Guid userId, CreateFundDto dto);
        Task<ContributeResponseDto> CreateContributionAsync(Guid userId, ContributeRequestDto request, CancellationToken cancellationToken = default);
        Task<ContributionPaymentStatusDto?> GetContributionPaymentStatusAsync(Guid userId, int clubId, int transactionId);
        Task<ContributionPaymentStatusDto?> GetContributionPaymentStatusByOrderCodeAsync(Guid userId, int orderCode);
        Task<FundResponseDto?> GetFundByIdAsync(int fundId);
        Task<IEnumerable<FundResponseDto>> GetFundsByClubIdAsync(int clubId);
        Task<IEnumerable<FundTransactionResponseDto>> GetFundHistoryAsync(int fundId, string? status, string? scope, Guid? currentUserId);
        Task<bool> ApproveFundAsync(Guid managerId, ApproveFundDto dto);
        Task<bool> ProcessPayOSPaymentSuccessAsync(int orderCode);
        Task<bool> TryCompleteOwnPendingContributionForDevelopmentAsync(Guid userId, int clubId, int transactionId);
        Task<FundCapabilitiesDto> GetFundCapabilitiesAsync(Guid userId, int clubId);
    }
}