using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    /// <summary>
    /// DTO for creating a new event
    /// </summary>
    public class CreateEventRequest
    {
        [Required(ErrorMessage = "Event name is required")]
        [MaxLength(200, ErrorMessage = "Event name cannot exceed 200 characters")]
        public string EventName { get; set; } = null!;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = null!;

        [MaxLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string? Location { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }

        public int? ClubId { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing event
    /// </summary>
    public class UpdateEventRequest
    {
        [Required(ErrorMessage = "Event ID is required")]
        public int EventId { get; set; }

        [Required(ErrorMessage = "Event name is required")]
        [MaxLength(200, ErrorMessage = "Event name cannot exceed 200 characters")]
        public string EventName { get; set; } = null!;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = null!;

        [MaxLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string? Location { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// DTO for event details including sessions
    /// </summary>
    public class EventDetailDto
    {
        public int EventId { get; set; }
        public int? ClubId { get; set; }
        public string EventName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public string? Location { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsPublic { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int? MaxAttendees { get; set; }
        public int CurrentAttendees { get; set; }
        public List<SessionDto> Sessions { get; set; } = new List<SessionDto>();
    }

    /// <summary>
    /// DTO for creating a new event session/schedule
    /// </summary>
    public class CreateSessionRequest
    {
        [Required(ErrorMessage = "Event ID is required")]
        public int EventId { get; set; }

        [Required(ErrorMessage = "Session name is required")]
        [MaxLength(100, ErrorMessage = "Session name cannot exceed 100 characters")]
        public string SessionName { get; set; } = null!;

        [Required(ErrorMessage = "Start time is required")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        public DateTime EndTime { get; set; }

        public string? Description { get; set; }

        public string? Location { get; set; }
    }

    /// <summary>
    /// DTO for opening event registration
    /// </summary>
    public class OpenRegistrationRequest
    {
        [Required(ErrorMessage = "Event ID is required")]
        public int EventId { get; set; }

        [Required(ErrorMessage = "Registration start date is required")]
        public DateTime RegistrationStartDate { get; set; }

        [Required(ErrorMessage = "Registration end date is required")]
        public DateTime RegistrationEndDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Max attendees must be greater than 0")]
        public int? MaxAttendees { get; set; }
    }

    /// <summary>
    /// DTO for event session information
    /// </summary>
    public class SessionDto
    {
        public int ScheduleId { get; set; }
        public string ScheduleName { get; set; } = null!;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
    }
}
