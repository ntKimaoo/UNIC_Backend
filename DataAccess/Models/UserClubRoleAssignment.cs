using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class UserClubRoleAssignment
    {
        public int ClubMemberId { get; set; }
        public int ClubRoleId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ClubMemberId")]
        public virtual UserClubRole ClubMember { get; set; }

        [ForeignKey("ClubRoleId")]
        public virtual ClubRole ClubRole { get; set; }
    }
}
