using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class Attendance
    {
        [Key]
        public int AttendId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public int EventId { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string AttendanceStatus { get; set; } = "REGISTERED";

        // Check-in fields
        public DateTime? CheckInTime { get; set; }

        // Evaluation fields
        public int? Score { get; set; }
        [MaxLength(500)]
        public string? Comment { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("EventId")]
        public virtual Event Event { get; set; }
    }
}