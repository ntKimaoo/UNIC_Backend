using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class ApplicationQuestion
    {
        [Key]
        public int QuestionId { get; set; }

        [Required]
        public int FormId { get; set; }

        [Required]
        public string QuestionText { get; set; }

        [MaxLength(50)]
        public string QuestionType { get; set; } // 'TEXT', 'MULTIPLE_CHOICE', 'ESSAY'

        public bool IsRequired { get; set; }

        public int? DisplayOrder { get; set; }

        [ForeignKey("FormId")]
        public virtual ApplicationForm ApplicationForm { get; set; }
        public virtual ICollection<ApplicationAnswer> ApplicationAnswers { get; set; }
    }
}