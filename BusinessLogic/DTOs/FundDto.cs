using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    public class CreateFundDto
    {
        [Required]
        public int ClubId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FundName { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền ban đầu không được âm")]
        public decimal InitialAmount { get; set; } = 0;

        /// <summary>Ngày cuối cùng quỹ còn nhận nộp tiền (theo ngày, inclusive). Bỏ trống = không giới hạn.</summary>
        public DateTime? ExpiresAt { get; set; }
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
        [Required]
        public int FundId { get; set; }

        [Required]
        public string Action { get; set; } 
    }

    public class FundResponseDto
    {
        public int FundId { get; set; }
        public int ClubId { get; set; }
        public string FundName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "PENDING";
        /// <summary>Ngày cuối nhận nộp tiền (UTC date). Null = không giới hạn.</summary>
        public DateTime? ExpiresAt { get; set; }
        /// <summary>Quỹ đã duyệt và chưa quá hạn nhận nộp tiền.</summary>
        public bool CanAcceptContributions { get; set; }
    }

    public class FundTransactionResponseDto
    {
        public int TransactionId { get; set; }
        public int FundId { get; set; }
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

        /// <summary>UTC — thời tạo giao dịch; FE có thể dùng nếu không có updatedAt.</summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>UTC — cập nhật cuối (sau PayOS); FE ưu tiên cho «thời gian nộp».</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Tên người nộp (Users.FullName). Cùng giá trị với contributorName / userFullName để FE gom key.</summary>
        public string? MemberName { get; set; }
        public string? ContributorName { get; set; }
        public string? UserFullName { get; set; }
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
    }
}