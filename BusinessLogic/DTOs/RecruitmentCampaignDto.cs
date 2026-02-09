using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    public class CreateRecruitmentCampaignDto
    {
        [Required]
        public int ClubId { get; set; }

        [Required]
        [MaxLength(200)]
        public string CampaignName { get; set; }

        public string? LinkCampaign { get; set; }
        
        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "OPEN";

        public string? ImageUrl { get; set; }

        public string? Content { get; set; }
    }

    public class UpdateRecruitmentCampaignDto
    {
        [MaxLength(200)]
        public string? CampaignName { get; set; }

        public string? LinkCampaign { get; set; }
        
        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        public string? ImageUrl { get; set; }

        public string? Content { get; set; }
    }

    public class RecruitmentCampaignResponseDto
    {
        public int CampaignId { get; set; }
        public int ClubId { get; set; }
        public string CampaignName { get; set; }
        public string? LinkCampaign { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }
        public string? ImageUrl { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
