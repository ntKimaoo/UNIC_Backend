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
        public string DepartmentName { get; set; } 

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        public virtual ICollection<DepartmentMember> DepartmentMembers { get; set; }
    }
}
