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
        public string TransactionType { get; set; } 

        [MaxLength(20)]
        public string Status { get; set; } = "PENDING"; 

        public Guid? CreatedBy { get; set; }  
        public Guid? ApprovedBy { get; set; } 

        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }

        public string Description { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        [MaxLength(100)]
        public string? PaymentLinkId { get; set; }

        [MaxLength(20)]
        public string? ContributionSource { get; set; }

        public int? RefundForTransactionId { get; set; }

        public bool IsMemberContribution { get; set; }

        [ForeignKey("FundId")]
        public virtual ClubFund ClubFund { get; set; }

        [ForeignKey(nameof(RefundForTransactionId))]
        public virtual FundTransaction? RefundForOriginalTransaction { get; set; }

        [ForeignKey("CategoryId")]
        public virtual FundCategory FundCategory { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }
    }
}