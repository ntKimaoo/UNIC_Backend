using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    public class CreateFundRequestDto
    {
        [Required]
        public int FundId { get; set; }
        public int? CategoryId { get; set; }

        [Required]
        public string TransactionType { get; set; } // "INCOME" or "EXPENSE"

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        public string Description { get; set; }
    }

    public class ProcessFundRequestDto
    {
        [Required]
        public int TransactionId { get; set; }

        [Required]
        public string Action { get; set; } // "APPROVE" or "REJECT"
    }
}