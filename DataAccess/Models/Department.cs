using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models
{
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        [Required]
        public int ClubId { get; set; }

        [Required]
        [MaxLength(100)]
        public string DepartmentName { get; set; } = null!;

        public string? Description { get; set; }

        public int? ManagerRoleId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; } = false;
        
        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; } = null!;

        [ForeignKey("ManagerRoleId")]
        public virtual ClubRole? ManagerRole { get; set; }

        public virtual ICollection<ClubRole> ClubRoles { get; set; } = new List<ClubRole>();

        /// <summary>Members assigned to this department (many-to-many via UserClubRoleDepartment).</summary>
        public virtual ICollection<UserClubRoleDepartment> DepartmentMembers { get; set; } = new List<UserClubRoleDepartment>();
    }
}
