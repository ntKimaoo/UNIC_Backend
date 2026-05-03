using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models
{
    public class ClubPost
    {
        [Key]
        public int PostId { get; set; }

        [Required]
        public int ClubId { get; set; }

        public Guid? UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string Caption { get; set; }
        public string Content { get; set; }

        public DateTime PostDate { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string Status { get; set; } = "PUBLISHED";

        public int? EventId { get; set; }
        public int? CampaignId { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation Properties
        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("EventId")]
        public virtual Event? Event { get; set; }

        [ForeignKey("CampaignId")]
        public virtual RecruitmentCampaign? RecruitmentCampaign { get; set; }
    }
}
