using System;
using DataAccess.Models.Meeting.Enums;

namespace DataAccess.Models.Meeting;

/// <summary>
/// Lịch sử tham gia phòng của từng participant.
/// Mỗi lần join tạo một dòng mới → tính được thời gian tham dự.
/// 
/// Cả interviewer lẫn candidate đều dùng UserId (Guid) vì
/// cả hai đều là Users trong DB kia.
/// </summary>
public class RoomParticipant
{
    public int Id { get; set; }

    public int         MeetingRoomId { get; set; }
    public MeetingRoom MeetingRoom   { get; set; } = null!;

    /// <summary>
    /// Users.UserId – áp dụng cho cả interviewer lẫn candidate.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Tên hiển thị trong phòng (snapshot tại thời điểm join,
    /// tránh phải join sang DB kia khi xem log).
    /// </summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Vai trò trong phòng: "Interviewer" | "Candidate" | "Observer"
    /// Snapshot từ InterviewAssignment.Role hoặc tự suy.
    /// </summary>
    public string Role { get; set; } = "Candidate";

    // ── WebRTC peer info ─────────────────────────────────────────

    /// <summary>Peer ID do signaling server cấp, unique per session.</summary>
    public string? PeerId { get; set; }

    // ── Trạng thái kết nối ───────────────────────────────────────
    public ParticipantConnectionState ConnectionState { get; set; } = ParticipantConnectionState.Joined;

    public DateTime  JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt   { get; set; }
}
