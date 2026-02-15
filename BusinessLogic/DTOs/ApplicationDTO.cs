using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNIC.BusinessLogic.DTOs
{
    public class CreateApplicationDto
    {
        [Required(ErrorMessage = "FormId is required")]
        public int FormId { get; set; }
        [Required(ErrorMessage = "UserId is required")]
        public Guid UserId { get; set; }
        public string? Status { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class ApplicationResponseDto
    {
        public int ApplicationId { get; set; }
        public int FormId { get; set; }
        public Guid UserId { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string Status { get; set; } = "PENDING";
        public DateTime? ReviewedAt { get; set; }
    }

    public class CreateApplicationFormDto
    {
        [Required]
        public int CampaignId { get; set; }

        [Required]
        [StringLength(200)]
        public string FormName { get; set; } = null!;

        public string? FormTitle { get; set; }
        public string? Description { get; set; }
    }

    public class ApplicationFormResponseDto
    {
        public int FormId { get; set; }
        public int CampaignId { get; set; }
        public string FormName { get; set; } = null!;
        public string? FormTitle { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateApplicationQuestionDto
    {
        [Required]
        public int FormId { get; set; }

        [Required]
        public string QuestionText { get; set; } = null!;

        [StringLength(50)]
        public string? QuestionType { get; set; }

        public bool IsRequired { get; set; } = false;

        public int? DisplayOrder { get; set; }
    }

    public class ApplicationQuestionResponseDto
    {
        public int QuestionId { get; set; }
        public int FormId { get; set; }
        public string QuestionText { get; set; } = null!;
        public string? QuestionType { get; set; }
        public bool IsRequired { get; set; }
        public int? DisplayOrder { get; set; }
    }
}
