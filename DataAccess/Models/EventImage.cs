using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class EventImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; }

        public string Caption { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        [ForeignKey("EventId")]
        public virtual Event Event { get; set; }
    }
}