using BusinessLogic.DTOs;
using BusinessLogic.Hubs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;

namespace BusinessLogic.Services.Implementation
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationHubContext _hubContext;

        public NotificationService(
            INotificationRepository notificationRepository,
            INotificationHubContext hubContext)
        {
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
        }

        public async Task<IEnumerable<NotificationDto>> GetByUserIdAsync(Guid userId)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId);
            return notifications.Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            });
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
            => await _notificationRepository.MarkAsReadAsync(notificationId);

        public async Task<bool> MarkAllAsReadAsync(Guid userId)
            => await _notificationRepository.MarkAllAsReadAsync(userId);

        public async Task SendNotificationAsync(Guid userId, string title, string message, string type)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            var saved = await _notificationRepository.CreateAsync(notification);

            var dto = new NotificationDto
            {
                NotificationId = saved.NotificationId,
                UserId = saved.UserId,
                Title = saved.Title,
                Message = saved.Message,
                Type = saved.Type,
                IsRead = saved.IsRead,
                CreatedAt = saved.CreatedAt
            };

            await _hubContext.SendToUserAsync(userId, dto);
        }
    }
}
