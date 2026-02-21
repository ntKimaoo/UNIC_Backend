using System;

namespace DataAccess.Models.Meeting;

/// <summary>
/// Audit trail các sự kiện trong phòng:
/// "room.started", "participant.joined", "participant.left",
/// "recording.started", "recording.stopped", v.v.
/// </summary>
public class RoomEvent
{
    public int Id { get; set; }

    public int         MeetingRoomId { get; set; }
    public MeetingRoom MeetingRoom   { get; set; } = null!;

    /// <summary>Users.UserId của người thực hiện hành động.</summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>
    /// Loại sự kiện. Ví dụ:
    /// "room.started" | "room.ended" | "participant.joined" |
    /// "participant.left" | "recording.started" | "ice.failed"
    /// </summary>
    public string EventType { get; set; } = null!;

    /// <summary>JSON payload tuỳ chọn (ICE info, SDP, error detail…)</summary>
    public string? Payload { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
