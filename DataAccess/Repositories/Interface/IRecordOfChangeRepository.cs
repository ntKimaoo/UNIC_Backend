using DataAccess.Models;

namespace DataAccess.Repositories.Interface
{
    public interface IRecordOfChangeRepository
    {
        Task<(IEnumerable<RecordOfChange> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? search = null,
            string? entityName = null,
            string? changeType = null,
            int? clubId = null,
            Guid? changedBy = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? oldValueSearch = null,
            string? newValueSearch = null);
    }
}
