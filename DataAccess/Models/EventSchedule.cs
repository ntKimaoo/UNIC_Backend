using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class EventSchedule
    {
        [Key]
        public int ScheduleId { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ScheduleName { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public string Description { get; set; }

        [ForeignKey("EventId")]
        public virtual Event Event { get; set; }

        public virtual ICollection<ScheduleDetail> ScheduleDetails { get; set; }
    }
}