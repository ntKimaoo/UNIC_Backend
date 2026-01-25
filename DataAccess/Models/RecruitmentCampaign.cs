using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models
{
    public class RecruitmentCampaign
    {
        [Key]
        public int CampaignId { get; set; }

        [Required]
        public int ClubId { get; set; }

        [Required]
        [MaxLength(200)]
        public string CampaignName { get; set; }
        public string LinkCampaign { get; set; }
        public string Description { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        [MaxLength(20)]
        public string Status { get; set; } = "OPEN";
        public string ImageUrl { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        public virtual ICollection<ApplicationForm> ApplicationForms { get; set; }
    }
}
