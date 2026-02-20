using System;

namespace DataAccess.Models.Meeting;

/// <summary>
/// Log các sự kiện quan trọng trong phòng: join, leave, mute, record…
/// Dùng để audit trail và debug WebRTC.
/// </summary>
public class RoomEvent
{
    public int    Id          { get; set; }

    public int        MeetingRoomId { get; set; }
    public MeetingRoom MeetingRoom  { get; set; } = null!;

    /// <summary>
    /// UserId hoặc CandidateId (lưu dưới dạng string cho linh hoạt).
    /// </summary>
    public string? ActorId    { get; set; }

    /// <summary>
    /// Loại sự kiện: "participant.joined", "participant.left",
    /// "room.started", "room.ended", "recording.started", v.v.
    /// </summary>
    public string  EventType  { get; set; } = null!;

    /// <summary>
    /// JSON payload tuỳ chọn (ICE candidate, SDP info, v.v.)
    /// </summary>
    public string? Payload    { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
