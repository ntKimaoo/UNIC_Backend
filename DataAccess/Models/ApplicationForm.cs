using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class ApplicationForm
    {
        [Key]
        public int FormId { get; set; }

        [Required]
        public int CampaignId { get; set; }

        [Required]
        [MaxLength(200)]
        public string FormName { get; set; }
        public string FormTitle { get; set; }
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("CampaignId")]
        public virtual RecruitmentCampaign RecruitmentCampaign { get; set; }

        public virtual ICollection<ApplicationQuestion> ApplicationQuestions { get; set; }
        public virtual ICollection<Application> Applications { get; set; }
    }
}