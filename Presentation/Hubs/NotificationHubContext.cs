using BusinessLogic.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs
{
    public class NotificationHubContext : INotificationHubContext
    {
        private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

        public NotificationHubContext(IHubContext<NotificationHub, INotificationClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendToUserAsync(Guid userId, object notification)
        {
            await _hubContext.Clients
                .Group(userId.ToString())
                .ReceiveNotification(notification);
        }
    }
}
