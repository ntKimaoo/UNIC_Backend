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

    public class CreateApplicationAnswerDto
    {
        [Required(ErrorMessage = "ApplicationId is required")]
        public int ApplicationId { get; set; }

        [Required(ErrorMessage = "QuestionId is required")]
        public int QuestionId { get; set; }

        public string? AnswerText { get; set; }
    }

    public class ApplicationAnswerResponseDto
    {
        public int AnswerId { get; set; }
        public int ApplicationId { get; set; }
        public int QuestionId { get; set; }
        public string? AnswerText { get; set; }
    }

    public class SubmitApplicationWithAnswersDto
    {
        [Required]
        public int FormId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public List<ApplicationAnswerItemDto> Answers { get; set; } = new();
    }

    public class ApplicationAnswerItemDto
    {
        [Required]
        public int QuestionId { get; set; }
        public string? AnswerText { get; set; }
    }

    public class UpdateApplicationStatusDto
    {
        [Required(ErrorMessage = "Status is required")]
        [StringLength(20)]
        public string Status { get; set; } = null!;
    }
}
