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

    // ═══════════════════════════════════════════════════════════════
    //  EVALUATION CRITERIA
    // ═══════════════════════════════════════════════════════════════

    public class EvaluationCriterionDto
    {
        public int Id { get; set; }
        public int CampaignId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int Weight { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsDefault { get; set; }
    }

    public class CreateEvaluationCriterionDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        [Range(1, 100)]
        public int Weight { get; set; }

        public int DisplayOrder { get; set; }
    }

    public class UpdateEvaluationCriterionDto
    {
        [MaxLength(200)]
        public string? Name { get; set; }
        public string? Description { get; set; }

        [Range(1, 100)]
        public int? Weight { get; set; }
        public int? DisplayOrder { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  CRITERIA ASSIGNMENT & SCORING
    // ═══════════════════════════════════════════════════════════════

    public class AssignCriteriaDto
    {
        /// <summary>
        /// Danh sách ID tiêu chí giao cho interviewer này.
        /// </summary>
        [Required]
        public List<int> CriteriaIds { get; set; } = new();
    }

    public class CriteriaScoreItemDto
    {
        [Required]
        public int CriterionId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Score { get; set; }

        public string? Note { get; set; }
    }

    public class SubmitCriteriaFeedbackDto
    {
        /// <summary>
        /// Điểm theo từng tiêu chí (1–5 sao).
        /// </summary>
        [Required]
        public List<CriteriaScoreItemDto> Scores { get; set; } = new();

        public string? FeedbackNotes { get; set; }

        /// <summary>
        /// Pass, Fail, OnHold, NoShow
        /// </summary>
        [Required]
        public string Result { get; set; } = null!;
    }

    /// <summary>
    /// Kết quả chấm 1 tiêu chí bởi 1 interviewer.
    /// </summary>
    public class CriteriaScoreResultDto
    {
        public int CriterionId { get; set; }
        public string CriterionName { get; set; } = null!;
        public int Weight { get; set; }
        public int Score { get; set; }
        public string? Note { get; set; }
        public Guid InterviewerUserId { get; set; }
        public string InterviewerRole { get; set; } = null!;
    }

    /// <summary>
    /// Tổng hợp đánh giá cho 1 buổi PV: điểm TB mỗi tiêu chí, tổng điểm, đề xuất.
    /// </summary>
    public class EvaluationSummaryDto
    {
        public int InterviewScheduleId { get; set; }
        public string Title { get; set; } = null!;
        public Guid CandidateUserId { get; set; }
        public int CampaignId { get; set; }

        /// <summary>
        /// Danh sách tiêu chí + điểm từng người chấm.
        /// </summary>
        public List<CriteriaSummaryItemDto> CriteriaSummaries { get; set; } = new();

        /// <summary>
        /// Điểm tổng (0–100), tính theo trọng số.
        /// </summary>
        public double TotalScore { get; set; }

        /// <summary>
        /// Đề xuất tự động: Pass / OnHold / Fail
        /// </summary>
        public string SuggestedResult { get; set; } = null!;

        public List<InterviewAssignmentResponseDto> Feedbacks { get; set; } = new();
    }

    public class CriteriaSummaryItemDto
    {
        public int CriterionId { get; set; }
        public string CriterionName { get; set; } = null!;
        public int Weight { get; set; }

        /// <summary>
        /// Điểm trung bình (mean of all interviewers).
        /// </summary>
        public double AverageScore { get; set; }

        /// <summary>
        /// Chi tiết điểm từng interviewer.
        /// </summary>
        public List<CriteriaScoreResultDto> IndividualScores { get; set; } = new();
    }

    // ═══════════════════════════════════════════════════════════════
    //  CANDIDATE COMPARISON
    // ═══════════════════════════════════════════════════════════════

    public class CandidateComparisonItemDto
    {
        public int InterviewScheduleId { get; set; }
        public Guid CandidateUserId { get; set; }
        public string Title { get; set; } = null!;

        /// <summary>
        /// Điểm từng tiêu chí (đã lấy trung bình).
        /// Key = CriterionId, Value = AverageScore
        /// </summary>
        public Dictionary<int, double> CriteriaScores { get; set; } = new();

        public double TotalScore { get; set; }
        public int Rank { get; set; }
        public string SuggestedResult { get; set; } = null!;
    }

    // ═══════════════════════════════════════════════════════════════
    //  DECISIONS & PUBLISH
    // ═══════════════════════════════════════════════════════════════

    public class CampaignDecisionItemDto
    {
        [Required]
        public int InterviewScheduleId { get; set; }

        [Required]
        public Guid CandidateUserId { get; set; }

        /// <summary>
        /// Accept, Reject, Waitlist
        /// </summary>
        [Required]
        public string Decision { get; set; } = null!;
    }

    public class SubmitDecisionsDto
    {
        [Required]
        public Guid DecidedByUserId { get; set; }

        [Required]
        public List<CampaignDecisionItemDto> Decisions { get; set; } = new();
    }

    public class PublishResultDto
    {
        /// <summary>
        /// "Now" = công bố ngay, "Schedule" = lên lịch
        /// </summary>
        [Required]
        public string Mode { get; set; } = null!; // "Now" | "Schedule"

        /// <summary>
        /// Bắt buộc khi Mode = "Schedule"
        /// </summary>
        public DateTime? ScheduledAt { get; set; }

        /// <summary>
        /// Kênh gửi thông báo, VD: "Email,InApp"
        /// </summary>
        public string? NotificationChannels { get; set; }
    }

    public class CampaignDecisionResponseDto
    {
        public int Id { get; set; }
        public int CampaignId { get; set; }
        public int InterviewScheduleId { get; set; }
        public Guid CandidateUserId { get; set; }
        public string Decision { get; set; } = null!;
        public Guid DecidedByUserId { get; set; }
        public DateTime DecidedAt { get; set; }
        public string PublishStatus { get; set; } = null!;
        public DateTime? ScheduledPublishAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }

    public class PublishStatusResponseDto
    {
        public int CampaignId { get; set; }
        public string OverallStatus { get; set; } = null!;
        public int TotalDecisions { get; set; }
        public int AcceptCount { get; set; }
        public int RejectCount { get; set; }
        public int WaitlistCount { get; set; }
        public DateTime? ScheduledPublishAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public List<CampaignDecisionResponseDto> Decisions { get; set; } = new();
    }
}

