using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models
{
    public class FundCategory
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; }

        public string Description { get; set; }

        public virtual ICollection<FundTransaction> FundTransactions { get; set; }
    }
}