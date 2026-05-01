using BusinessLogic.Hubs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs
{
    public class NotificationHub : Hub<INotificationClient>
    {
        private readonly ILogger<NotificationHub> _logger;
        private readonly INotificationService _notificationService;

        public NotificationHub(ILogger<NotificationHub> logger, INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task RegisterUser(Guid userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
            _logger.LogInformation("User {UserId} registered for notifications (ConnectionId: {ConnectionId})",
                userId, Context.ConnectionId);
        }

        public async Task SendNotificationToUser(Guid targetUserId, string title, string message, string type)
        {
            await _notificationService.SendNotificationAsync(targetUserId, title, message, type);
            _logger.LogInformation("Notification sent to user {TargetUserId} via Hub", targetUserId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Notification client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}