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

    public class ContributeRequestDto
    {
        [Required]
        public int FundId { get; set; }
        public int? CategoryId { get; set; }
        [Required]
        [Range(1000, double.MaxValue, ErrorMessage = "Số tiền tối thiểu 1.000 ₫")]
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
        public decimal TotalAmount { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "PENDING";
        public DateTime? ExpiresAt { get; set; }
        public string? RejectReason { get; set; }
        public DateTime? RejectedAt { get; set; }
        [JsonPropertyName("rejectionReasonVi")]
        public string? RejectionReasonVi { get; set; }
        public bool CanAcceptContributions { get; set; }
        public string? CannotContributeReasonVi { get; set; }
        public string? BalanceContextVi { get; set; }
        public string? ExpiresAtUtcNoteVi { get; set; }
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
        public bool CanViewFunds { get; set; }
        public bool CanContribute { get; set; }
        public bool CanCreateFund { get; set; }
        public bool CanApproveOrRejectFundEntity { get; set; }
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
}