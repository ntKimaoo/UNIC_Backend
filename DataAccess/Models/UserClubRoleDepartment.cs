using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    /// <summary>
    /// Junction table for the many-to-many relationship between UserClubRole and Department.
    /// A club member can belong to multiple departments,
    /// and a department can have multiple club members.
    /// </summary>
    public class UserClubRoleDepartment
    {
        [Required]
        public int ClubMemberId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [ForeignKey("ClubMemberId")]
        public virtual UserClubRole ClubMember { get; set; } = null!;

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; } = null!;
    }
}
