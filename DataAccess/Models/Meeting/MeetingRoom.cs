using System;
using System.Collections.Generic;
using DataAccess.Models.Meeting.Enums;

namespace DataAccess.Models.Meeting;

/// <summary>
/// Phòng WebRTC – dùng chung cho nhiều mục đích:
/// phỏng vấn, họp nội bộ, đào tạo, v.v.
/// Khi RoomType = Interview thì gắn với InterviewSchedule (optional 1-0..1).
/// </summary>
public class MeetingRoom
{
    public int Id { get; set; }

    // ── Phân loại phòng ──────────────────────────────────────────

    /// <summary>
    /// Loại phòng: Interview, Internal, Training, General
    /// </summary>
    public RoomType RoomType { get; set; } = RoomType.General;

    /// <summary>
    /// Tiêu đề phòng. Ví dụ: "Họp ban chủ nhiệm", "PV Vòng 1 – Năng lực"
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Mô tả mục đích / nội dung cuộc họp.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Users.UserId – người tạo phòng.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    // ── Lịch trình ───────────────────────────────────────────────

    /// <summary>Thời gian dự kiến bắt đầu.</summary>
    public DateTime? ScheduledStartAt { get; set; }

    /// <summary>Thời gian dự kiến kết thúc.</summary>
    public DateTime? ScheduledEndAt { get; set; }

    // ── Liên kết Interview (optional) ────────────────────────────

    /// <summary>
    /// FK tới InterviewSchedule – chỉ có giá trị khi RoomType = Interview.
    /// Nullable để room có thể tồn tại độc lập.
    /// </summary>
    public int?               InterviewScheduleId { get; set; }
    public InterviewSchedule? InterviewSchedule   { get; set; }

    // ── Định danh phòng ──────────────────────────────────────────

    /// <summary>
    /// Slug ngắn, unique – dùng làm path WebRTC: /room/{RoomCode}
    /// Ví dụ: "a3f9-xk2m". Sinh tự động khi tạo room.
    /// </summary>
    public string RoomCode { get; set; } = null!;

    // ── TURN / STUN config ───────────────────────────────────────

    /// <summary>STUN server URI. Ví dụ: "stun:stun.example.com:3478"</summary>
    public string? StunServerUri { get; set; }

    /// <summary>TURN server URI. Ví dụ: "turn:turn.example.com:3478"</summary>
    public string? TurnServerUri { get; set; }

    /// <summary>TURN username – sinh mới mỗi session (short-term credential).</summary>
    public string? TurnUsername { get; set; }

    /// <summary>TURN credential – HMAC-SHA1 hoặc static password.</summary>
    public string? TurnCredential { get; set; }

    /// <summary>Thời điểm TURN credential hết hạn.</summary>
    public DateTime? TurnCredentialExpiresAt { get; set; }

    // ── Cấu hình phòng ───────────────────────────────────────────
    public bool IsRecordingEnabled   { get; set; } = false;
    public bool IsWaitingRoomEnabled { get; set; } = true;
    public int  MaxParticipants      { get; set; } = 10;

    // ── Trạng thái phòng ─────────────────────────────────────────
    public RoomStatus Status    { get; set; } = RoomStatus.Idle;
    public DateTime?  StartedAt { get; set; }
    public DateTime?  EndedAt   { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<RoomParticipant> Participants { get; set; } = new List<RoomParticipant>();
    public ICollection<RoomEvent>       Events       { get; set; } = new List<RoomEvent>();
}
