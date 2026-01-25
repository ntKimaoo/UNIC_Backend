using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class ScheduleDetail
    {
        [Key]
        public int DetailId { get; set; }

        [Required]
        public int ScheduleId { get; set; }

        [MaxLength(100)]
        public string ActivityName { get; set; }

        public string Description { get; set; }

        public int? Duration { get; set; }

        [ForeignKey("ScheduleId")]
        public virtual EventSchedule EventSchedule { get; set; }
    }
}