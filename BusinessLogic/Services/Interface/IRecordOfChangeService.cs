using BusinessLogic.DTOs;

namespace BusinessLogic.Services.Interface
{
    public interface IRecordOfChangeService
    {
        Task<(IEnumerable<RecordOfChangeResponseDto> Items, int TotalCount, int TotalPages)> GetPagedAsync(RecordOfChangeFilterDto filter);
    }
}
