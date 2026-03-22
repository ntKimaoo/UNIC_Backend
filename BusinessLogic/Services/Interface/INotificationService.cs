using BusinessLogic.DTOs;

namespace BusinessLogic.Services.Interface
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetByUserIdAsync(Guid userId);
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> MarkAllAsReadAsync(Guid userId);
        Task SendNotificationAsync(Guid userId, string title, string message, string type);
    }
}
