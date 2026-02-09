using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    public class CreateClubDto
    {
        [Required]
        [MaxLength(100)]
        public string ClubName { get; set; }

        [MaxLength(50)]
        public string? ShortName { get; set; }

        public string? Description { get; set; }

        public DateTime? FoundedDate { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsPublic { get; set; } = true;

        public string? LogoUrl { get; set; }

        public string? CoverImageUrl { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? FacebookUrl { get; set; }

        public string? WebsiteUrl { get; set; }

        public string? Address { get; set; }
    }

    public class UpdateClubDto
    {
        [MaxLength(100)]
        public string? ClubName { get; set; }

        [MaxLength(50)]
        public string? ShortName { get; set; }

        public string? Description { get; set; }

        public DateTime? FoundedDate { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        public bool? IsPublic { get; set; }

        public string? LogoUrl { get; set; }

        public string? CoverImageUrl { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? FacebookUrl { get; set; }

        public string? WebsiteUrl { get; set; }

        public string? Address { get; set; }

        public bool? IsActive { get; set; }
    }

    public class ClubResponseDto
    {
        public int ClubId { get; set; }
        public string ClubName { get; set; }
        public string? ShortName { get; set; }
        public string? Description { get; set; }
        public DateTime? FoundedDate { get; set; }
        public string Status { get; set; }
        public bool IsPublic { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FacebookUrl { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? Address { get; set; }
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }
}
