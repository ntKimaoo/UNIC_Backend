namespace BusinessLogic.DTOs
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class BulkNotificationDto
    {
        public List<Guid> UserIds { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
    }
}
