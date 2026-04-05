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
    public class UserRole
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
