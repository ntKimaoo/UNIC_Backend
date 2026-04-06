using System;
using System.Collections.Generic;
using DataAccess.Models.Meeting.Enums;

namespace DataAccess.Models.Meeting;

/// <summary>
/// Assign một Interviewer (User) vào một buổi phỏng vấn.
/// Mỗi dòng = 1 interviewer, 1 schedule.
/// 
/// InterviewerUserId → Users.UserId (FK mềm)
/// </summary>
public class InterviewAssignment
{
    public int Id { get; set; }

    // ── FK sang InterviewSchedule ────────────────────────────────
    public int               InterviewScheduleId { get; set; }
    public InterviewSchedule InterviewSchedule   { get; set; } = null!;

    /// <summary>
    /// Users.UserId – interviewer được assign.
    /// </summary>
    public Guid InterviewerUserId { get; set; }

    // ── Vai trò ──────────────────────────────────────────────────
    public InterviewerRole Role         { get; set; } = InterviewerRole.Interviewer;
    public bool            HasConfirmed { get; set; } = false;

    // ── Feedback & kết quả ───────────────────────────────────────
    public string?          FeedbackNotes        { get; set; }
    public InterviewResult? Result               { get; set; }   // null = chưa đánh giá

    public DateTime  AssignedAt          { get; set; } = DateTime.UtcNow;
    public DateTime? FeedbackSubmittedAt { get; set; }

    // ── Navigation ───────────────────────────────────────────────
    public ICollection<CriteriaScore> CriteriaScores { get; set; } = new List<CriteriaScore>();
}
