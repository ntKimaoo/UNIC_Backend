using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models
{
    public class DepartmentRole
    {
        [Key]
        public int DeptRoleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; }

        public string Description { get; set; }

        public string Permissions { get; set; }

        public virtual ICollection<DepartmentMember> DepartmentMembers { get; set; }
    }
}