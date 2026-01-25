using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class DepartmentMember
    {
        [Key]
        public int DeptMemberId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        public int DeptRoleId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public Guid? AssignedBy { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        [ForeignKey("DeptRoleId")]
        public virtual DepartmentRole DepartmentRole { get; set; }

        [ForeignKey("AssignedBy")]
        public virtual User AssignedByUser { get; set; }
    }
}