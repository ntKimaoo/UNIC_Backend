using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BusinessLogic.DTOs
{
    public class CreateFundDto
    {
        private static readonly HashSet<string> DescriptionAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            "description",
            "fundDescription",
            "fund_description",
            "desc",
            "note",
            "content",
            "mota",
            "moTa"
        };

        [Required]
        public int ClubId { get; set; }
        [Required]
        [MaxLength(100)]
        public string FundName { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int FundTypeId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Mục tiêu quỹ phải >= 0")]
        public decimal? GoalAmount { get; set; }
        public DateTime? ExpiresAt { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtraData { get; set; }

        public string? ResolveDescription()
        {
            if (!string.IsNullOrWhiteSpace(Description))
                return Description.Trim();
            if (ExtraData == null || ExtraData.Count == 0)
                return null;

            foreach (var item in ExtraData)
            {
                if (!DescriptionAliases.Contains(item.Key))
                    continue;
                if (item.Value.ValueKind != JsonValueKind.String)
                    continue;

                var value = item.Value.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return null;
        }
    }

    public class RecordCashContributionRequestDto
    {
        [Required]
        public int FundId { get; set; }

        [Required]
        public Guid ContributorUserId { get; set; }

        [Required]
        [Range(10000, double.MaxValue, ErrorMessage = "Số tiền tối thiểu 10.000 ₫")]
        public decimal Amount { get; set; }

        [Required]
        [MinLength(3)]
        [MaxLength(2000)]
        public string Note { get; set; } = string.Empty;

        public int? CategoryId { get; set; }
        public DateTime? ContributedAtUtc { get; set; }
    }

    public class RecordCashContributionResponseDto
    {
        public int TransactionId { get; set; }
        public int FundId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "APPROVED";
        public string ContributionSource { get; set; } = "CASH";
        public decimal NewCurrentBalance { get; set; }
        public Guid ContributorUserId { get; set; }
        public Guid RecordedByUserId { get; set; }
    }

    public class ContributeRequestDto
    {
        [Required]
        public int FundId { get; set; }
        public int? CategoryId { get; set; }
        [Required]
        [Range(10000, double.MaxValue, ErrorMessage = "Số tiền tối thiểu 10.000 ₫")]
        public decimal Amount { get; set; }
        [MaxLength(255)]
        public string? Description { get; set; }
    }

    public class ContributeResponseDto
    {
        public int TransactionId { get; set; }
        public string CheckoutUrl { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
        public string? PaymentLinkId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentLinkExpiresAtUtc { get; set; }
        public string Message { get; set; } = "Quét QR hoặc mở link để thanh toán. Sau khi thanh toán thành công, quỹ sẽ được cập nhật tự động.";
    }

    public class ContributionPaymentStatusDto
    {
        public int ClubId { get; set; }
        public int TransactionId { get; set; }
        public int FundId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
        public bool IsPaymentLinkExpired { get; set; }
        public DateTime? PaymentLinkExpiresAtUtc { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ApproveFundDto
    {
        private static readonly HashSet<string> RejectReasonAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            "rejectReason",
            "RejectReason",
            "rejectionReason",
            "RejectionReason"
        };

        [Required]
        public int FundId { get; set; }
        [Required]
        public string Action { get; set; } = string.Empty;

        public string? RejectReason { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtraData { get; set; }

        public string? ResolveRejectReason()
        {
            if (!string.IsNullOrWhiteSpace(RejectReason))
                return RejectReason.Trim();
            if (ExtraData == null || ExtraData.Count == 0)
                return null;

            foreach (var item in ExtraData)
            {
                if (!RejectReasonAliases.Contains(item.Key))
                    continue;
                if (item.Value.ValueKind != JsonValueKind.String)
                    continue;
                var value = item.Value.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return null;
        }
    }

    public class FundResponseDto
    {
        public int FundId { get; set; }
        public int ClubId { get; set; }
        public string FundName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int FundTypeId { get; set; }
        public string? FundTypeName { get; set; }
        public decimal? GoalAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "PENDING";
        public DateTime? ExpiresAt { get; set; }
        public string? RejectReason { get; set; }
        public DateTime? RejectedAt { get; set; }
        [JsonPropertyName("rejectionReasonVi")]
        public string? RejectionReasonVi { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsClosed { get; set; }
        public string? ClosedReasonCode { get; set; }
        public string? LifecycleStatusVi { get; set; }
        public bool CanAcceptContributions { get; set; }
        public string? CannotContributeReasonVi { get; set; }
        public string? BalanceContextVi { get; set; }
        public string? ExpiresAtUtcNoteVi { get; set; }
    }

    public sealed class FundMemberContributionStatusDto
    {
        public Guid UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public decimal PaidAmount { get; set; }
        public decimal? RequiredAmount { get; set; }
        public decimal? RemainingAmount { get; set; }
        public bool IsPaidEnough { get; set; }
    }

    public sealed class FundMemberContributionOverviewDto
    {
        public int ClubId { get; set; }
        public int FundId { get; set; }
        public string FundName { get; set; } = string.Empty;
        public int FundTypeId { get; set; }
        public string? FundTypeName { get; set; }
        public decimal? GoalAmount { get; set; }
        public int ActiveMemberCount { get; set; }
        public decimal? RequiredPerMember { get; set; }
        public decimal TotalApprovedMemberContributions { get; set; }
        public IReadOnlyList<FundMemberContributionStatusDto> Members { get; set; } = Array.Empty<FundMemberContributionStatusDto>();
    }

    public class FundTransactionResponseDto
    {
        public int TransactionId { get; set; }
        public int FundId { get; set; }
        public string? FundName { get; set; }
        public int? CategoryId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? ApprovedBy { get; set; }
        public string? PaymentLinkId { get; set; }
        public string? PaymentProvider { get; set; }
        public string? ContributionSource { get; set; }
        public int? RefundForTransactionId { get; set; }
        public bool IsMemberContribution { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? MemberName { get; set; }
        public string? ContributorName { get; set; }
        public string? UserFullName { get; set; }
        public string? CategoryName { get; set; }
    }

    public class FundCategoryResponseDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ClubId { get; set; }
    }

    public class FundMenuItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string LabelVi { get; set; } = string.Empty;
        public string LabelEn { get; set; } = string.Empty;
        public bool Visible { get; set; }
    }

    public class FundCapabilitiesDto
    {
        public int ClubId { get; set; }
        public bool IsActiveClubMember { get; set; }
        public int? ClubRoleLevel { get; set; }
        public string? ClubRoleName { get; set; }
        public bool HasViewFinancePolicy { get; set; }
        public bool HasCreateFinancePolicy { get; set; }
        public bool HasEditFinancePolicy { get; set; }
        public bool HasDeleteFinancePolicy { get; set; }
        public bool CanViewFunds { get; set; }
        public bool CanContribute { get; set; }
        public bool CanCreateFund { get; set; }
        public bool CanApproveOrRejectFundEntity { get; set; }
        public bool CanManageOnlinePaymentSettings { get; set; }
        public bool CanRecordCashContributions { get; set; }
        public bool CanProcessClubRefunds { get; set; }
        public bool CanSoftDeleteFund { get; set; }
        public bool CanViewSoftDeletedFunds { get; set; }
        public string? FinanceAccessHintVi { get; set; }
        public IReadOnlyList<FundMenuItemDto> MenuItems { get; set; } = Array.Empty<FundMenuItemDto>();
    }

    public class ClubFundReportSummaryDto
    {
        public int ClubId { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public string DateFilterNoteVi { get; set; } =
            "fromUtc/toUtc lọc theo mốc thời gian giao dịch (UTC). Nên dùng cùng khoảng ngày khi đối chiếu với GET .../funds/transactions.";

        public int PendingFundCount { get; set; }
        public int ApprovedFundCount { get; set; }
        public int RejectedFundCount { get; set; }
        public decimal TotalBalanceApprovedFunds { get; set; }
        public decimal TotalApprovedIncome { get; set; }
        public decimal TotalApprovedExpense { get; set; }
    }

    public class CreateFundRefundRequestDto
    {
        [Required]
        public int OriginalTransactionId { get; set; }

        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Số tiền hoàn phải lớn hơn 0.")]
        public decimal Amount { get; set; }

        [MaxLength(2000)]
        public string? Reason { get; set; }

        [Required]
        [MaxLength(100)]
        public string BankName { get; set; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string AccountHolderName { get; set; } = string.Empty;
    }

    public class CompleteFundRefundRequestDto
    {
        [MaxLength(100)]
        public string? TransferReference { get; set; }

        [MaxLength(500)]
        public string? ManagerNote { get; set; }
    }

    public class RejectFundRefundRequestDto
    {
        [Required]
        [MaxLength(2000)]
        public string RejectionReason { get; set; } = string.Empty;
    }

    public sealed class ManagerRefundContributionDto
    {
        [Required]
        public int OriginalTransactionId { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Số tiền hoàn phải > 0")]
        public decimal Amount { get; set; }

        [MaxLength(2000)]
        public string? Reason { get; set; }

        [MaxLength(100)]
        public string? TransferReference { get; set; }

        [MaxLength(500)]
        public string? ManagerNote { get; set; }
    }

    public class FundRefundRequestResponseDto
    {
        public int RefundRequestId { get; set; }
        public int ClubId { get; set; }
        public int FundId { get; set; }
        public int OriginalTransactionId { get; set; }
        public Guid RequestedBy { get; set; }
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public Guid? CompletedBy { get; set; }
        public DateTime? RejectedAtUtc { get; set; }
        public Guid? RejectedBy { get; set; }
        public string? RejectionReason { get; set; }
        public string? TransferReference { get; set; }
        public string? ManagerNote { get; set; }
        public string? FundName { get; set; }
    }
}