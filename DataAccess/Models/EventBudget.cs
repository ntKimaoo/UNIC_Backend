using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class EventBudget
    {
        [Key]
        public int BudgetId { get; set; }

        [Required]
        public int EventId { get; set; }

        public string SpendName { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal BudgetAmount { get; set; } = 0;

        [Column(TypeName = "decimal(15,2)")]
        public decimal SpentAmount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string Notes { get; set; }

        [ForeignKey("EventId")]
        public virtual Event Event { get; set; }
    }
}