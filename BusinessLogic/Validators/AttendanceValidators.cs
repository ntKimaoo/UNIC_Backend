using BusinessLogic.DTOs;
using FluentValidation;
using System;

namespace BusinessLogic.Validators
{
    /// <summary>
    /// Validator for EventRegistrationRequest
    /// </summary>
    public class EventRegistrationRequestValidator : AbstractValidator<EventRegistrationRequest>
    {
        public EventRegistrationRequestValidator()
        {
            RuleFor(x => x.EventId)
                .NotEmpty().WithMessage("Event ID is required")
                .GreaterThan(0).WithMessage("Event ID must be greater than 0");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    /// <summary>
    /// Validator for CheckInRequest
    /// </summary>
    public class CheckInRequestValidator : AbstractValidator<CheckInRequest>
    {
        public CheckInRequestValidator()
        {
            RuleFor(x => x.EventId)
                .NotEmpty().WithMessage("Event ID is required")
                .GreaterThan(0).WithMessage("Event ID must be greater than 0");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Check-in code is required")
                .NotNull().WithMessage("Check-in code cannot be null");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90")
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180")
                .When(x => x.Longitude.HasValue);
        }
    }

    /// <summary>
    /// Validator for EvaluateMemberRequest
    /// </summary>
    public class EvaluateMemberRequestValidator : AbstractValidator<EvaluateMemberRequest>
    {
        public EvaluateMemberRequestValidator()
        {
            RuleFor(x => x.EventId)
                .NotEmpty().WithMessage("Event ID is required")
                .GreaterThan(0).WithMessage("Event ID must be greater than 0");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Score)
                .NotEmpty().WithMessage("Score is required")
                .InclusiveBetween(0, 100).WithMessage("Score must be between 0 and 100");

            RuleFor(x => x.Comment)
                .MaximumLength(500).WithMessage("Comment cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Comment));
        }
    }
}
