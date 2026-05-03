using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNIC.DataAccess.Models;

namespace DataAccess.Models
{
    /// <summary>
    /// Thành viên ban tổ chức sự kiện.
    /// Pattern copy từ UserClubRole.
    /// </summary>
    public class EventMember
    {
        [Key]
        public int EventMemberId { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public int? EventRoleId { get; set; }

        public Guid? AssignedBy { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        [ForeignKey("EventId")]
        public virtual Event Event { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("EventRoleId")]
        public virtual EventRole? EventRole { get; set; }

        [ForeignKey("AssignedBy")]
        public virtual User? AssignedByUser { get; set; }

        public virtual IList<EventMemberPolicy>? EventMemberPolicies { get; set; }
    }
}
