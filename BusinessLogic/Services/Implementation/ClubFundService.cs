using System;
using BusinessLogic.DTOs;
using BusinessLogic.Options;
using BusinessLogic.PaymentGateways;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.Extensions.Options;
using UNIC.DataAccess.Repositories.Interface;
using System.Linq;
using System.Collections.Generic;

namespace BusinessLogic.Services.Implementation
{
    public class ClubFundService : IClubFundService
    {
        private readonly IFundRepository _fundRepository;
        private readonly IClubMemberRepository _clubMemberRepository;
        private readonly IFundPaymentGatewayRegistry _paymentGatewayRegistry;
        private readonly IPolicyService _policyService;
        private readonly IClubPayOSSettingsRepository _clubPayOSSettingsRepository;
        private readonly IFundTypeRepository _fundTypeRepository;
        private readonly PayOSOptions _payOSOptions;
        private const string STATUS_PENDING = "PENDING";
        private const string STATUS_APPROVED = "APPROVED";
        private const string STATUS_REJECTED = "REJECTED";
        private const string TYPE_INCOME = "INCOME";
        private const string MEMBER_STATUS_ACTIVE = "ACTIVE";
        private const decimal MinTransactionAmountVnd = 10000m;
        private const string CONTRIBUTION_SOURCE_CASH = "CASH";

        public ClubFundService(
            IFundRepository fundRepository,
            IClubMemberRepository clubMemberRepository,
            IFundPaymentGatewayRegistry paymentGatewayRegistry,
            IPolicyService policyService,
            IClubPayOSSettingsRepository clubPayOSSettingsRepository,
            IFundTypeRepository fundTypeRepository,
            IOptions<PayOSOptions> payOSOptions)
        {
            _fundRepository = fundRepository;
            _clubMemberRepository = clubMemberRepository;
            _paymentGatewayRegistry = paymentGatewayRegistry;
            _policyService = policyService;
            _clubPayOSSettingsRepository = clubPayOSSettingsRepository;
            _fundTypeRepository = fundTypeRepository;
            _payOSOptions = payOSOptions.Value;
        }

        public async Task<FundResponseDto> CreateFundAsync(Guid userId, CreateFundDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FundName))
                throw new ArgumentException("Tên quỹ không được để trống.", nameof(dto.FundName));
            var fundName = dto.FundName.Trim();
            if (await _fundRepository.ExistsNonRejectedFundNameInClubAsync(dto.ClubId, fundName))
                throw new ArgumentException("Tên quỹ đã tồn tại trong câu lạc bộ này.", nameof(dto.FundName));

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
                throw new UnauthorizedAccessException("Bạn không phải thành viên của câu lạc bộ này.");
            if (!string.Equals(member.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ thành viên đang hoạt động mới được tạo quỹ.");
            if (!HasManagerOrViceLevel(member.ClubRole))
                throw new UnauthorizedAccessException("Chỉ Club Manager hoặc Vice Manager mới được tạo quỹ.");

            var fundStatus = HasHighestClubLevel(member.ClubRole) ? STATUS_APPROVED : STATUS_PENDING;

            if (dto.FundTypeId <= 0)
                throw new ArgumentException("Bắt buộc chọn loại quỹ.", nameof(dto.FundTypeId));
            var fundType = await _fundTypeRepository.GetByIdAsync(dto.FundTypeId);
            if (fundType == null || !fundType.IsActive)
                throw new ArgumentException("Loại quỹ không hợp lệ hoặc đã bị tắt.", nameof(dto.FundTypeId));

            if (dto.GoalAmount.HasValue && dto.GoalAmount.Value < 0)
                throw new ArgumentException("Mục tiêu quỹ phải >= 0.", nameof(dto.GoalAmount));

            var fund = new ClubFund
            {
                ClubId = dto.ClubId,
                FundName = fundName,
                Description = dto.ResolveDescription(),
                FundTypeId = fundType.FundTypeId,
                GoalAmount = dto.GoalAmount,
                TotalAmount = 0m,
                CurrentBalance = 0m,
                CreatedAt = DateTime.UtcNow,
                Status = fundStatus,
                ExpiresAt = expiresAtDate,
                CreatedBy = userId,
                ApprovedBy = fundStatus == STATUS_APPROVED ? userId : null
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

            if (request.CategoryId.HasValue)
            {
                var category = await _fundRepository.GetFundCategoryByIdAsync(request.CategoryId.Value);
                if (category == null)
                    throw new ArgumentException("Danh mục không tồn tại.", nameof(request.CategoryId));
                if (category.ClubId.HasValue && category.ClubId.Value != fund.ClubId)
                    throw new ArgumentException("Danh mục không thuộc câu lạc bộ của quỹ này.", nameof(request.CategoryId));
            }

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

            try
            {
                ClubPayOSSettings settingsForGateway;
                if (_payOSOptions.UseMock)
                {
                    settingsForGateway = new ClubPayOSSettings
                    {
                        ClubId = fund.ClubId,
                        PaymentProvider = PaymentGatewayProviderCodes.PayOS,
                        ClientId = "mock",
                        ApiKey = "mock",
                        ChecksumKey = "mock",
                        IsEnabled = true
                    };
                }
                else
                {
                    var s = await _clubPayOSSettingsRepository.GetByClubIdAsync(fund.ClubId);
                    if (s == null || !s.IsEnabled)
                        throw new InvalidOperationException("Câu lạc bộ chưa liên kết cổng thanh toán trực tuyến hoặc đã tắt liên kết.");
                    settingsForGateway = s;
                }

                var providerCode = PaymentGatewayProviderCodes.Normalize(settingsForGateway.PaymentProvider);
                var gateway = _paymentGatewayRegistry.Get(providerCode);
                gateway.ValidateForCreatingPaymentLink(settingsForGateway);

                var payResult = await gateway.CreatePaymentLinkAsync(
                    settingsForGateway,
                    transaction.TransactionId,
                    transaction.Amount,
                    transaction.Description ?? "Nộp quỹ",
                    cancellationToken);

                transaction.PaymentProvider = providerCode;
                transaction.PaymentLinkId = payResult.ExternalPaymentId;
                transaction.UpdatedAt = DateTime.UtcNow;
                await _fundRepository.UpdateTransactionAsync(transaction);

                var expiresAtUtc = transaction.TransactionDate.AddMinutes(_payOSOptions.LinkExpirationMinutes);
                return new ContributeResponseDto
                {
                    TransactionId = transaction.TransactionId,
                    CheckoutUrl = payResult.CheckoutUrl,
                    QrCode = payResult.QrCode,
                    PaymentLinkId = payResult.ExternalPaymentId,
                    Amount = transaction.Amount,
                    PaymentLinkExpiresAtUtc = expiresAtUtc,
                    Message = "Quét QR hoặc mở link để thanh toán. Sau khi thanh toán thành công, quỹ sẽ được cập nhật tự động."
                };
            }
            catch
            {
                await _fundRepository.DeleteTransactionByIdAsync(transaction.TransactionId);
                throw;
            }
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

        public async Task<PagedResultDto<FundResponseDto>> GetFundsByClubIdPagedAsync(
            int clubId,
            Guid currentUserId,
            bool isSystemAdmin,
            string? status,
            string? search,
            string? sort,
            int pageNumber,
            int pageSize)
        {
            bool canFilterByWorkflowStatus;
            if (isSystemAdmin)
            {
                canFilterByWorkflowStatus = true;
            }
            else
            {
                var member = await _clubMemberRepository.GetMemberAsync(currentUserId, clubId);
                var hasEditFinance = await _policyService.HasMemberPolicyInClubAsync(currentUserId, clubId, "editfinance");
                canFilterByWorkflowStatus = hasEditFinance && member != null && HasHighestClubLevel(member.ClubRole);
            }

            var statusForNormalization = (!canFilterByWorkflowStatus) ? STATUS_APPROVED : status;

            string? normalizedStatus;
            var trimmedStatus = statusForNormalization?.Trim();
            if (string.IsNullOrEmpty(trimmedStatus) || string.Equals(trimmedStatus, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                normalizedStatus = null;
            }
            else
            {
                var upper = trimmedStatus.ToUpperInvariant();
                if (upper != STATUS_PENDING && upper != STATUS_APPROVED && upper != STATUS_REJECTED)
                    throw new ArgumentException("Trạng thái hợp lệ: PENDING, APPROVED, REJECTED hoặc ALL.", nameof(status));
                normalizedStatus = upper;
            }

            var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            var normalizedSort = string.IsNullOrWhiteSpace(sort) ? "NEWEST" : sort.Trim().ToUpperInvariant();
            if (normalizedSort is not ("NEWEST" or "OLDEST" or "NAME_ASC" or "NAME_DESC"))
                throw new ArgumentException("Sắp xếp hợp lệ: NEWEST, OLDEST, NAME_ASC, NAME_DESC.", nameof(sort));
            var (items, totalCount) = await _fundRepository.GetFundsByClubIdPagedAsync(
                clubId,
                normalizedStatus,
                normalizedSearch,
                normalizedSort,
                pageNumber,
                pageSize);
            return ToPagedResult(items.Select(MapToFundDto), pageNumber, pageSize, totalCount);
        }

        public async Task<PagedResultDto<FundResponseDto>> GetMyFundsByClubIdPagedAsync(
            int clubId,
            Guid currentUserId,
            string? mineType,
            string? status,
            string? search,
            string? sort,
            int pageNumber,
            int pageSize)
        {
            var normalizedMineType = string.IsNullOrWhiteSpace(mineType) ? "CREATED" : mineType.Trim().ToUpperInvariant();
            if (normalizedMineType is not ("ALL" or "CREATED" or "RESPONSIBLE"))
                throw new ArgumentException(
                    "mineType hợp lệ: CREATED, ALL, RESPONSIBLE. Mặc định (bỏ trống): CREATED — chỉ quỹ do bạn tạo. " +
                    "ALL — quỹ có liên quan: bạn tạo, bạn duyệt/từ chối (ApprovedBy), hoặc có giao dịch bạn tạo/duyệt. " +
                    "RESPONSIBLE — quỹ mà bạn là người duyệt/từ chối gần nhất (ApprovedBy).",
                    nameof(mineType));

            string? normalizedStatus;
            var trimmedStatus = status?.Trim();
            if (string.IsNullOrEmpty(trimmedStatus) || string.Equals(trimmedStatus, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                normalizedStatus = null;
            }
            else
            {
                var upper = trimmedStatus.ToUpperInvariant();
                if (upper != STATUS_PENDING && upper != STATUS_APPROVED && upper != STATUS_REJECTED)
                    throw new ArgumentException("Trạng thái hợp lệ: PENDING, APPROVED, REJECTED hoặc ALL.", nameof(status));
                normalizedStatus = upper;
            }

            var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            var normalizedSort = string.IsNullOrWhiteSpace(sort) ? "NEWEST" : sort.Trim().ToUpperInvariant();
            if (normalizedSort is not ("NEWEST" or "OLDEST" or "NAME_ASC" or "NAME_DESC"))
                throw new ArgumentException("Sắp xếp hợp lệ: NEWEST, OLDEST, NAME_ASC, NAME_DESC.", nameof(sort));

            var (items, totalCount) = await _fundRepository.GetMyFundsByClubIdPagedAsync(
                clubId,
                currentUserId,
                normalizedMineType,
                normalizedStatus,
                normalizedSearch,
                normalizedSort,
                pageNumber,
                pageSize);

            return ToPagedResult(items.Select(MapToFundDto), pageNumber, pageSize, totalCount);
        }

        public async Task<PagedResultDto<FundTransactionResponseDto>> GetFundHistoryPagedAsync(
            int fundId, string? status, string? scope, Guid? currentUserId, int pageNumber, int pageSize)
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

            var (items, totalCount) = await _fundRepository.GetTransactionsByFundIdPagedAsync(
                fundId,
                normalizedStatus,
                memberContributionsOnly: true,
                filterUser,
                pageNumber,
                pageSize);

            return ToPagedResult(items.Select(MapToTransactionDto), pageNumber, pageSize, totalCount);
        }

        public async Task<PagedResultDto<FundTransactionResponseDto>> GetClubFundTransactionsPagedAsync(
            int clubId,
            int? fundId,
            string? status,
            string? scope,
            Guid currentUserId,
            DateTime? fromUtc,
            DateTime? toUtc,
            int pageNumber,
            int pageSize)
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

            var (items, totalCount) = await _fundRepository.GetTransactionsByClubIdPagedAsync(
                clubId,
                fundId,
                normalizedStatus,
                memberContributionsOnly: false,
                filterUser,
                fromUtc,
                toUtc,
                pageNumber,
                pageSize);

            return ToPagedResult(items.Select(MapToTransactionDto), pageNumber, pageSize, totalCount);
        }

        private static PagedResultDto<T> ToPagedResult<T>(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
        {
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
            return new PagedResultDto<T>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < totalPages
            };
        }

        private static FundResponseDto MapToFundDto(ClubFund fund)
        {
            var status = string.IsNullOrWhiteSpace(fund.Status) ? STATUS_PENDING : fund.Status.Trim();
            var approved = string.Equals(status, STATUS_APPROVED, StringComparison.OrdinalIgnoreCase);
            var rejected = string.Equals(status, STATUS_REJECTED, StringComparison.OrdinalIgnoreCase);
            var pending = string.Equals(status, STATUS_PENDING, StringComparison.OrdinalIgnoreCase);
            var notExpired = !fund.ExpiresAt.HasValue || DateTime.UtcNow.Date <= fund.ExpiresAt.Value.Date;
            var canContribute = approved && notExpired;

            string? cannotContributeReason = null;
            if (!canContribute)
            {
                if (rejected)
                    cannotContributeReason = "Quỹ đã bị từ chối.";
                else if (pending)
                    cannotContributeReason = "Quỹ đang chờ duyệt.";
                else if (approved && !notExpired)
                    cannotContributeReason = "Đã quá hạn nhận nộp tiền.";
                else
                    cannotContributeReason = "Quỹ chưa được duyệt hoặc không thể nhận nộp ở trạng thái hiện tại.";
            }

            string? balanceContext = null;
            if (rejected)
                balanceContext = "Quỹ không hoạt động.";
            else if (pending)
                balanceContext = "Quỹ đang chờ duyệt — số dư sẽ cập nhật sau khi được duyệt.";
            else if (approved && fund.CurrentBalance == 0m && fund.TotalAmount == 0m)
                balanceContext = "Chưa có giao dịch thu/chi được duyệt (số 0 là bình thường nếu chưa phát sinh).";

            return new FundResponseDto
            {
                FundId = fund.FundId,
                ClubId = fund.ClubId,
                FundName = fund.FundName,
                Description = fund.Description,
                FundTypeId = fund.FundTypeId,
                FundTypeName = fund.FundType?.Name,
                GoalAmount = fund.GoalAmount,
                TotalAmount = fund.TotalAmount,
                CurrentBalance = fund.CurrentBalance,
                CreatedAt = fund.CreatedAt,
                Status = status,
                ExpiresAt = fund.ExpiresAt,
                RejectReason = fund.RejectReason,
                RejectedAt = fund.RejectedAt,
                RejectionReasonVi = rejected ? fund.RejectReason : null,
                CanAcceptContributions = canContribute,
                CannotContributeReasonVi = cannotContributeReason,
                BalanceContextVi = balanceContext,
                ExpiresAtUtcNoteVi = fund.ExpiresAt.HasValue
                    ? "Hạn nhận nộp theo ngày lưu trong DB (UTC, so với ngày hiện tại của máy chủ)."
                    : null
            };
        }

        private static FundTransactionResponseDto MapToTransactionDto(FundTransaction t)
        {
            var createdAt = t.CreatedAt != default ? t.CreatedAt : t.TransactionDate;
            var updatedAt = t.UpdatedAt != default ? t.UpdatedAt : t.TransactionDate;
            var displayName = string.IsNullOrWhiteSpace(t.Creator?.FullName)
                ? null
                : t.Creator!.FullName.Trim();

            var fundName = string.IsNullOrWhiteSpace(t.ClubFund?.FundName)
                ? null
                : t.ClubFund!.FundName.Trim();

            return new FundTransactionResponseDto
            {
                TransactionId = t.TransactionId,
                FundId = t.FundId,
                FundName = fundName,
                CategoryId = t.CategoryId,
                TransactionType = t.TransactionType ?? "INCOME",
                Status = t.Status ?? "PENDING",
                Amount = t.Amount,
                Description = t.Description,
                TransactionDate = t.TransactionDate,
                CreatedBy = t.CreatedBy,
                ApprovedBy = t.ApprovedBy,
                PaymentLinkId = t.PaymentLinkId,
                PaymentProvider = t.PaymentProvider,
                ContributionSource = t.ContributionSource,
                RefundForTransactionId = t.RefundForTransactionId,
                IsMemberContribution = t.IsMemberContribution,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                MemberName = displayName,
                ContributorName = displayName,
                UserFullName = displayName,
                CategoryName = string.IsNullOrWhiteSpace(t.FundCategory?.CategoryName)
                    ? null
                    : t.FundCategory!.CategoryName.Trim()
            };
        }

        public async Task<FundMemberContributionOverviewDto> GetFundMemberContributionOverviewAsync(
            Guid currentUserId, int clubId, int fundId)
        {
            var fund = await _fundRepository.GetFundByIdAsync(fundId);
            if (fund == null)
                throw new InvalidOperationException("Quỹ không tồn tại.");
            if (fund.ClubId != clubId)
                throw new UnauthorizedAccessException("Quỹ không thuộc câu lạc bộ này.");

            var member = await _clubMemberRepository.GetMemberAsync(currentUserId, clubId);
            if (member == null)
                throw new UnauthorizedAccessException("Bạn không phải thành viên của club này.");
            if (!string.Equals(member.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ thành viên đang hoạt động mới được xem danh sách đóng góp.");

            var members = (await _clubMemberRepository.GetMembersByClubIdAsync(clubId))
                .Where(m => string.Equals(m.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var activeCount = members.Count;
            var goal = fund.GoalAmount;

            decimal? requiredPerMember = null;
            if (goal.HasValue && goal.Value > 0 && activeCount > 0)
                requiredPerMember = RoundUpVnd(goal.Value / activeCount);

            var approvedMemberTx = await _fundRepository.GetTransactionsByFundIdAsync(
                fundId,
                status: STATUS_APPROVED,
                memberContributionsOnly: true,
                createdByUserId: null);

            var paidByUser = approvedMemberTx
                .Where(t => t.CreatedBy.HasValue)
                .GroupBy(t => t.CreatedBy!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var memberDtos = members
                .Select(m =>
                {
                    var uid = m.UserId;
                    paidByUser.TryGetValue(uid, out var paid);

                    decimal? remaining = null;
                    var isPaidEnough = false;
                    if (requiredPerMember.HasValue)
                    {
                        remaining = Math.Max(0, requiredPerMember.Value - paid);
                        isPaidEnough = paid >= requiredPerMember.Value;
                    }

                    return new FundMemberContributionStatusDto
                    {
                        UserId = uid,
                        FullName = m.User?.FullName,
                        Email = m.User?.Email,
                        Status = m.Status ?? MEMBER_STATUS_ACTIVE,
                        PaidAmount = paid,
                        RequiredAmount = requiredPerMember,
                        RemainingAmount = remaining,
                        IsPaidEnough = isPaidEnough
                    };
                })
                .OrderBy(x => x.IsPaidEnough)
                .ThenBy(x => x.FullName)
                .ToList();

            return new FundMemberContributionOverviewDto
            {
                ClubId = clubId,
                FundId = fund.FundId,
                FundName = fund.FundName ?? string.Empty,
                FundTypeId = fund.FundTypeId,
                FundTypeName = fund.FundType?.Name,
                GoalAmount = goal,
                ActiveMemberCount = activeCount,
                RequiredPerMember = requiredPerMember,
                TotalApprovedMemberContributions = approvedMemberTx.Sum(x => x.Amount),
                Members = memberDtos
            };
        }

        private static decimal RoundUpVnd(decimal value)
        {
            if (value <= 0) return 0;
            return Math.Ceiling(value);
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
            return await _fundRepository.TryApproveMemberContributionAsync(orderCode);
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
            const int rejectReasonMinLen = 5;
            const int rejectReasonMaxLen = 2000;

            var fund = await _fundRepository.GetFundByIdAsync(dto.FundId);
            if (fund == null)
                throw new InvalidOperationException("Quỹ không tồn tại.");

            var st = string.IsNullOrWhiteSpace(fund.Status) ? STATUS_PENDING : fund.Status.Trim();
            if (string.Equals(st, STATUS_APPROVED, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Quỹ đã được duyệt trước đó.");
            if (string.Equals(st, STATUS_REJECTED, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Quỹ đã bị từ chối, không thể duyệt.");
            if (!string.Equals(st, STATUS_PENDING, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Chỉ có thể duyệt hoặc từ chối quỹ đang chờ duyệt (PENDING).");

            var member = await _clubMemberRepository.GetMemberAsync(managerId, fund.ClubId);
            if (member == null)
                throw new UnauthorizedAccessException("Bạn không phải thành viên của club này.");
            if (!string.Equals(member.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ thành viên đang hoạt động mới được duyệt quỹ.");
            if (!HasHighestClubLevel(member.ClubRole))
                throw new UnauthorizedAccessException("Chỉ Club Manager (role có Level cao nhất trong club) mới được duyệt hoặc từ chối quỹ.");

            var action = dto.Action?.Trim().ToUpperInvariant();
            if (action == "APPROVE")
            {
                fund.Status = STATUS_APPROVED;
                fund.RejectReason = null;
                fund.RejectedAt = null;
            }
            else if (action == "REJECT")
            {
                var reason = dto.ResolveRejectReason();
                if (string.IsNullOrEmpty(reason))
                    throw new ArgumentException("Khi từ chối quỹ, bắt buộc nhập lý do (rejectReason).", nameof(dto.RejectReason));
                if (reason.Length < rejectReasonMinLen)
                    throw new ArgumentException(
                        $"Lý do từ chối phải có ít nhất {rejectReasonMinLen} ký tự sau khi bỏ khoảng trắng đầu cuối.",
                        nameof(dto.RejectReason));
                if (reason.Length > rejectReasonMaxLen)
                    throw new ArgumentException($"Lý do từ chối không được vượt quá {rejectReasonMaxLen} ký tự.", nameof(dto.RejectReason));

                fund.Status = STATUS_REJECTED;
                fund.RejectReason = reason;
                fund.RejectedAt = DateTime.UtcNow;
            }
            else
                throw new ArgumentException("Hành động phải là APPROVE hoặc REJECT.", nameof(dto.Action));

            fund.ApprovedBy = managerId;

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
            {
                dto.FinanceAccessHintVi =
                    "Tài khoản thành viên chưa ở trạng thái hoạt động — các quyền tài chính và thao tác quỹ có thể bị hạn chế.";
                return dto;
            }

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

            if (!hasView)
                dto.FinanceAccessHintVi =
                    "Bạn chưa có quyền xem tài chính CLB (policy viewfinance). Liên hệ quản lý CLB nếu cần cấp quyền.";
            else if (!hasEdit && isMgr)
                dto.FinanceAccessHintVi =
                    "Bạn chưa có quyền duyệt quỹ (policy editfinance). Liên hệ quản lý CLB nếu cần cấp quyền.";
            else if (!hasCreate && isMgrOrVice)
                dto.FinanceAccessHintVi =
                    "Bạn chưa có quyền tạo quỹ (policy createfinance). Liên hệ quản lý CLB nếu cần cấp quyền.";

            dto.MenuItems = BuildFundMenuItems(dto);
            return dto;
        }

        public async Task<ClubFundReportSummaryDto> GetClubFundReportSummaryAsync(int clubId, DateTime? fromUtc, DateTime? toUtc)
        {
            var (pending, approved, rejected, balance, income, expense) =
                await _fundRepository.GetClubFundReportAggregatesAsync(clubId, fromUtc, toUtc);
            return new ClubFundReportSummaryDto
            {
                ClubId = clubId,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                PendingFundCount = pending,
                ApprovedFundCount = approved,
                RejectedFundCount = rejected,
                TotalBalanceApprovedFunds = balance,
                TotalApprovedIncome = income,
                TotalApprovedExpense = expense
            };
        }

        private static IReadOnlyList<FundMenuItemDto> BuildFundMenuItems(FundCapabilitiesDto caps)
        {
            if (!caps.CanViewFunds)
                return Array.Empty<FundMenuItemDto>();

            return new FundMenuItemDto[]
            {
                new()
                {
                    Id = "overview",
                    LabelVi = "Tổng quan quỹ",
                    LabelEn = "Fund overview",
                    Visible = true
                },
                new()
                {
                    Id = "my-funds",
                    LabelVi = "Quỹ của tôi",
                    LabelEn = "My funds",
                    Visible = true
                },
                new()
                {
                    Id = "transactions",
                    LabelVi = "Giao dịch",
                    LabelEn = "Transactions",
                    Visible = true
                },
                new()
                {
                    Id = "reports",
                    LabelVi = "Báo cáo & thống kê",
                    LabelEn = "Reports & statistics",
                    Visible = true
                },
                new()
                {
                    Id = "settings",
                    LabelVi = "Cài đặt quỹ",
                    LabelEn = "Fund settings",
                    Visible = true
                }
            };
        }

        public async Task<IReadOnlyList<FundCategoryResponseDto>> GetFundCategoriesForClubAsync(int clubId)
        {
            var list = await _fundRepository.GetFundCategoriesForClubAsync(clubId);
            return list.Select(c => new FundCategoryResponseDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Description = c.Description,
                ClubId = c.ClubId
            }).ToList();
        }

        public async Task<RecordCashContributionResponseDto> RecordCashContributionAsync(
            Guid managerUserId,
            int clubId,
            bool isSystemAdmin,
            RecordCashContributionRequestDto dto)
        {
            await EnsureClubManagerLevel1OrAdminAsync(managerUserId, clubId, isSystemAdmin);

            if (dto.Amount < MinTransactionAmountVnd)
                throw new ArgumentException($"Số tiền tối thiểu là {MinTransactionAmountVnd:N0} ₫.", nameof(dto.Amount));

            var note = (dto.Note ?? string.Empty).Trim();
            if (note.Length < 3)
                throw new ArgumentException("Ghi chú phải có ít nhất 3 ký tự.", nameof(dto.Note));

            var fund = await _fundRepository.GetFundByIdAsync(dto.FundId);
            if (fund == null)
                throw new KeyNotFoundException("Quỹ không tồn tại.");
            if (fund.ClubId != clubId)
                throw new ArgumentException("Quỹ không thuộc câu lạc bộ này.", nameof(dto.FundId));
            if (!string.Equals(fund.Status, STATUS_APPROVED, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Chỉ có thể ghi nhận đóng góp vào quỹ đã được duyệt.");
            if (fund.ExpiresAt.HasValue && DateTime.UtcNow.Date > fund.ExpiresAt.Value.Date)
                throw new InvalidOperationException("Quỹ đã hết hạn nhận nộp tiền.");

            var contributor = await _clubMemberRepository.GetMemberAsync(dto.ContributorUserId, clubId);
            if (contributor == null)
                throw new ArgumentException("Thành viên không thuộc câu lạc bộ này.", nameof(dto.ContributorUserId));
            if (!string.Equals(contributor.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Chỉ có thể ghi nhận đóng góp cho thành viên đang hoạt động.", nameof(dto.ContributorUserId));

            if (dto.CategoryId.HasValue)
            {
                var category = await _fundRepository.GetFundCategoryByIdAsync(dto.CategoryId.Value);
                if (category == null)
                    throw new ArgumentException("Danh mục không tồn tại.", nameof(dto.CategoryId));
                if (category.ClubId.HasValue && category.ClubId.Value != clubId)
                    throw new ArgumentException("Danh mục không thuộc câu lạc bộ của quỹ này.", nameof(dto.CategoryId));
            }

            DateTime transactionDateUtc;
            if (dto.ContributedAtUtc.HasValue)
            {
                var d = dto.ContributedAtUtc.Value;
                transactionDateUtc = d.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(d, DateTimeKind.Utc)
                    : d.ToUniversalTime();
            }
            else
            {
                transactionDateUtc = DateTime.UtcNow;
            }

            var description = $"[Tiền mặt] {note}";

            var result = await _fundRepository.TryRecordApprovedCashIncomeAsync(
                clubId,
                dto.FundId,
                dto.ContributorUserId,
                managerUserId,
                dto.Amount,
                description,
                transactionDateUtc,
                dto.CategoryId,
                CONTRIBUTION_SOURCE_CASH);

            if (result == null)
                throw new InvalidOperationException("Không thể ghi nhận giao dịch (quỹ không hợp lệ hoặc không thuộc CLB).");

            var (transactionId, newBalance) = result.Value;
            return new RecordCashContributionResponseDto
            {
                TransactionId = transactionId,
                FundId = dto.FundId,
                Amount = dto.Amount,
                Status = STATUS_APPROVED,
                ContributionSource = CONTRIBUTION_SOURCE_CASH,
                NewCurrentBalance = newBalance,
                ContributorUserId = dto.ContributorUserId,
                RecordedByUserId = managerUserId
            };
        }

        public async Task<FundTransactionResponseDto> ManagerRefundContributionAsync(
            Guid managerUserId, int clubId, int fundId, bool isSystemAdmin, ManagerRefundContributionDto dto)
        {
            await EnsureClubManagerLevel1OrAdminAsync(managerUserId, clubId, isSystemAdmin);

            if (dto.OriginalTransactionId <= 0)
                throw new ArgumentException("OriginalTransactionId không hợp lệ.", nameof(dto.OriginalTransactionId));
            if (dto.Amount <= 0m)
                throw new ArgumentException("Số tiền hoàn phải > 0.", nameof(dto.Amount));

            var fund = await _fundRepository.GetFundByIdAsync(fundId);
            if (fund == null)
                throw new KeyNotFoundException("Quỹ không tồn tại.");
            if (fund.ClubId != clubId)
                throw new ArgumentException("Quỹ không thuộc câu lạc bộ này.", nameof(fundId));

            var result = await _fundRepository.TryRecordApprovedManagerRefundExpenseAsync(
                clubId,
                fundId,
                dto.OriginalTransactionId,
                managerUserId,
                dto.Amount,
                dto.Reason,
                dto.TransferReference,
                dto.ManagerNote);

            if (result == null)
                throw new InvalidOperationException("Không thể hoàn tiền (giao dịch gốc không hợp lệ, số tiền vượt quá, hoặc quỹ không đủ số dư).");

            var tx = await _fundRepository.GetTransactionByIdAsync(result.Value.TransactionId);
            if (tx == null)
                throw new InvalidOperationException("Không thể tải giao dịch hoàn vừa tạo.");

            return MapToTransactionDto(tx);
        }

        private const string REFUND_PENDING = "PENDING";

        private async Task EnsureClubManagerLevel1OrAdminAsync(Guid userId, int clubId, bool isSystemAdmin)
        {
            if (isSystemAdmin)
                return;

            var member = await _clubMemberRepository.GetMemberAsync(userId, clubId);
            if (member == null)
                throw new UnauthorizedAccessException("Bạn không phải thành viên của club này.");
            if (!string.Equals(member.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ thành viên đang hoạt động mới được thực hiện thao tác này.");
            if (member.ClubRole?.Level != 1)
                throw new UnauthorizedAccessException("Chỉ Club Manager (Level 1) mới được thực hiện thao tác này.");
        }

        public async Task<FundRefundRequestResponseDto> CreateFundRefundRequestAsync(
            Guid userId, int clubId, CreateFundRefundRequestDto dto)
        {
            var orig = await _fundRepository.GetTransactionByIdAsync(dto.OriginalTransactionId);
            if (orig == null || orig.ClubFund == null)
                throw new ArgumentException("Giao dịch gốc không tồn tại.", nameof(dto.OriginalTransactionId));
            if (orig.ClubFund.ClubId != clubId)
                throw new ArgumentException("Giao dịch không thuộc câu lạc bộ này.", nameof(dto.OriginalTransactionId));
            if (!orig.IsMemberContribution
                || orig.TransactionType == null
                || !string.Equals(orig.TransactionType, TYPE_INCOME, StringComparison.OrdinalIgnoreCase)
                || orig.Status == null
                || !string.Equals(orig.Status, STATUS_APPROVED, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Chỉ có thể yêu cầu hoàn cho giao dịch nộp quỹ đã thanh toán thành công.",
                    nameof(dto.OriginalTransactionId));
            }

            if (orig.CreatedBy != userId)
                throw new UnauthorizedAccessException("Bạn chỉ có thể yêu cầu hoàn cho giao dịch do chính bạn nộp.");

            var member = await _clubMemberRepository.GetMemberAsync(userId, clubId);
            if (member == null)
                throw new UnauthorizedAccessException("Bạn không phải thành viên của club này.");
            if (!string.Equals(member.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ thành viên đang hoạt động mới được gửi yêu cầu hoàn tiền.");

            if (dto.Amount <= 0m)
                throw new ArgumentException("Số tiền hoàn phải lớn hơn 0.", nameof(dto.Amount));

            var alreadyRefunded = await _fundRepository.GetTotalRefundedAmountForOriginalTransactionAsync(orig.TransactionId);
            if (dto.Amount + alreadyRefunded > orig.Amount)
                throw new ArgumentException("Số tiền hoàn vượt quá phần còn có thể hoàn của giao dịch này.", nameof(dto.Amount));

            if (await _fundRepository.ExistsPendingRefundForOriginalTransactionAsync(dto.OriginalTransactionId))
                throw new InvalidOperationException("Đã có yêu cầu hoàn đang chờ xử lý cho giao dịch này.");

            var bankName = (dto.BankName ?? string.Empty).Trim();
            var bankAccount = (dto.BankAccountNumber ?? string.Empty).Trim();
            var holder = (dto.AccountHolderName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(bankName))
                throw new ArgumentException("Tên ngân hàng không được để trống.", nameof(dto.BankName));
            if (string.IsNullOrEmpty(bankAccount))
                throw new ArgumentException("Số tài khoản không được để trống.", nameof(dto.BankAccountNumber));
            if (string.IsNullOrEmpty(holder))
                throw new ArgumentException("Tên chủ tài khoản không được để trống.", nameof(dto.AccountHolderName));

            var utc = DateTime.UtcNow;
            var entity = new FundRefundRequest
            {
                ClubId = clubId,
                FundId = orig.FundId,
                OriginalTransactionId = orig.TransactionId,
                RequestedBy = userId,
                Amount = dto.Amount,
                Reason = string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim(),
                BankName = bankName,
                BankAccountNumber = bankAccount.Length > 32 ? bankAccount[..32] : bankAccount,
                AccountHolderName = holder.Length > 200 ? holder[..200] : holder,
                Status = REFUND_PENDING,
                CreatedAtUtc = utc,
                UpdatedAtUtc = utc
            };

            await _fundRepository.AddRefundRequestAsync(entity);

            var loaded = await _fundRepository.GetRefundRequestByIdAsync(entity.RefundRequestId);
            if (loaded == null)
                throw new InvalidOperationException("Không thể tải yêu cầu hoàn vừa tạo.");
            return MapRefundRequestDto(loaded);
        }

        public async Task<PagedResultDto<FundRefundRequestResponseDto>> GetMyFundRefundRequestsPagedAsync(
            Guid userId, int clubId, int pageNumber, int pageSize)
        {
            var member = await _clubMemberRepository.GetMemberAsync(userId, clubId);
            if (member == null)
                throw new UnauthorizedAccessException("Bạn không phải thành viên của club này.");
            if (!string.Equals(member.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ thành viên đang hoạt động mới được xem yêu cầu hoàn tiền.");

            var (items, total) = await _fundRepository.GetRefundRequestsForMemberPagedAsync(clubId, userId, pageNumber, pageSize);
            return ToPagedResult(items.Select(MapRefundRequestDto), pageNumber, pageSize, total);
        }

        public async Task<PagedResultDto<FundRefundRequestResponseDto>> GetClubFundRefundRequestsPagedAsync(
            Guid managerUserId, int clubId, bool isSystemAdmin, string? status, int pageNumber, int pageSize)
        {
            await EnsureClubManagerLevel1OrAdminAsync(managerUserId, clubId, isSystemAdmin);

            string? normalizedStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                var u = status.Trim().ToUpperInvariant();
                if (u is not ("PENDING" or "COMPLETED" or "REJECTED" or "CANCELLED" or "ALL"))
                    throw new ArgumentException("Trạng thái hợp lệ: PENDING, COMPLETED, REJECTED, CANCELLED hoặc ALL.", nameof(status));
                if (u != "ALL")
                    normalizedStatus = u;
            }

            var (items, total) = await _fundRepository.GetRefundRequestsForClubPagedAsync(
                clubId, normalizedStatus, pageNumber, pageSize);
            return ToPagedResult(items.Select(MapRefundRequestDto), pageNumber, pageSize, total);
        }

        public async Task<bool> CancelFundRefundRequestAsync(Guid userId, int clubId, int refundRequestId)
        {
            var r = await _fundRepository.GetRefundRequestByIdAsync(refundRequestId);
            if (r == null || r.ClubId != clubId)
                return false;

            var member = await _clubMemberRepository.GetMemberAsync(userId, clubId);
            if (member == null
                || !string.Equals(member.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                return false;

            return await _fundRepository.TryCancelRefundRequestAsync(refundRequestId, userId);
        }

        public async Task<bool> CompleteFundRefundRequestAsync(
            Guid managerUserId, int clubId, bool isSystemAdmin, int refundRequestId, CompleteFundRefundRequestDto dto)
        {
            await EnsureClubManagerLevel1OrAdminAsync(managerUserId, clubId, isSystemAdmin);

            var r = await _fundRepository.GetRefundRequestByIdAsync(refundRequestId);
            if (r == null || r.ClubId != clubId)
                return false;

            var transferRef = (dto?.TransferReference ?? string.Empty).Trim();
            if (transferRef.Length > 100)
                throw new ArgumentException("TransferReference không được vượt quá 100 ký tự.", nameof(dto.TransferReference));
            var note = (dto?.ManagerNote ?? string.Empty).Trim();
            if (note.Length > 500)
                throw new ArgumentException("ManagerNote không được vượt quá 500 ký tự.", nameof(dto.ManagerNote));

            return await _fundRepository.TryCompleteRefundRequestAsync(
                refundRequestId,
                managerUserId,
                dto?.TransferReference,
                dto?.ManagerNote);
        }

        public async Task<bool> RejectFundRefundRequestAsync(
            Guid managerUserId, int clubId, bool isSystemAdmin, int refundRequestId, RejectFundRefundRequestDto dto)
        {
            await EnsureClubManagerLevel1OrAdminAsync(managerUserId, clubId, isSystemAdmin);

            var r = await _fundRepository.GetRefundRequestByIdAsync(refundRequestId);
            if (r == null || r.ClubId != clubId)
                return false;

            var reason = (dto?.RejectionReason ?? string.Empty).Trim();
            if (reason.Length < 5)
                throw new ArgumentException(
                    "Lý do từ chối phải có ít nhất 5 ký tự sau khi bỏ khoảng trắng đầu cuối.",
                    nameof(dto.RejectionReason));
            if (reason.Length > 2000)
                throw new ArgumentException("Lý do từ chối không được vượt quá 2000 ký tự.", nameof(dto.RejectionReason));

            return await _fundRepository.TryRejectRefundRequestAsync(refundRequestId, managerUserId, reason);
        }

        private static FundRefundRequestResponseDto MapRefundRequestDto(FundRefundRequest r)
        {
            var fn = r.OriginalTransaction?.ClubFund?.FundName ?? r.ClubFund?.FundName;
            return new FundRefundRequestResponseDto
            {
                RefundRequestId = r.RefundRequestId,
                ClubId = r.ClubId,
                FundId = r.FundId,
                OriginalTransactionId = r.OriginalTransactionId,
                RequestedBy = r.RequestedBy,
                Amount = r.Amount,
                Reason = r.Reason,
                BankName = r.BankName,
                BankAccountNumber = r.BankAccountNumber,
                AccountHolderName = r.AccountHolderName,
                Status = r.Status ?? REFUND_PENDING,
                CreatedAtUtc = r.CreatedAtUtc,
                UpdatedAtUtc = r.UpdatedAtUtc,
                CompletedAtUtc = r.CompletedAtUtc,
                CompletedBy = r.CompletedBy,
                RejectedAtUtc = r.RejectedAtUtc,
                RejectedBy = r.RejectedBy,
                RejectionReason = r.RejectionReason,
                TransferReference = r.TransferReference,
                ManagerNote = r.ManagerNote,
                FundName = string.IsNullOrWhiteSpace(fn) ? null : fn.Trim()
            };
        }
    }
}