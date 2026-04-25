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
        public string ShortName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime? FoundedDate { get; set; }

        [MaxLength(20)]
        public string Status { get; set; }

        public bool IsPublic { get; set; } = true;

        public string LogoUrl { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string FacebookUrl { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
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
        public virtual ICollection<RecordOfChange> RecordsOfChange { get; set; }
    }


}
