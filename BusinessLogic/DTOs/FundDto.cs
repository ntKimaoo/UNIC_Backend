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
    }

    public class ContributeRequestDto
    {
        [Required]
        public int FundId { get; set; }
        public int? CategoryId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
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
        public string Message { get; set; } = "Quét QR hoặc mở link để thanh toán. Sau khi thanh toán thành công, quỹ sẽ được cập nhật tự động.";
    }

    public class CreateFundRequestDto
    {
        [Required]
        public int FundId { get; set; }
        public int? CategoryId { get; set; }

        [Required]
        public string TransactionType { get; set; } // "INCOME" or "EXPENSE"

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal Amount { get; set; }

        public string? Description { get; set; }
    }

    public class ProcessFundRequestDto
    {
        [Required]
        public int TransactionId { get; set; }

        [Required]
        public string Action { get; set; } // "APPROVE" or "REJECT"
    }

    public class ApproveFundDto
    {
        [Required]
        public int FundId { get; set; }

        [Required]
        public string Action { get; set; } // "APPROVE" or "REJECT"
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
    }
}