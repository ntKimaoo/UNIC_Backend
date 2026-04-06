using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class FundTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int FundId { get; set; }

        public int? CategoryId { get; set; }

        [Required]
        [MaxLength(20)]
        public string TransactionType { get; set; } // 'INCOME' or 'EXPENSE'

        [MaxLength(20)]
        public string Status { get; set; } = "PENDING"; // 'PENDING', 'APPROVED', 'REJECTED'

        public Guid? CreatedBy { get; set; }  // Nguoi tao yeu cau
        public Guid? ApprovedBy { get; set; } // Nguoi duyet

        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }

        public string Description { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        /// <summary>Thời điểm tạo bản ghi (UTC). Dùng cho lịch sử nộp tiền.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Thời điểm cập nhật cuối (UTC), ví dụ sau PayOS webhook — FE ưu tiên cho «thời gian nộp».</summary>
        public DateTime UpdatedAt { get; set; }

        [MaxLength(100)]
        public string? PaymentLinkId { get; set; }

        /// <summary>True nếu giao dịch tạo từ luồng nộp tiền (PayOS) của thành viên — dùng cho lịch sử nộp tiền.</summary>
        public bool IsMemberContribution { get; set; }

        [ForeignKey("FundId")]
        public virtual ClubFund ClubFund { get; set; }

        [ForeignKey("CategoryId")]
        public virtual FundCategory FundCategory { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }
    }
}