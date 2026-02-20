using System;
using DataAccess.Models.Meeting.Enums;

namespace DataAccess.Models.Meeting;

/// <summary>
/// Lịch sử tham gia phòng của từng participant (Interviewer hoặc Candidate).
/// Mỗi lần join/leave tạo một dòng mới → dễ tính thời gian tham dự.
/// </summary>
public class RoomParticipant
{
    public int Id { get; set; }

    public int        MeetingRoomId { get; set; }
    public MeetingRoom MeetingRoom  { get; set; } = null!;

    // ── Người tham gia ───────────────────────────────────────────
    /// <summary>
    /// UserId (Guid as string) nếu là interviewer.
    /// Null nếu là candidate (dùng CandidateId).
    /// </summary>
    public string? UserId      { get; set; }

    /// <summary>
    /// CandidateId nếu người tham gia là ứng viên.
    /// Null nếu là interviewer.
    /// </summary>
    public int? CandidateId    { get; set; }

    /// <summary>
    /// Tên hiển thị trong phòng (snapshot tại thời điểm join).
    /// </summary>
    public string DisplayName  { get; set; } = null!;

    // ── WebRTC peer info ─────────────────────────────────────────
    /// <summary>
    /// Peer ID do signaling server cấp (unique per session).
    /// </summary>
    public string? PeerId      { get; set; }

    // ── Trạng thái kết nối ───────────────────────────────────────
    public ParticipantConnectionState ConnectionState { get; set; } = ParticipantConnectionState.Joined;

    public DateTime  JoinedAt  { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt    { get; set; }
}
