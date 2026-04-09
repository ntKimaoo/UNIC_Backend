using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    // ═══════════════════════════════════════════════════════════════
    //  INTERVIEW SCHEDULE
    // ═══════════════════════════════════════════════════════════════

    public class CreateInterviewScheduleDto
    {
        [Required]
        public int ApplicationId { get; set; }

        [Required]
        public Guid CandidateUserId { get; set; }

        [Required]
        public int CampaignId { get; set; }

        [Required]
        public Guid CreatedByUserId { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        public DateTime ScheduledAt { get; set; }

        public int DurationMinutes { get; set; } = 60;

        /// <summary>
        /// Danh sách interviewer assign ngay khi tạo lịch (tuỳ chọn).
        /// </summary>
        public List<AssignInterviewerItemDto>? Interviewers { get; set; }
    }

    public class UpdateInterviewScheduleDto
    {
        [MaxLength(300)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public DateTime? ScheduledAt { get; set; }

        public int? DurationMinutes { get; set; }
    }

    public class UpdateInterviewStatusDto
    {
        /// <summary>
        /// Giá trị: Confirmed, Cancelled, Rescheduled
        /// </summary>
        [Required]
        public string Status { get; set; } = null!;

        /// <summary>
        /// Bắt buộc khi status = Cancelled
        /// </summary>
        public string? CancelReason { get; set; }
    }

    public class InterviewScheduleResponseDto
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public Guid CandidateUserId { get; set; }
        public int CampaignId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; }
        public string Status { get; set; } = null!;
        public string? CancelReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<InterviewAssignmentResponseDto> Assignments { get; set; } = new();
        public MeetingRoomResponseDto? MeetingRoom { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  INTERVIEWER ASSIGNMENT
    // ═══════════════════════════════════════════════════════════════

    public class AssignInterviewerItemDto
    {
        [Required]
        public Guid InterviewerUserId { get; set; }

        /// <summary>
        /// Interviewer, Lead, Observer, HRRepresentative
        /// </summary>
        public string Role { get; set; } = "Interviewer";
    }

    public class AssignInterviewersDto
    {
        [Required]
        public List<AssignInterviewerItemDto> Interviewers { get; set; } = new();
    }

    public class InterviewAssignmentResponseDto
    {
        public int Id { get; set; }
        public int InterviewScheduleId { get; set; }
        public Guid InterviewerUserId { get; set; }
        public string Role { get; set; } = null!;
        public bool HasConfirmed { get; set; }
        public string? FeedbackNotes { get; set; }
        public string? Result { get; set; }
        public int? Score { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? FeedbackSubmittedAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  MEETING ROOM
    // ═══════════════════════════════════════════════════════════════

    public class MeetingRoomResponseDto
    {
        public int Id { get; set; }
        public int InterviewScheduleId { get; set; }
        public string RoomCode { get; set; } = null!;
        public string? StunServerUri { get; set; }
        public string? TurnServerUri { get; set; }
        public string? TurnUsername { get; set; }
        public string? TurnCredential { get; set; }
        public DateTime? TurnCredentialExpiresAt { get; set; }
        public bool IsRecordingEnabled { get; set; }
        public bool IsWaitingRoomEnabled { get; set; }
        public int MaxParticipants { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class JoinRoomDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = null!;

        /// <summary>
        /// "Interviewer" | "Candidate" | "Observer"
        /// </summary>
        public string Role { get; set; } = "Candidate";
    }

    public class JoinRoomResponseDto
    {
        public string RoomCode { get; set; } = null!;
        public string? PeerId { get; set; }
        public string? StunServerUri { get; set; }
        public string? TurnServerUri { get; set; }
        public string? TurnUsername { get; set; }
        public string? TurnCredential { get; set; }
        public DateTime? TurnCredentialExpiresAt { get; set; }
        public string RoomStatus { get; set; } = null!;
        public List<RoomParticipantResponseDto> CurrentParticipants { get; set; } = new();
    }

    public class LeaveRoomDto
    {
        [Required]
        public Guid UserId { get; set; }
    }

    public class RoomParticipantResponseDto
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? PeerId { get; set; }
        public string ConnectionState { get; set; } = null!;
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
    }

    public class RoomEventResponseDto
    {
        public int Id { get; set; }
        public int MeetingRoomId { get; set; }
        public Guid? ActorUserId { get; set; }
        public string EventType { get; set; } = null!;
        public string? Payload { get; set; }
        public DateTime OccurredAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  FEEDBACK
    // ═══════════════════════════════════════════════════════════════

    public class SubmitFeedbackDto
    {
        public string? FeedbackNotes { get; set; }

        /// <summary>
        /// Pass, Fail, OnHold, NoShow
        /// </summary>
        [Required]
        public string Result { get; set; } = null!;

        /// <summary>
        /// 0–100
        /// </summary>
        [Range(0, 100)]
        public int? Score { get; set; }
    }

    public class FeedbackSummaryResponseDto
    {
        public int InterviewScheduleId { get; set; }
        public string Title { get; set; } = null!;
        public List<InterviewAssignmentResponseDto> Feedbacks { get; set; } = new();
    }
}
