using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UNIC.DataAccess.Models;

namespace DataAccess.Models
{
    public class ClubRole
    {
        [Key]
        public int ClubRoleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; }

        public string Description { get; set; }

        public int Level { get; set; } = 0;

        public int? ClubId { get; set; }
        
        public int? DepartmentId { get; set; }

        [ForeignKey("ClubId")]
        public virtual Club? Club { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        public virtual ICollection<UserClubRoleAssignment> MemberAssignments { get; set; }
        
        public virtual ICollection<Department> ManagedDepartments { get; set; }

        public virtual IList<ClubRolePolicy>? ClubRolePolicies { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
