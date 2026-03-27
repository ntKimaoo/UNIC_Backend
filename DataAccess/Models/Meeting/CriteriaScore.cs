using System;

namespace DataAccess.Models.Meeting;

/// <summary>
/// Điểm đánh giá theo từng tiêu chí của một interviewer.
/// Mỗi dòng = 1 interviewer chấm 1 tiêu chí cho 1 buổi PV.
///
/// FK: InterviewAssignmentId → InterviewAssignment.Id
/// FK: EvaluationCriterionId → EvaluationCriterion.Id
/// </summary>
public class CriteriaScore
{
    public int Id { get; set; }

    // ── FK sang InterviewAssignment ──────────────────────────────
    public int InterviewAssignmentId { get; set; }
    public InterviewAssignment InterviewAssignment { get; set; } = null!;

    // ── FK sang EvaluationCriterion ──────────────────────────────
    public int EvaluationCriterionId { get; set; }
    public EvaluationCriterion EvaluationCriterion { get; set; } = null!;

    /// <summary>
    /// Điểm 1–5 (thang sao).
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Nhận xét riêng cho tiêu chí này.
    /// </summary>
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
