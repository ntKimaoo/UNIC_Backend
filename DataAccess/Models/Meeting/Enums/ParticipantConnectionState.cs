namespace DataAccess.Models.Meeting.Enums;

public enum ParticipantConnectionState
{
    Joined       = 0,
    Reconnecting = 1,
    Left         = 2,
    Kicked       = 3
}
