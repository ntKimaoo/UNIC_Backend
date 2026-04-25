using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNIC.DataAccess.Models;

namespace DataAccess.Models
{
    /// <summary>
    /// Vai trò trong sự kiện (Creator, Coordinator, CheckInStaff...).
    /// Pattern copy từ ClubRole.
    /// </summary>
    public class EventRole
    {
        [Key]
        public int EventRoleId { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>Level 0 = Creator (full quyền), giống ClubRole.Level 0 = Manager.</summary>
        public int Level { get; set; } = 1;

        [ForeignKey("EventId")]
        public virtual Event Event { get; set; } = null!;

        public virtual ICollection<EventMember> EventMembers { get; set; } = new List<EventMember>();
        public virtual IList<EventRolePolicy> EventRolePolicies { get; set; } = new List<EventRolePolicy>();
    }
}
