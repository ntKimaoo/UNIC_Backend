using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models
{
    public class Club
    {
        [Key]
        public int ClubId { get; set; }

        [Required, MaxLength(100)]
        public string ClubName { get; set; }

        [MaxLength(50)]
        public string? ShortName { get; set; }

        public string? Description { get; set; }

        public DateTime? FoundedDate { get; set; }

        [MaxLength(20)]
        public string Status { get; set; }

        public bool IsPublic { get; set; } = true;

        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }

        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FacebookUrl { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

        public virtual ICollection<Department> Departments { get; set; }
        public virtual ICollection<UserClubRole> ClubMembers { get; set; }
        public virtual ICollection<ClubPost> ClubPosts { get; set; }
        public virtual ICollection<Event> Events { get; set; }
        public virtual ICollection<ClubFund> ClubFunds { get; set; }
        public virtual ICollection<RecruitmentCampaign> RecruitmentCampaigns { get; set; }
        public virtual ICollection<ClubRole> ClubRoles { get; set; }
        public virtual RecordOfChange RecordOfChange { get; set; }
    }


}
