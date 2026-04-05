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
        public DateTime AssignedAt { get; set; }
        public DateTime? FeedbackSubmittedAt { get; set; }
        public List<CriteriaScoreResponseDto> CriteriaScores { get; set; } = new();
    }

    public class CriteriaScoreResponseDto
    {
        public int Id { get; set; }
        public int EvaluationCriterionId { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  MEETING ROOM
    // ═══════════════════════════════════════════════════════════════

    public class MeetingRoomResponseDto
    {
        public int Id { get; set; }
        public string RoomType { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public Guid CreatedByUserId { get; set; }
        public DateTime? ScheduledStartAt { get; set; }
        public DateTime? ScheduledEndAt { get; set; }
        public int? InterviewScheduleId { get; set; }
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

    public class CreateMeetingRoomDto
    {
        /// <summary>
        /// Interview, Internal, Training, General
        /// </summary>
        [Required]
        public string RoomType { get; set; } = "General";

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        public Guid CreatedByUserId { get; set; }

        public DateTime? ScheduledStartAt { get; set; }
        public DateTime? ScheduledEndAt { get; set; }

        /// <summary>
        /// Chỉ cần khi RoomType = Interview
        /// </summary>
        public int? InterviewScheduleId { get; set; }

        public int MaxParticipants { get; set; } = 10;
        public bool IsWaitingRoomEnabled { get; set; } = true;
        public bool IsRecordingEnabled { get; set; } = false;
    }

    public class JoinRoomDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = null!;

        /// <summary>
        /// "Interviewer" | "Candidate" | "Observer" | "Host" | "Participant" | "Guest"
        /// </summary>
        public string Role { get; set; } = "Participant";
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
    }

    public class UpdateEvaluationCriterionDto
    {
        [MaxLength(200)]
        public string? Name { get; set; }
        public string? Description { get; set; }

        [Range(1, 100)]
        public int? Weight { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  CRITERIA ASSIGNMENT & FEEDBACK
    // ═══════════════════════════════════════════════════════════════

    public class AssignCriteriaDto
    {
        /// <summary>
        /// Danh sách ID tiêu chí giao cho interviewer này.
        /// </summary>
        [Required]
        public List<int> CriteriaIds { get; set; } = new();
    }

    public class CriteriaNoteItemDto
    {
        [Required]
        public int CriterionId { get; set; }

        public string? Note { get; set; }
    }

    public class SubmitCriteriaFeedbackDto
    {
        /// <summary>
        /// Nhận xét theo từng tiêu chí.
        /// </summary>
        [Required]
        public List<CriteriaNoteItemDto> Notes { get; set; } = new();

        public string? FeedbackNotes { get; set; }

        /// <summary>
        /// Pass, Fail, OnHold, NoShow
        /// </summary>
        [Required]
        public string Result { get; set; } = null!;
    }

    /// <summary>
    /// Nhận xét 1 tiêu chí bởi 1 interviewer.
    /// </summary>
    public class CriteriaNoteResultDto
    {
        public int CriterionId { get; set; }
        public string CriterionName { get; set; } = null!;
        public int Weight { get; set; }
        public string? Note { get; set; }
        public Guid InterviewerUserId { get; set; }
        public string InterviewerRole { get; set; } = null!;
    }

    /// <summary>
    /// Tổng hợp đánh giá cho 1 buổi PV: nhận xét từng tiêu chí.
    /// </summary>
    public class EvaluationSummaryDto
    {
        public int InterviewScheduleId { get; set; }
        public string Title { get; set; } = null!;
        public Guid CandidateUserId { get; set; }
        public int CampaignId { get; set; }

        /// <summary>
        /// Danh sách tiêu chí + nhận xét từng người.
        /// </summary>
        public List<CriteriaSummaryItemDto> CriteriaSummaries { get; set; } = new();

        public List<InterviewAssignmentResponseDto> Feedbacks { get; set; } = new();
    }

    public class CriteriaSummaryItemDto
    {
        public int CriterionId { get; set; }
        public string CriterionName { get; set; } = null!;
        public int Weight { get; set; }

        /// <summary>
        /// Nhận xét từng interviewer cho tiêu chí này.
        /// </summary>
        public List<CriteriaNoteResultDto> IndividualNotes { get; set; } = new();
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
        /// Nhận xét tổng hợp từ các interviewer.
        /// </summary>
        public List<string> FeedbackNotes { get; set; } = new();

        /// <summary>
        /// Nhận xét từng tiêu chí.
        /// </summary>
        public List<CriteriaSummaryItemDto> CriteriaSummaries { get; set; } = new();
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

    // ═══════════════════════════════════════════════════════════════
    //  AI ANALYSIS (OpenRouter AI Model)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Phân tích AI cho 1 ứng viên — trả về từ backend AI model.
    /// </summary>
    public class AiCandidateAnalysisDto
    {
        public int InterviewScheduleId { get; set; }
        public Guid CandidateUserId { get; set; }
        public string CandidateName { get; set; } = null!;

        /// <summary> StrongFit, MediumFit, WeakFit, NoData </summary>
        public string FitLevel { get; set; } = "NoData";

        /// <summary> Accept, Reject, Waitlist, Undecided </summary>
        public string SuggestedResult { get; set; } = "Undecided";

        /// <summary> AI-generated summary text. </summary>
        public string SummaryText { get; set; } = string.Empty;

        public List<AiCriteriaSentimentDto> CriteriaSentiments { get; set; } = new();
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
    }

    /// <summary>
    /// Sentiment AI cho 1 tiêu chí của ứng viên.
    /// </summary>
    public class AiCriteriaSentimentDto
    {
        public int CriterionId { get; set; }
        public string CriterionName { get; set; } = null!;

        /// <summary> positive, negative, neutral </summary>
        public string Sentiment { get; set; } = "neutral";

        /// <summary> Confidence 0–1. </summary>
        public double Confidence { get; set; }

        public string? Explanation { get; set; }
    }

    /// <summary>
    /// Response tổng cho AI campaign analysis.
    /// </summary>
    public class AiCampaignAnalysisResponseDto
    {
        public int CampaignId { get; set; }
        public string AnalyzedAt { get; set; } = null!;
        public List<AiCandidateAnalysisDto> Candidates { get; set; } = new();
    }

    /// <summary>
    /// Request body cho AI search — ngôn ngữ tự nhiên.
    /// </summary>
    public class AiSearchRequestDto
    {
        /// <summary> Natural language query. VD: "ứng viên giỏi code nhất" </summary>
        [Required]
        public string Query { get; set; } = null!;

        /// <summary> Max kết quả trả về. </summary>
        public int TopK { get; set; } = 10;
    }

    /// <summary>
    /// Kết quả AI search cho 1 ứng viên.
    /// </summary>
    public class AiSearchCandidateDto
    {
        public int InterviewScheduleId { get; set; }
        public Guid CandidateUserId { get; set; }
        public string CandidateName { get; set; } = null!;

        /// <summary> Relevance score 0–1. </summary>
        public double RelevanceScore { get; set; }

        /// <summary> AI explanation tại sao match. </summary>
        public string MatchReason { get; set; } = string.Empty;

        public string SuggestedResult { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response tổng cho AI search.
    /// </summary>
    public class AiSearchResponseDto
    {
        public string Query { get; set; } = null!;
        public List<AiSearchCandidateDto> Results { get; set; } = new();
        public int TotalFound { get; set; }

        /// <summary> Overall AI explanation. </summary>
        public string AiExplanation { get; set; } = string.Empty;
    }
}

