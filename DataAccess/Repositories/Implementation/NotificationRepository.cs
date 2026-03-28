using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implementation
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly UnicContext _context;

        public NotificationRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Notification?> GetByIdAsync(int notificationId)
        {
            return await _context.Notifications.FindAsync(notificationId);
        }

        public async Task<Notification> CreateAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            var rows = await _context.Notifications
                .Where(n => n.NotificationId == notificationId)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
            return rows > 0;
        }

        public async Task<bool> MarkAllAsReadAsync(Guid userId)
        {
            var rows = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
            return rows >= 0;
        }

        public async Task<bool> HasRecentNotificationAsync(Guid userId, string type, TimeSpan period)
        {
            var since = DateTime.Now - period;
            return await _context.Notifications
                .AnyAsync(n => n.UserId == userId && n.Type == type && n.CreatedAt >= since);
        }
    }
}
