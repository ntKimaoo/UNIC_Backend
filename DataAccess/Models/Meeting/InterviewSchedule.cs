using System;
using System.Collections.Generic;
using DataAccess.Models.Meeting.Enums;

namespace DataAccess.Models.Meeting;

/// <summary>
/// Lịch phỏng vấn – 1 schedule có 1 MeetingRoom WebRTC đi kèm.
/// </summary>
public class InterviewSchedule
{
    public int    Id          { get; set; }

    // ── Ứng viên ────────────────────────────────────────────────
    public int       CandidateId { get; set; }
    public Candidate Candidate   { get; set; } = null!;

    // ── Người tạo lịch – FK mềm sang bảng User của bạn ──────────
    public string CreatedByUserId { get; set; } = null!;   // Guid as string

    // ── Thông tin buổi phỏng vấn ─────────────────────────────────
    public string  Title            { get; set; } = null!;
    public string? Description      { get; set; }

    public DateTime ScheduledAt     { get; set; }
    public int      DurationMinutes { get; set; } = 60;

    // ── Trạng thái ───────────────────────────────────────────────
    public InterviewStatus Status       { get; set; } = InterviewStatus.Scheduled;
    public string?         CancelReason { get; set; }

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<InterviewAssignment> Assignments { get; set; } = new List<InterviewAssignment>();
    public MeetingRoom?                     MeetingRoom { get; set; }  // 1-1
}
