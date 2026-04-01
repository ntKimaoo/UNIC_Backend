using System;
using DataAccess.Models.Meeting.Enums;

namespace DataAccess.Models.Meeting;

/// <summary>
/// Quyết định cuối cùng cho một ứng viên trong campaign.
/// Mỗi dòng = 1 ứng viên trong 1 campaign.
///
/// FK mềm: CampaignId, CandidateUserId, DecidedByUserId
/// FK: InterviewScheduleId → InterviewSchedule.Id
/// </summary>
public class CampaignDecision
{
    public int Id { get; set; }

    /// <summary>
    /// RecruitmentCampaigns.CampaignId
    /// </summary>
    public int CampaignId { get; set; }

    /// <summary>
    /// FK sang InterviewSchedule – buổi PV của ứng viên.
    /// </summary>
    public int InterviewScheduleId { get; set; }
    public InterviewSchedule InterviewSchedule { get; set; } = null!;

    /// <summary>
    /// Users.UserId – ứng viên.
    /// </summary>
    public Guid CandidateUserId { get; set; }

    /// <summary>
    /// Quyết định: Accept / Reject / Waitlist
    /// </summary>
    public DecisionResult Decision { get; set; }

    /// <summary>
    /// Users.UserId – người ra quyết định.
    /// </summary>
    public Guid DecidedByUserId { get; set; }

    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;

    // ── Công bố kết quả ─────────────────────────────────────────

    /// <summary>
    /// Trạng thái công bố: Draft → Scheduled → Published (hoặc Revoked)
    /// </summary>
    public PublishStatus PublishStatus { get; set; } = PublishStatus.Draft;

    /// <summary>
    /// Thời điểm hẹn công bố (nếu chọn Schedule Publish).
    /// </summary>
    public DateTime? ScheduledPublishAt { get; set; }

    /// <summary>
    /// Thời điểm thực sự công bố.
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Kênh thông báo, comma-separated. VD: "Email,InApp"
    /// </summary>
    public string? NotificationChannels { get; set; }
}
