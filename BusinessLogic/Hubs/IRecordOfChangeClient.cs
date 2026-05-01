using BusinessLogic.DTOs;

namespace BusinessLogic.Hubs
{
    public interface IRecordOfChangeClient
    {
        Task ReceiveRecordOfChange(RecordOfChangeResponseDto record);
    }
}
