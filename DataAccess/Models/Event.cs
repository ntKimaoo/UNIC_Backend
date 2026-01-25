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

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        public virtual ICollection<EventSchedule> EventSchedules { get; set; }
        public virtual ICollection<EventImage> EventImages { get; set; }
        public virtual ICollection<EventBudget> EventBudgets { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; }
    }
}
