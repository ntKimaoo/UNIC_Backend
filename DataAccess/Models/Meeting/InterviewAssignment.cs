using System;
using DataAccess.Models.Meeting.Enums;

namespace DataAccess.Models.Meeting;

/// <summary>
/// Assign một User (Interviewer) vào một InterviewSchedule.
/// </summary>
public class InterviewAssignment
{
    public int Id { get; set; }

    // ── FK sang InterviewSchedule ────────────────────────────────
    public int               InterviewScheduleId { get; set; }
    public InterviewSchedule InterviewSchedule   { get; set; } = null!;

    // ── Interviewer – FK mềm sang bảng User của bạn ─────────────
    public string InterviewerUserId { get; set; } = null!;  // Guid as string

    // ── Vai trò ──────────────────────────────────────────────────
    public InterviewerRole Role         { get; set; } = InterviewerRole.Interviewer;
    public bool            HasConfirmed { get; set; } = false;

    // ── Feedback & kết quả ───────────────────────────────────────
    public string?          FeedbackNotes        { get; set; }
    public InterviewResult? Result               { get; set; }
    public int?             Score                { get; set; }  // 0–100

    public DateTime  AssignedAt          { get; set; } = DateTime.UtcNow;
    public DateTime? FeedbackSubmittedAt { get; set; }
}
