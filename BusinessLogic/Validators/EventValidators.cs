using BusinessLogic.DTOs;
using FluentValidation;
using System;

namespace BusinessLogic.Validators
{
    /// <summary>
    /// Validator for CreateEventRequest
    /// </summary>
    public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
    {
        public CreateEventRequestValidator()
        {
            RuleFor(x => x.EventName)
                .NotEmpty().WithMessage("Event name is required")
                .MaximumLength(200).WithMessage("Event name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required");

            RuleFor(x => x.Location)
                .MaximumLength(200).WithMessage("Location cannot exceed 200 characters")
                .When(x => !string.IsNullOrEmpty(x.Location));

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Start date is required")
                .LessThan(x => x.EndDate).WithMessage("Start date must be before end date");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("End date is required")
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date");

            RuleFor(x => x.ClubId)
                .GreaterThan(0).WithMessage("Club ID must be greater than 0")
                .When(x => x.ClubId.HasValue);
        }
    }

    /// <summary>
    /// Validator for UpdateEventRequest
    /// </summary>
    public class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
    {
        public UpdateEventRequestValidator()
        {
            RuleFor(x => x.EventId)
                .NotEmpty().WithMessage("Event ID is required")
                .GreaterThan(0).WithMessage("Event ID must be greater than 0");

            RuleFor(x => x.EventName)
                .NotEmpty().WithMessage("Event name is required")
                .MaximumLength(200).WithMessage("Event name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required");

            RuleFor(x => x.Location)
                .MaximumLength(200).WithMessage("Location cannot exceed 200 characters")
                .When(x => !string.IsNullOrEmpty(x.Location));

            RuleFor(x => x.StartDate)
                .LessThan(x => x.EndDate).WithMessage("Start date must be before end date")
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date")
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
        }
    }

    /// <summary>
    /// Validator for CreateSessionRequest
    /// </summary>
    public class CreateSessionValidator : AbstractValidator<CreateSessionRequest>
    {
        public CreateSessionValidator()
        {
            RuleFor(x => x.EventId)
                .NotEmpty().WithMessage("Event ID is required")
                .GreaterThan(0).WithMessage("Event ID must be greater than 0");

            RuleFor(x => x.SessionName)
                .NotEmpty().WithMessage("Session name is required")
                .MaximumLength(100).WithMessage("Session name cannot exceed 100 characters");

            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("Start time is required")
                .LessThan(x => x.EndTime).WithMessage("Start time must be before end time");

            RuleFor(x => x.EndTime)
                .NotEmpty().WithMessage("End time is required")
                .GreaterThan(x => x.StartTime).WithMessage("End time must be after start time");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }

    /// <summary>
    /// Validator for OpenRegistrationRequest
    /// </summary>
    public class OpenRegistrationValidator : AbstractValidator<OpenRegistrationRequest>
    {
        public OpenRegistrationValidator()
        {
            RuleFor(x => x.EventId)
                .NotEmpty().WithMessage("Event ID is required")
                .GreaterThan(0).WithMessage("Event ID must be greater than 0");

            RuleFor(x => x.RegistrationStartDate)
                .NotEmpty().WithMessage("Registration start date is required")
                .LessThan(x => x.RegistrationEndDate).WithMessage("Registration start date must be before registration end date");

            RuleFor(x => x.RegistrationEndDate)
                .NotEmpty().WithMessage("Registration end date is required")
                .GreaterThan(x => x.RegistrationStartDate).WithMessage("Registration end date must be after registration start date");

            // Note: Validation that RegistrationEndDate < Event.StartDate should be done in the service layer
            // as it requires database access to get the Event.StartDate

            RuleFor(x => x.MaxAttendees)
                .GreaterThan(0).WithMessage("Max attendees must be greater than 0")
                .When(x => x.MaxAttendees.HasValue);
        }
    }
}
