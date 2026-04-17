using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNIC.DataAccess.Models;

namespace DataAccess.Models
{
    public class UserEventRole
    {
        [Key]
        public int EventMemberId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public int EventId { get; set; }

        public int? EventRoleId { get; set; }

        public DateTime JoinDate { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string Status { get; set; } = "ACTIVE";

        public Guid? AssignedBy { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("EventId")]
        public virtual Event Event { get; set; }

        [ForeignKey("EventRoleId")]
        public virtual EventRole? EventRole { get; set; }

        [ForeignKey("AssignedBy")]
        public virtual User AssignedByUser { get; set; }
        
        public virtual IList<EventMemberPolicy>? EventMemberPolicies { get; set; }
    }
}
