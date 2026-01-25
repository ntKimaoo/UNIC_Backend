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

        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }

        public string Description { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [ForeignKey("FundId")]
        public virtual ClubFund ClubFund { get; set; }

        [ForeignKey("CategoryId")]
        public virtual FundCategory FundCategory { get; set; }
    }
}