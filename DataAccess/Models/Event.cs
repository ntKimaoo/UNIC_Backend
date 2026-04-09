using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }

        public int? ClubId { get; set; }

        [Required]
        [MaxLength(200)]
        public string EventName { get; set; }

        public string Description { get; set; }
        public string ImageUrl { get; set; }
        [MaxLength(200)]
        public string Location { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsPublic { get; set; }
        [MaxLength(20)]
        public string Status { get; set; } = "PLANNED";

        // Registration fields
        public DateTime? RegistrationStartDate { get; set; }
        public DateTime? RegistrationEndDate { get; set; }
        public int? MaxAttendees { get; set; }
        public bool RequiresApproval { get; set; } = false;
        public int? AvailableSlots { get; set; }    // null = không giới hạn, được tính từ MaxAttendees

        // Check-in fields
        [MaxLength(10)]
        public string? CheckInCode { get; set; }
        public DateTime? CodeExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        public virtual ICollection<EventSchedule> EventSchedules { get; set; }
        public virtual ICollection<EventImage> EventImages { get; set; }
        public virtual ICollection<EventBudget> EventBudgets { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; }
        public virtual ICollection<EventRole> EventRoles { get; set; } = new List<EventRole>();
        public virtual ICollection<EventMember> EventMembers { get; set; } = new List<EventMember>();
    }
}
