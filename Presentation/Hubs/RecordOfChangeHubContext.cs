using BusinessLogic.DTOs;
using BusinessLogic.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs
{
    public class RecordOfChangeHubContext : IRecordOfChangeHubContext
    {
        private readonly IHubContext<RecordOfChangeHub, IRecordOfChangeClient> _hubContext;

        public RecordOfChangeHubContext(IHubContext<RecordOfChangeHub, IRecordOfChangeClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastAsync(RecordOfChangeResponseDto record, int? clubId)
        {
            if (clubId.HasValue)
            {
                await _hubContext.Clients
                    .Group($"club_{clubId.Value}")
                    .ReceiveRecordOfChange(record);
            }

            await _hubContext.Clients
                .Group("all")
                .ReceiveRecordOfChange(record);
        }
    }
}
