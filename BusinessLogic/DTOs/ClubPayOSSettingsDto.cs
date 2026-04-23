using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    public class UpsertClubPayOSSettingsDto
    {
        [MaxLength(32)]
        public string? PaymentProvider { get; set; }

        [MaxLength(100)]
        public string ClientId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ApiKey { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ChecksumKey { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;
    }

    public class ClubPayOSSettingsResponseDto
    {
        public int ClubId { get; set; }
        public string PaymentProvider { get; set; } = "PAYOS";
        public bool IsConfigured { get; set; }
        public bool IsEnabled { get; set; }
        public string? ClientId { get; set; }
        public string? ApiKeyMasked { get; set; }
        public string? ChecksumKeyMasked { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}

