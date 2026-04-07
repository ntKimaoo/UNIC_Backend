using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class ClubPayOSSettings
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ClubId { get; set; }

        [MaxLength(100)]
        public string ClientId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ApiKey { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ChecksumKey { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public Guid? UpdatedBy { get; set; }

        [ForeignKey("ClubId")]
        public virtual Club? Club { get; set; }
    }
}

