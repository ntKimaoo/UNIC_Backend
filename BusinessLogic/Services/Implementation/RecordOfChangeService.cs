using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Repositories.Interface;

namespace BusinessLogic.Services.Implementation
{
    public class RecordOfChangeService : IRecordOfChangeService
    {
        private readonly IRecordOfChangeRepository _repository;

        public RecordOfChangeService(IRecordOfChangeRepository repository)
        {
            _repository = repository;
        }

        public async Task<(IEnumerable<RecordOfChangeResponseDto> Items, int TotalCount, int TotalPages)> GetPagedAsync(RecordOfChangeFilterDto filter)
        {
            var (items, totalCount) = await _repository.GetPagedAsync(
                filter.PageNumber,
                filter.PageSize,
                filter.Search,
                filter.EntityName,
                filter.ChangeType,
                filter.ClubId,
                filter.ChangedBy,
                filter.FromDate,
                filter.ToDate,
                filter.OldValueSearch,
                filter.NewValueSearch);

            var dtos = items.Select(r => new RecordOfChangeResponseDto
            {
                Id = r.Id,
                EntityName = r.EntityName,
                EntityId = r.EntityId,
                OldValue = r.OldValue,
                NewValue = r.NewValue,
                ChangedAt = r.ChangedAt,
                ChangedBy = r.ChangedBy,
                ChangedByName = r.ChangedByUser?.FullName ?? string.Empty,
                ChangeType = r.ChangeType,
                Notification = r.Notification,
                ClubId = r.ClubId,
                ClubName = r.Club?.ClubName
            });

            var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);
            return (dtos, totalCount, totalPages);
        }
    }
}
