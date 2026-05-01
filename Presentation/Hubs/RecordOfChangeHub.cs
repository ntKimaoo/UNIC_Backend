using BusinessLogic.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs
{
    public class RecordOfChangeHub : Hub<IRecordOfChangeClient>
    {
        private readonly ILogger<RecordOfChangeHub> _logger;

        public RecordOfChangeHub(ILogger<RecordOfChangeHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinClubGroup(int clubId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"club_{clubId}");
            _logger.LogInformation("Client joined club_{ClubId} group: {ConnectionId}", clubId, Context.ConnectionId);
        }

        public async Task LeaveClubGroup(int clubId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"club_{clubId}");
        }

        public async Task JoinAllGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "all");
            _logger.LogInformation("Client joined all-records group: {ConnectionId}", Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("RecordOfChange client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
