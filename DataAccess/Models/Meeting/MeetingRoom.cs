using System;
using System.Collections.Generic;
using DataAccess.Models.Meeting.Enums;

namespace DataAccess.Models.Meeting;

/// <summary>
/// Phòng họp WebRTC được tạo tự động khi schedule được confirm.
/// Lưu tất cả thông tin cần thiết để signaling server kết nối peer.
/// </summary>
public class MeetingRoom
{
    public int    Id    { get; set; }

    // ── Liên kết 1-1 với InterviewSchedule ──────────────────────
    public int               InterviewScheduleId { get; set; }
    public InterviewSchedule InterviewSchedule   { get; set; } = null!;

    // ── Định danh phòng ──────────────────────────────────────────
    /// <summary>
    /// Room slug ngắn, dùng làm path WebRTC: /room/{RoomCode}
    /// Sinh tự động, unique, ví dụ: "a3f9-xk2m"
    /// </summary>
    public string RoomCode { get; set; } = null!;

    // ── TURN / STUN config ───────────────────────────────────────
    /// <summary>
    /// STUN server URI, ví dụ: "stun:stun.example.com:3478"
    /// Có thể để null để dùng default của client.
    /// </summary>
    public string? StunServerUri  { get; set; }

    /// <summary>
    /// TURN server URI, ví dụ: "turn:turn.example.com:3478"
    /// </summary>
    public string? TurnServerUri  { get; set; }

    /// <summary>
    /// TURN username (short-term credential, sinh mới mỗi session).
    /// </summary>
    public string? TurnUsername   { get; set; }

    /// <summary>
    /// TURN credential (HMAC-SHA1 hoặc static password).
    /// </summary>
    public string? TurnCredential { get; set; }

    /// <summary>
    /// Thời điểm TURN credential hết hạn (dùng với time-limited credential).
    /// </summary>
    public DateTime? TurnCredentialExpiresAt { get; set; }

    // ── Cấu hình phòng ───────────────────────────────────────────
    public bool IsRecordingEnabled  { get; set; } = false;
    public bool IsWaitingRoomEnabled { get; set; } = true;
    public int  MaxParticipants     { get; set; } = 10;

    // ── Trạng thái phòng ─────────────────────────────────────────
    public RoomStatus Status    { get; set; } = RoomStatus.Idle;
    public DateTime?  StartedAt { get; set; }   // Khi người đầu tiên join
    public DateTime?  EndedAt   { get; set; }   // Khi room đóng

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<RoomParticipant> Participants { get; set; } = new List<RoomParticipant>();
    public ICollection<RoomEvent>       Events       { get; set; } = new List<RoomEvent>();
}
