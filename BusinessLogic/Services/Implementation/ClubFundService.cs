using BusinessLogic.DTOs;
using BusinessLogic.Options;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.Extensions.Options;
using UNIC.DataAccess.Repositories.Interface;

namespace BusinessLogic.Services.Implementation
{
    public class ClubFundService : IClubFundService
    {
        private readonly IFundRepository _fundRepository;
        private readonly IClubMemberRepository _clubMemberRepository;
        private readonly IPayOSService _payOSService;
        private readonly IPolicyService _policyService;
        private readonly PayOSOptions _payOSOptions;
        private const string STATUS_PENDING = "PENDING";
        private const string STATUS_APPROVED = "APPROVED";
        private const string STATUS_REJECTED = "REJECTED";
        private const string TYPE_INCOME = "INCOME";
        private const string MEMBER_STATUS_ACTIVE = "ACTIVE";
        private const decimal MinTransactionAmountVnd = 1000m;

        public ClubFundService(
            IFundRepository fundRepository,
            IClubMemberRepository clubMemberRepository,
            IPayOSService payOSService,
            IPolicyService policyService,
            IOptions<PayOSOptions> payOSOptions)
        {
            _fundRepository = fundRepository;
            _clubMemberRepository = clubMemberRepository;
            _payOSService = payOSService;
            _policyService = policyService;
            _payOSOptions = payOSOptions.Value;
        }

        public async Task<FundResponseDto> CreateFundAsync(Guid userId, CreateFundDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FundName))
                throw new ArgumentException("Tên quỹ không được để trống.", nameof(dto.FundName));
            if (dto.InitialAmount < 0)
                throw new ArgumentException("Số tiền ban đầu không được âm.", nameof(dto.InitialAmount));

            DateTime? expiresAtDate = null;
            if (dto.ExpiresAt.HasValue)
            {
                var lastDay = dto.ExpiresAt.Value.Date;
                if (lastDay < DateTime.UtcNow.Date)
                    throw new ArgumentException("Ngày hết hạn nhận nộp tiền phải từ hôm nay trở đi.", nameof(dto.ExpiresAt));
                expiresAtDate = lastDay;
            }

            var member = await _clubMemberRepository.GetMemberAsync(userId, dto.ClubId);
            if (member == null)
                throw new UnauthorizedAccessException("Bạn không phải thành viên của club này.");
            if (!string.Equals(member.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ thành viên đang hoạt động mới được tạo quỹ.");
            if (!HasManagerOrViceLevel(member.ClubRole))
                throw new UnauthorizedAccessException("Chỉ Club Manager hoặc Vice Manager mới được tạo quỹ.");

            var fundStatus = HasHighestClubLevel(member.ClubRole) ? STATUS_APPROVED : STATUS_PENDING;

            var fund = new ClubFund
            {
                ClubId = dto.ClubId,
                FundName = dto.FundName.Trim(),
                TotalAmount = dto.InitialAmount,
                CurrentBalance = dto.InitialAmount,
                CreatedAt = DateTime.UtcNow,
                Status = fundStatus,
                ExpiresAt = expiresAtDate
            };

            var created = await _fundRepository.AddFundAsync(fund);
            return MapToFundDto(created);
        }

        public async Task<ContributeResponseDto> CreateContributionAsync(Guid userId, ContributeRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request.Amount <= 0)
                throw new ArgumentException("Số tiền phải lớn hơn 0.", nameof(request.Amount));
            if (request.Amount < MinTransactionAmountVnd)
                throw new ArgumentException($"Số tiền tối thiểu là {MinTransactionAmountVnd:N0} ₫.", nameof(request.Amount));

            var fund = await _fundRepository.GetFundByIdAsync(request.FundId);
            if (fund == null)
                throw new InvalidOperationException("Quỹ không tồn tại.");
            if (!string.Equals(fund.Status, STATUS_APPROVED, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Chỉ có thể nộp tiền vào quỹ đã được duyệt.");
            if (fund.ExpiresAt.HasValue && DateTime.UtcNow.Date > fund.ExpiresAt.Value.Date)
                throw new InvalidOperationException("Quỹ đã hết hạn nhận nộp tiền.");

            var member = await _clubMemberRepository.GetMemberAsync(userId, fund.ClubId);
            if (member == null)
                throw new UnauthorizedAccessException("Bạn không phải thành viên của club này.");
            if (!string.Equals(member.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ thành viên đang hoạt động mới được nộp tiền vào quỹ.");

            var utc = DateTime.UtcNow;
            var transaction = new FundTransaction
            {
                FundId = request.FundId,
                CategoryId = request.CategoryId,
                TransactionType = TYPE_INCOME,
                Amount = request.Amount,
                Description = request.Description?.Trim() ?? "Nộp quỹ",
                TransactionDate = utc,
                CreatedAt = utc,
                UpdatedAt = utc,
                Status = STATUS_PENDING,
                CreatedBy = userId,
                IsMemberContribution = true
            };

            await _fundRepository.AddTransactionAsync(transaction);

            var payOsResult = await _payOSService.CreatePaymentLinkAsync(
                transaction.TransactionId,
                transaction.Amount,
                transaction.Description ?? "Nop quy",
                cancellationToken);

            transaction.PaymentLinkId = payOsResult.PaymentLinkId;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _fundRepository.UpdateTransactionAsync(transaction);

            var expiresAtUtc = transaction.TransactionDate.AddMinutes(_payOSOptions.LinkExpirationMinutes);
            return new ContributeResponseDto
            {
                TransactionId = transaction.TransactionId,
                CheckoutUrl = payOsResult.CheckoutUrl,
                QrCode = payOsResult.QrCode,
                PaymentLinkId = payOsResult.PaymentLinkId,
                Amount = transaction.Amount,
                PaymentLinkExpiresAtUtc = expiresAtUtc,
                Message = "Quét QR hoặc mở link để thanh toán. Sau khi thanh toán thành công, quỹ sẽ được cập nhật tự động."
            };
        }

        public async Task<ContributionPaymentStatusDto?> GetContributionPaymentStatusAsync(Guid userId, int clubId, int transactionId)
        {
            var t = await _fundRepository.GetTransactionByIdAsync(transactionId);
            if (t == null || !t.IsMemberContribution || !string.Equals(t.TransactionType, TYPE_INCOME, StringComparison.OrdinalIgnoreCase))
                return null;
            if (t.CreatedBy != userId)
                return null;
            if (t.ClubFund == null || t.ClubFund.ClubId != clubId)
                return null;

            return MapContributionPaymentStatus(t);
        }

        public async Task<ContributionPaymentStatusDto?> GetContributionPaymentStatusByOrderCodeAsync(Guid userId, int orderCode)
        {
            var t = await _fundRepository.GetTransactionByIdAsync(orderCode);
            if (t == null || !t.IsMemberContribution || !string.Equals(t.TransactionType, TYPE_INCOME, StringComparison.OrdinalIgnoreCase))
                return null;
            if (t.CreatedBy != userId)
                return null;
            if (t.ClubFund == null)
                return null;

            return MapContributionPaymentStatus(t);
        }

        private ContributionPaymentStatusDto MapContributionPaymentStatus(FundTransaction t)
        {
            var status = t.Status ?? STATUS_PENDING;
            var isPaid = string.Equals(status, STATUS_APPROVED, StringComparison.OrdinalIgnoreCase);
            DateTime? expiresAtUtc = null;
            var linkExpired = false;
            if (string.Equals(status, STATUS_PENDING, StringComparison.OrdinalIgnoreCase))
            {
                expiresAtUtc = t.TransactionDate.AddMinutes(_payOSOptions.LinkExpirationMinutes);
                linkExpired = DateTime.UtcNow > expiresAtUtc.Value;
            }

            var message = isPaid
                ? "Thanh toán thành công, quỹ đã được cập nhật."
                : linkExpired
                    ? "Link thanh toán đã hết hạn. Hãy tạo yêu cầu nộp tiền mới."
                    : "Đang chờ thanh toán. Sau khi hoàn tất, trạng thái sẽ chuyển thành công (có thể trễ vài giây so với webhook).";

            return new ContributionPaymentStatusDto
            {
                ClubId = t.ClubFund!.ClubId,
                TransactionId = t.TransactionId,
                FundId = t.FundId,
                Status = status,
                Amount = t.Amount,
                IsPaid = isPaid,
                IsPaymentLinkExpired = linkExpired,
                PaymentLinkExpiresAtUtc = expiresAtUtc,
                Message = message
            };
        }

        public async Task<FundResponseDto?> GetFundByIdAsync(int fundId)
        {
            var fund = await _fundRepository.GetFundByIdAsync(fundId);
            return fund == null ? null : MapToFundDto(fund);
        }

        public async Task<IEnumerable<FundResponseDto>> GetFundsByClubIdAsync(int clubId)
        {
            var funds = await _fundRepository.GetFundsByClubIdAsync(clubId);
            return funds.Select(MapToFundDto);
        }

        public async Task<IEnumerable<FundTransactionResponseDto>> GetFundHistoryAsync(int fundId, string? status, string? scope, Guid? currentUserId)
        {
            var trimmed = status?.Trim();
            string? normalizedStatus;
            if (string.IsNullOrEmpty(trimmed))
                normalizedStatus = STATUS_APPROVED;
            else if (string.Equals(trimmed, "ALL", StringComparison.OrdinalIgnoreCase))
                normalizedStatus = null;
            else
                normalizedStatus = trimmed.ToUpperInvariant();

            var normalizedScope = scope?.Trim().ToLowerInvariant();
            Guid? filterUser = normalizedScope == "mine" ? currentUserId : null;

            var list = await _fundRepository.GetTransactionsByFundIdAsync(
                fundId,
                normalizedStatus,
                memberContributionsOnly: true,
                filterUser);
            return list.Select(MapToTransactionDto);
        }

        private static FundResponseDto MapToFundDto(ClubFund fund)
        {
            var approved = string.Equals(fund.Status, STATUS_APPROVED, StringComparison.OrdinalIgnoreCase);
            var notExpired = !fund.ExpiresAt.HasValue || DateTime.UtcNow.Date <= fund.ExpiresAt.Value.Date;
            return new FundResponseDto
            {
                FundId = fund.FundId,
                ClubId = fund.ClubId,
                FundName = fund.FundName,
                TotalAmount = fund.TotalAmount,
                CurrentBalance = fund.CurrentBalance,
                CreatedAt = fund.CreatedAt,
                Status = fund.Status ?? "PENDING",
                ExpiresAt = fund.ExpiresAt,
                CanAcceptContributions = approved && notExpired
            };
        }

        private static FundTransactionResponseDto MapToTransactionDto(FundTransaction t)
        {
            var createdAt = t.CreatedAt != default ? t.CreatedAt : t.TransactionDate;
            var updatedAt = t.UpdatedAt != default ? t.UpdatedAt : t.TransactionDate;
            var displayName = string.IsNullOrWhiteSpace(t.Creator?.FullName)
                ? null
                : t.Creator!.FullName.Trim();

            return new FundTransactionResponseDto
            {
                TransactionId = t.TransactionId,
                FundId = t.FundId,
                CategoryId = t.CategoryId,
                TransactionType = t.TransactionType ?? "INCOME",
                Status = t.Status ?? "PENDING",
                Amount = t.Amount,
                Description = t.Description,
                TransactionDate = t.TransactionDate,
                CreatedBy = t.CreatedBy,
                ApprovedBy = t.ApprovedBy,
                PaymentLinkId = t.PaymentLinkId,
                IsMemberContribution = t.IsMemberContribution,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                MemberName = displayName,
                ContributorName = displayName,
                UserFullName = displayName
            };
        }

        private static bool HasManagerOrViceLevel(ClubRole? clubRole)
        {
            if (clubRole == null) return false;
            return clubRole.Level == 1 || clubRole.Level == 2;
        }

        private static bool HasHighestClubLevel(ClubRole? clubRole)
        {
            if (clubRole == null) return false;
            return clubRole.Level == 1;
        }

        public async Task<bool> ProcessPayOSPaymentSuccessAsync(int orderCode)
        {
            var transaction = await _fundRepository.GetTransactionByIdAsync(orderCode);
            if (transaction == null)
                return false;
            if (!transaction.IsMemberContribution || transaction.TransactionType != TYPE_INCOME || transaction.Status != STATUS_PENDING)
                return false;

            var fund = transaction.ClubFund;
            if (fund == null)
                return false;

            var utc = DateTime.UtcNow;
            transaction.Status = STATUS_APPROVED;
            transaction.TransactionDate = utc;
            transaction.UpdatedAt = utc;
            fund.CurrentBalance += transaction.Amount;
            fund.TotalAmount += transaction.Amount;

            await _fundRepository.UpdateTransactionAndFundAsync(transaction, fund);
            return true;
        }

        public async Task<bool> TryCompleteOwnPendingContributionForDevelopmentAsync(Guid userId, int clubId, int transactionId)
        {
            var transaction = await _fundRepository.GetTransactionByIdAsync(transactionId);
            if (transaction == null)
                return false;
            if (transaction.CreatedBy != userId)
                return false;
            if (transaction.ClubFund == null || transaction.ClubFund.ClubId != clubId)
                return false;
            if (!transaction.IsMemberContribution || !string.Equals(transaction.TransactionType, TYPE_INCOME, StringComparison.OrdinalIgnoreCase) || transaction.Status != STATUS_PENDING)
                return false;

            return await ProcessPayOSPaymentSuccessAsync(transactionId);
        }

        public async Task<bool> ApproveFundAsync(Guid managerId, ApproveFundDto dto)
        {
            var fund = await _fundRepository.GetFundByIdAsync(dto.FundId);
            if (fund == null)
                throw new InvalidOperationException("Quỹ không tồn tại.");
            if (string.Equals(fund.Status, STATUS_APPROVED, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Quỹ đã được duyệt trước đó.");
            if (string.Equals(fund.Status, STATUS_REJECTED, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Quỹ đã bị từ chối, không thể duyệt.");

            var member = await _clubMemberRepository.GetMemberAsync(managerId, fund.ClubId);
            if (member == null)
                throw new UnauthorizedAccessException("Bạn không phải thành viên của club này.");
            if (!string.Equals(member.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ thành viên đang hoạt động mới được duyệt quỹ.");
            if (!HasHighestClubLevel(member.ClubRole))
                throw new UnauthorizedAccessException("Chỉ Club Manager (role có Level cao nhất trong club) mới được duyệt hoặc từ chối quỹ.");

            var action = dto.Action?.Trim().ToUpperInvariant();
            if (action == "APPROVE")
                fund.Status = STATUS_APPROVED;
            else if (action == "REJECT")
                fund.Status = STATUS_REJECTED;
            else
                throw new ArgumentException("Hành động phải là APPROVE hoặc REJECT.", nameof(dto.Action));

            await _fundRepository.UpdateClubFundAsync(fund);
            return true;
        }

        public async Task<FundCapabilitiesDto> GetFundCapabilitiesAsync(Guid userId, int clubId)
        {
            var dto = new FundCapabilitiesDto { ClubId = clubId };
            var member = await _clubMemberRepository.GetMemberAsync(userId, clubId);
            if (member == null)
                return dto;

            dto.IsActiveClubMember = string.Equals(member.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase);
            dto.ClubRoleLevel = member.ClubRole?.Level;
            dto.ClubRoleName = member.ClubRole?.RoleName;

            if (!dto.IsActiveClubMember)
                return dto;

            var hasView = await _policyService.HasMemberPolicyInClubAsync(userId, clubId, "viewfinance");
            var hasCreate = await _policyService.HasMemberPolicyInClubAsync(userId, clubId, "createfinance");
            var hasEdit = await _policyService.HasMemberPolicyInClubAsync(userId, clubId, "editfinance");

            dto.HasViewFinancePolicy = hasView;
            dto.HasCreateFinancePolicy = hasCreate;
            dto.HasEditFinancePolicy = hasEdit;

            var isMgrOrVice = HasManagerOrViceLevel(member.ClubRole);
            var isMgr = HasHighestClubLevel(member.ClubRole);

            dto.CanViewFunds = hasView;
            dto.CanContribute = true;
            dto.CanCreateFund = hasCreate && isMgrOrVice;
            dto.CanApproveOrRejectFundEntity = hasEdit && isMgr;

            return dto;
        }
    }
}