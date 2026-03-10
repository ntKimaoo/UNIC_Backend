using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class UserClubRole
    {
        [Key]
        public int ClubMemberId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public int ClubId { get; set; }

        public int? ClubRoleId { get; set; }

        public DateTime JoinDate { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string Status { get; set; } = "ACTIVE";

        public Guid? AssignedBy { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        [ForeignKey("ClubRoleId")]
        public virtual ClubRole? ClubRole { get; set; }

        [ForeignKey("AssignedBy")]
        public virtual User AssignedByUser { get; set; }
    }
}