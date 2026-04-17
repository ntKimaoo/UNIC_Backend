using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class FundCategory
    {
        [Key]
        public int CategoryId { get; set; }

        /// <summary>Null = danh mục dùng chung cho mọi club; có giá trị = chỉ club đó.</summary>
        public int? ClubId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; }

        public string Description { get; set; }

        [ForeignKey("ClubId")]
        public virtual Club? Club { get; set; }

        public virtual ICollection<FundTransaction> FundTransactions { get; set; }
    }
}
