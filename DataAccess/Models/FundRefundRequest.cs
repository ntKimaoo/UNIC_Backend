using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class FundRefundRequest
    {
        [Key]
        public int RefundRequestId { get; set; }

        [Required]
        public int ClubId { get; set; }

        [Required]
        public int FundId { get; set; }

        [Required]
        public int OriginalTransactionId { get; set; }

        [Required]
        public Guid RequestedBy { get; set; }

        [Column(TypeName = "decimal(15,2)")]
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

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "PENDING";

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public DateTime? CompletedAtUtc { get; set; }

        public Guid? CompletedBy { get; set; }

        public DateTime? RejectedAtUtc { get; set; }

        public Guid? RejectedBy { get; set; }

        [MaxLength(2000)]
        public string? RejectionReason { get; set; }

        [MaxLength(100)]
        public string? TransferReference { get; set; }

        [MaxLength(500)]
        public string? ManagerNote { get; set; }

        [ForeignKey(nameof(OriginalTransactionId))]
        public virtual FundTransaction OriginalTransaction { get; set; } = null!;

        [ForeignKey(nameof(FundId))]
        public virtual ClubFund ClubFund { get; set; } = null!;
    }
}
