namespace DataAccess.Models.Meeting.Enums;

public enum RoomStatus
{
    Idle    = 0,   // Chưa có ai vào
    Waiting = 1,   // Có người đang chờ
    Active  = 2,   // Đang diễn ra
    Closed  = 3    // Đã kết thúc / hết hạn
}
