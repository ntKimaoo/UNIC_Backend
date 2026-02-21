using System;
using System.Collections.Generic;
using DataAccess.Models.Meeting.Enums;

namespace DataAccess.Models.Meeting;

/// <summary>
/// Lịch phỏng vấn cho một đơn ứng tuyển (Application).
/// 
/// Quan hệ với DB hiện có:
///   - ApplicationId  → Applications.ApplicationId   (FK mềm)
///   - CandidateUserId → Users.UserId                (FK mềm, ứng viên)
///   - CreatedByUserId → Users.UserId                (FK mềm, người tạo lịch)
///   - CampaignId      → RecruitmentCampaigns.CampaignId (FK mềm, để biết lịch thuộc campaign nào)
/// 
/// Dùng FK mềm (không HasForeignKey thật) vì các bảng trên
/// nằm ở DbContext khác. Chỉ lưu Guid as string.
/// </summary>
public class InterviewSchedule
{
    public int Id { get; set; }

    // ── FK mềm sang DB hiện có ───────────────────────────────────

    /// <summary>
    /// Applications.ApplicationId – đơn ứng tuyển được mời phỏng vấn.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Users.UserId – ứng viên (người nộp đơn).
    /// Snapshot để tiện query, không cần join sang DB kia.
    /// </summary>
    public Guid CandidateUserId { get; set; }

    /// <summary>
    /// RecruitmentCampaigns.CampaignId – campaign tuyển dụng liên quan.
    /// </summary>
    public Guid CampaignId { get; set; }

    /// <summary>
    /// Users.UserId – người tạo lịch phỏng vấn (HR / Club admin).
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    // ── Thông tin buổi phỏng vấn ─────────────────────────────────

    public string  Title            { get; set; } = null!;   // "Vòng 1 – Năng lực"
    public string? Description      { get; set; }

    public DateTime ScheduledAt     { get; set; }            // Thời điểm bắt đầu
    public int      DurationMinutes { get; set; } = 60;

    // ── Trạng thái ───────────────────────────────────────────────

    public InterviewStatus Status       { get; set; } = InterviewStatus.Scheduled;
    public string?         CancelReason { get; set; }

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // ── Navigation ───────────────────────────────────────────────

    public ICollection<InterviewAssignment> Assignments { get; set; } = new List<InterviewAssignment>();
    public MeetingRoom?                     MeetingRoom { get; set; }
}
