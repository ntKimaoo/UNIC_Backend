using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class ClubFund
    {
        [Key]
        public int FundId { get; set; }
        [Required]
        public int ClubId { get; set; }
        [Required]
        [MaxLength(100)]
        public string FundName { get; set; }
        [MaxLength(1000)]
        public string? Description { get; set; }
        public int FundTypeId { get; set; }
        [ForeignKey("FundTypeId")]
        public virtual FundType? FundType { get; set; }
        [Column(TypeName = "decimal(15,2)")]
        public decimal? GoalAmount { get; set; }
        [Column(TypeName = "decimal(15,2)")]
        public decimal TotalAmount { get; set; } = 0;
        [Column(TypeName = "decimal(15,2)")]
        public decimal CurrentBalance { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [MaxLength(20)]
        public string Status { get; set; } = "PENDING";
        [Column(TypeName = "date")]
        public DateTime? ExpiresAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? ApprovedBy { get; set; }
        [MaxLength(2000)]
        public string? RejectReason { get; set; }
        public DateTime? RejectedAt { get; set; }
        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }
        public virtual ICollection<FundTransaction> FundTransactions { get; set; }
    }
}