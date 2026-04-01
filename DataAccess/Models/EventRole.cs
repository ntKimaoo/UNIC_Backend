using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UNIC.DataAccess.Models;

namespace DataAccess.Models
{
    public class EventRole
    {
        [Key]
        public int EventRoleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; }

        public string Description { get; set; }

        public int Level { get; set; } = 0;

        [Required]
        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public virtual Event Event { get; set; }

        public virtual ICollection<UserEventRole> UserEventRoles { get; set; }
        public virtual IList<EventRolePolicy>? EventRolePolicies { get; set; }
    }
}
