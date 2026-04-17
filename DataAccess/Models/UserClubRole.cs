using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNIC.DataAccess.Models;

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

        public DateTime JoinDate { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string Status { get; set; } = "ACTIVE";

        public Guid? AssignedBy { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        [ForeignKey("AssignedBy")]
        public virtual User AssignedByUser { get; set; }
        public virtual IList<ClubMemberPolicy>? ClubMemberPolicies { get; set; }

        /// <summary>Departments the member belongs to (many-to-many via UserClubRoleDepartment).</summary>
        public virtual ICollection<UserClubRoleDepartment> MemberDepartments { get; set; } = new List<UserClubRoleDepartment>();

        public virtual ICollection<UserClubRoleAssignment> RoleAssignments { get; set; } = new List<UserClubRoleAssignment>();
    }
}