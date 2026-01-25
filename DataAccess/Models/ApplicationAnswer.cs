using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class ApplicationAnswer
    {
        [Key]
        public int AnswerId { get; set; }

        [Required]
        public int ApplicationId { get; set; }

        [Required]
        public int QuestionId { get; set; }
        public string AnswerText { get; set; }

        [ForeignKey("ApplicationId")]
        public virtual Application Application { get; set; }

        [ForeignKey("QuestionId")]
        public virtual ApplicationQuestion ApplicationQuestion { get; set; }
    }
}