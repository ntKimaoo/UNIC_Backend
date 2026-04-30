using BusinessLogic.DTOs;

namespace BusinessLogic.Hubs
{
    public interface IRecordOfChangeHubContext
    {
        Task BroadcastAsync(RecordOfChangeResponseDto record, int? clubId);
    }
}
