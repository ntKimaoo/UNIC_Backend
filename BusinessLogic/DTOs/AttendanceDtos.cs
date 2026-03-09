using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    /// <summary>
    /// DTO for registering a user to an event
    /// </summary>
    public class EventRegistrationRequest
    {
        [Required(ErrorMessage = "Event ID is required")]
        public int EventId { get; set; }

        [Required(ErrorMessage = "User ID is required")]
        public Guid UserId { get; set; }
    }

    /// <summary>
    /// DTO for event check-in with QR code and location
    /// </summary>
    public class CheckInRequest
    {
        [Required(ErrorMessage = "Event ID is required")]
        public int EventId { get; set; }

        [Required(ErrorMessage = "User ID is required")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Check-in code is required")]
        public string Code { get; set; } = null!;

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double? Longitude { get; set; }
    }

    /// <summary>
    /// DTO for check-in QR code response
    /// </summary>
    public class CheckInCodeResponse
    {
        public int EventId { get; set; }
        public string Code { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public string QrContent { get; set; } = null!;
    }

    /// <summary>
    /// Response for participant's personal QR code (content to encode in QR for event check-in).
    /// </summary>
    public class CheckInQrResponse
    {
        public int EventId { get; set; }
        public string QrContent { get; set; } = null!;
    }

    /// <summary>
    /// Request when organizer scans a participant's QR code to check them in.
    /// </summary>
    public class CheckInByQrRequest
    {
        [Required(ErrorMessage = "Token (from QR) is required")]
        [MaxLength(64)]
        public string Token { get; set; } = null!;
    }

    /// <summary>
    /// Response after scanning QR to check in a participant.
    /// </summary>
    public class CheckInByQrResponse
    {
        public string Message { get; set; } = null!;
        public string MemberName { get; set; } = null!;
        public bool AlreadyCheckedIn { get; set; }
    }

    public class VerifyByLinkResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public bool AlreadyCheckedIn { get; set; }
        public string? MemberName { get; set; }
        public string? EventName { get; set; }
    }

    /// <summary>
    /// DTO for evaluating member performance at an event
    /// </summary>
    public class EvaluateMemberRequest
    {
        [Required(ErrorMessage = "Event ID is required")]
        public int EventId { get; set; }

        [Required(ErrorMessage = "User ID is required")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Score is required")]
        [Range(0, 100, ErrorMessage = "Score must be between 0 and 100")]
        public int Score { get; set; }

        [MaxLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        public string? Comment { get; set; }
    }

    /// <summary>
    /// DTO for attendance details with member information
    /// </summary>
    public class AttendanceDetailDto
    {
        public int AttendId { get; set; }
        public int EventId { get; set; }
        public Guid UserId { get; set; }
        public string MemberName { get; set; } = null!;
        public string? StudentId { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string AttendanceStatus { get; set; } = null!;
        public DateTime? CheckInTime { get; set; }
        public int? Score { get; set; }
        public string? Comment { get; set; }
    }

    
}
