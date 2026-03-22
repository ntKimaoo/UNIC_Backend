using DataAccess.Models;

namespace DataAccess.Repositories.Interface
{
    public interface INotificationRepository
    {
        Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId);
        Task<Notification?> GetByIdAsync(int notificationId);
        Task<Notification> CreateAsync(Notification notification);
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> MarkAllAsReadAsync(Guid userId);
        Task<bool> HasRecentNotificationAsync(Guid userId, string type, TimeSpan period);
    }
}
