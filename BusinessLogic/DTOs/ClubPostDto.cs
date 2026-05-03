using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    public class CreateClubPostDto
    {
        [Required]
        public int ClubId { get; set; }

        public Guid? UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public string? ImageUrl { get; set; }
        public string? Caption { get; set; }
        public string? Content { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        public int? EventId { get; set; }
        public int? CampaignId { get; set; }
    }

    public class UpdateClubPostDto
    {
        [MaxLength(200)]
        public string? Title { get; set; }

        public string? ImageUrl { get; set; }
        public string? Caption { get; set; }
        public string? Content { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        public int? EventId { get; set; }
        public int? CampaignId { get; set; }
    }

    public class ClubPostResponseDto
    {
        public int PostId { get; set; }
        public int ClubId { get; set; }
        public string ClubName { get; set; }
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public string Title { get; set; }
        public string? ImageUrl { get; set; }
        public string? Caption { get; set; }
        public string? Content { get; set; }
        public DateTime PostDate { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Status { get; set; }
        public bool IsDeleted { get; set; }
        public int? EventId { get; set; }
        public string? EventName { get; set; }
        public int? CampaignId { get; set; }
        public string? CampaignName { get; set; }
    }
}
