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

        public virtual ICollection<UserClubRole> ClubMembers { get; set; }
        public virtual IList<ClubRolePolicy>? ClubRolePolicies { get; set; }
        public int? ClubId { get; set; }
        public Club? Club { get; set; }
    }
}
