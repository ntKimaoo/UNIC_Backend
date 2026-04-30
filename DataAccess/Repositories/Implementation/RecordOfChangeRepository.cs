using DataAccess.Context;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implementation
{
    public class RecordOfChangeRepository : IRecordOfChangeRepository
    {
        private readonly UnicContext _context;

        public RecordOfChangeRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<RecordOfChange> Items, int TotalCount)> GetPagedAsync(
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
            string? newValueSearch = null)
        {
            var query = _context.RecordsOfChange
                .Include(r => r.ChangedByUser)
                .Include(r => r.Club)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(r =>
                    r.EntityName.ToLower().Contains(s) ||
                    r.Notification.ToLower().Contains(s) ||
                    (r.OldValue != null && r.OldValue.ToLower().Contains(s)) ||
                    (r.NewValue != null && r.NewValue.ToLower().Contains(s)) ||
                    r.ChangedByUser.FullName.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(oldValueSearch))
            {
                var v = oldValueSearch.ToLower();
                query = query.Where(r => r.OldValue != null && r.OldValue.ToLower().Contains(v));
            }

            if (!string.IsNullOrWhiteSpace(newValueSearch))
            {
                var v = newValueSearch.ToLower();
                query = query.Where(r => r.NewValue != null && r.NewValue.ToLower().Contains(v));
            }

            if (!string.IsNullOrWhiteSpace(entityName))
                query = query.Where(r => r.EntityName.ToLower() == entityName.ToLower());

            if (!string.IsNullOrWhiteSpace(changeType))
                query = query.Where(r => r.ChangeType.ToLower() == changeType.ToLower());

            if (clubId.HasValue)
                query = query.Where(r => r.ClubId == clubId.Value);

            if (changedBy.HasValue)
                query = query.Where(r => r.ChangedBy == changedBy.Value);

            if (fromDate.HasValue)
                query = query.Where(r => r.ChangedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(r => r.ChangedAt <= toDate.Value);

            query = query.OrderByDescending(r => r.ChangedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
