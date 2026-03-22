using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using UNIC.DataAccess.Repositories.Interface;

namespace BusinessLogic.Services.Implementation
{
    public class ClubFundService : IClubFundService
    {
        private readonly IFundRepository _fundRepository;
        private readonly IClubMemberRepository _clubMemberRepository;
        private readonly IPayOSService _payOSService;

        private const string STATUS_PENDING = "PENDING";
        private const string STATUS_APPROVED = "APPROVED";
        private const string STATUS_REJECTED = "REJECTED";
        private const string TYPE_INCOME = "INCOME";
        private const string TYPE_EXPENSE = "EXPENSE";
        private const string MEMBER_STATUS_ACTIVE = "ACTIVE";

        public ClubFundService(IFundRepository fundRepository, IClubMemberRepository clubMemberRepository, IPayOSService payOSService)
        {
            _fundRepository = fundRepository;
            _clubMemberRepository = clubMemberRepository;
            _payOSService = payOSService;
        }

        public async Task<FundResponseDto> CreateFundAsync(Guid userId, CreateFundDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FundName))
                throw new ArgumentException("Tên quỹ không được để trống.", nameof(dto.FundName));
            if (dto.InitialAmount < 0)
                throw new ArgumentException("Số tiền ban đầu không được âm.", nameof(dto.InitialAmount));

            var member = await _clubMemberRepository.GetMemberAsync(userId, dto.ClubId);
            if (member == null)
                throw new UnauthorizedAccessException("Bạn không phải thành viên của club này.");
            if (!string.Equals(member.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ thành viên đang hoạt động mới được tạo quỹ.");
            if (!HasManagerOrViceLevel(member.ClubRole))
                throw new UnauthorizedAccessException("Chỉ Club Manager hoặc Vice Manager mới được tạo quỹ.");

            // Manager (Level 1) tạo quỹ → đã duyệt; Vice Manager (Level 2) tạo → chờ duyệt.
            var fundStatus = HasHighestClubLevel(member.ClubRole) ? STATUS_APPROVED : STATUS_PENDING;

            var fund = new ClubFund
            {
                ClubId = dto.ClubId,
                FundName = dto.FundName.Trim(),
                TotalAmount = dto.InitialAmount,
                CurrentBalance = dto.InitialAmount,
                CreatedAt = DateTime.UtcNow,
                Status = fundStatus
            };

            var created = await _fundRepository.AddFundAsync(fund);
            return MapToFundDto(created);
        }

        public async Task<ContributeResponseDto> CreateContributionAsync(Guid userId, ContributeRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request.Amount <= 0)
                throw new ArgumentException("Số tiền phải lớn hơn 0.", nameof(request.Amount));

            var fund = await _fundRepository.GetFundByIdAsync(request.FundId);
            if (fund == null)
                throw new InvalidOperationException("Quỹ không tồn tại.");
            if (!string.Equals(fund.Status, STATUS_APPROVED, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Chỉ có thể nộp tiền vào quỹ đã được duyệt.");

            var member = await _clubMemberRepository.GetMemberAsync(userId, fund.ClubId);
            if (member == null)
                throw new UnauthorizedAccessException("Bạn không phải thành viên của club này.");
            if (!string.Equals(member.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ thành viên đang hoạt động mới được nộp tiền vào quỹ.");

            var transaction = new FundTransaction
            {
                FundId = request.FundId,
                CategoryId = request.CategoryId,
                TransactionType = TYPE_INCOME,
                Amount = request.Amount,
                Description = request.Description?.Trim() ?? "Nộp quỹ",
                TransactionDate = DateTime.UtcNow,
                Status = STATUS_PENDING,
                CreatedBy = userId
            };

            await _fundRepository.AddTransactionAsync(transaction);

            var payOsResult = await _payOSService.CreatePaymentLinkAsync(
                transaction.TransactionId,
                transaction.Amount,
                transaction.Description ?? "Nop quy",
                cancellationToken);

            transaction.PaymentLinkId = payOsResult.PaymentLinkId;
            await _fundRepository.UpdateTransactionAsync(transaction);

            return new ContributeResponseDto
            {
                TransactionId = transaction.TransactionId,
                CheckoutUrl = payOsResult.CheckoutUrl,
                QrCode = payOsResult.QrCode,
                PaymentLinkId = payOsResult.PaymentLinkId,
                Amount = transaction.Amount,
                Message = "Quét QR hoặc mở link để thanh toán. Sau khi thanh toán thành công, quỹ sẽ được cập nhật tự động."
            };
        }

        public async Task<FundTransaction> CreateRequestAsync(Guid userId, CreateFundRequestDto request)
        {
            if (request.Amount <= 0)
                throw new ArgumentException("Số tiền phải lớn hơn 0.", nameof(request.Amount));

            var transactionType = request.TransactionType?.Trim().ToUpperInvariant();
            if (transactionType != TYPE_INCOME && transactionType != TYPE_EXPENSE)
                throw new ArgumentException("Loại giao dịch phải là INCOME hoặc EXPENSE.", nameof(request.TransactionType));

            var fund = await _fundRepository.GetFundByIdAsync(request.FundId);
            if (fund == null)
                throw new InvalidOperationException("Quỹ không tồn tại.");
            if (!string.Equals(fund.Status, STATUS_APPROVED, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Chỉ có thể tạo yêu cầu THU/CHI khi quỹ đã được duyệt.");

            var member = await _clubMemberRepository.GetMemberAsync(userId, fund.ClubId);
            if (member == null)
                throw new UnauthorizedAccessException("Bạn không phải thành viên của club này.");
            if (!string.Equals(member.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ thành viên đang hoạt động mới được gửi yêu cầu.");

            var transaction = new FundTransaction
            {
                FundId = request.FundId,
                CategoryId = request.CategoryId,
                TransactionType = transactionType,
                Amount = request.Amount,
                Description = request.Description?.Trim() ?? string.Empty,
                TransactionDate = DateTime.UtcNow,
                Status = STATUS_PENDING,
                CreatedBy = userId
            };

            await _fundRepository.AddTransactionAsync(transaction);
            return transaction;
        }

        public async Task<bool> ProcessRequestAsync(Guid managerId, ProcessFundRequestDto request)
        {
            var transaction = await _fundRepository.GetTransactionByIdAsync(request.TransactionId);
            if (transaction == null)
                throw new InvalidOperationException("Giao dịch không tồn tại.");
            if (transaction.Status != STATUS_PENDING)
                throw new InvalidOperationException("Giao dịch đã được xử lý trước đó.");

            var action = request.Action?.Trim().ToUpperInvariant();
            if (action != "APPROVE" && action != "REJECT")
                throw new ArgumentException("Hành động phải là APPROVE hoặc REJECT.", nameof(request.Action));

            var fund = transaction.ClubFund;
            if (fund == null)
                throw new InvalidOperationException("Không tìm thấy quỹ liên kết với giao dịch.");

            var approverMember = await _clubMemberRepository.GetMemberAsync(managerId, fund.ClubId);
            if (approverMember == null)
                throw new UnauthorizedAccessException("Bạn không phải thành viên của club này.");
            if (!string.Equals(approverMember.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ thành viên đang hoạt động mới được duyệt yêu cầu.");
            if (!HasManagerOrViceLevel(approverMember.ClubRole))
                throw new UnauthorizedAccessException("Chỉ Club Manager hoặc Vice Manager mới được duyệt/từ chối yêu cầu quỹ.");

            transaction.ApprovedBy = managerId;
            transaction.TransactionDate = DateTime.UtcNow;

            if (action == "APPROVE")
            {
                if (transaction.Amount <= 0)
                    throw new InvalidOperationException("Số tiền giao dịch không hợp lệ.");

                if (transaction.TransactionType == TYPE_EXPENSE)
                {
                    if (fund.CurrentBalance < transaction.Amount)
                        throw new InvalidOperationException("Số dư quỹ không đủ để duyệt chi tiêu này.");
                    fund.CurrentBalance -= transaction.Amount;
                }
                else if (transaction.TransactionType == TYPE_INCOME)
                {
                    fund.CurrentBalance += transaction.Amount;
                    fund.TotalAmount += transaction.Amount;
                }

                transaction.Status = STATUS_APPROVED;
            }
            else
            {
                transaction.Status = STATUS_REJECTED;
            }

            await _fundRepository.UpdateTransactionAndFundAsync(transaction, fund);
            return true;
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

        public async Task<IEnumerable<FundTransactionResponseDto>> GetFundHistoryAsync(int fundId, string? status)
        {
            var normalizedStatus = status?.ToUpperInvariant();
            var list = await _fundRepository.GetTransactionsByFundIdAsync(fundId, normalizedStatus);
            return list.Select(MapToTransactionDto);
        }

        private static FundResponseDto MapToFundDto(ClubFund fund)
        {
            return new FundResponseDto
            {
                FundId = fund.FundId,
                ClubId = fund.ClubId,
                FundName = fund.FundName,
                TotalAmount = fund.TotalAmount,
                CurrentBalance = fund.CurrentBalance,
                CreatedAt = fund.CreatedAt,
                Status = fund.Status ?? "PENDING"
            };
        }

        private static FundTransactionResponseDto MapToTransactionDto(FundTransaction t)
        {
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
                PaymentLinkId = t.PaymentLinkId
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
            if (transaction.TransactionType != TYPE_INCOME || transaction.Status != STATUS_PENDING)
                return false;

            var fund = transaction.ClubFund;
            if (fund == null)
                return false;

            transaction.Status = STATUS_APPROVED;
            transaction.TransactionDate = DateTime.UtcNow;
            fund.CurrentBalance += transaction.Amount;
            fund.TotalAmount += transaction.Amount;

            await _fundRepository.UpdateTransactionAndFundAsync(transaction, fund);
            return true;
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
    }
}