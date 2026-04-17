using System;

namespace DataAccess.Models.Meeting;

/// <summary>
/// Khung giờ đề xuất cho ứng viên chọn.
/// Mỗi InterviewSchedule có thể có nhiều ProposedTimeSlot.
/// Khi ứng viên confirm, slot được chọn sẽ có IsSelected = true
/// và ScheduledAt của InterviewSchedule sẽ được cập nhật tương ứng.
/// </summary>
public class ProposedTimeSlot
{
    public int Id { get; set; }

    // ── FK sang InterviewSchedule ────────────────────────────────
    public int               InterviewScheduleId { get; set; }
    public InterviewSchedule InterviewSchedule   { get; set; } = null!;

    /// <summary>
    /// Ngày + giờ đề xuất (kết hợp date + time).
    /// </summary>
    public DateTime ProposedAt { get; set; }

    /// <summary>
    /// true nếu ứng viên đã chọn slot này.
    /// Chỉ 1 slot trong list được chọn.
    /// </summary>
    public bool IsSelected { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
