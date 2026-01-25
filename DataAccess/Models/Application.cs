using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class Application
    {
        [Key]
        public int ApplicationId { get; set; }

        [Required]
        public int FormId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string Status { get; set; } = "PENDING";

        public DateTime? ReviewedAt { get; set; }

        [ForeignKey("FormId")]
        public virtual ApplicationForm ApplicationForm { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public virtual ICollection<ApplicationAnswer> ApplicationAnswers { get; set; }
    }
}