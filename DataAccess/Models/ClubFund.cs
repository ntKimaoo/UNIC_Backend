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

        [Column(TypeName = "decimal(15,2)")]
        public decimal TotalAmount { get; set; } = 0;

        [Column(TypeName = "decimal(15,2)")]
        public decimal CurrentBalance { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string Status { get; set; } = "PENDING";

        /// <summary>Ngày cuối cùng (theo lịch UTC) quỹ còn nhận nộp tiền từ thành viên. Null = không giới hạn.</summary>
        [Column(TypeName = "date")]
        public DateTime? ExpiresAt { get; set; }

        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        public virtual ICollection<FundTransaction> FundTransactions { get; set; }
    }
}