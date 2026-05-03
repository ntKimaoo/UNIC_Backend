using DataAccess.Models;
using DataAccess.Repositories.Interface;
using DataAccess.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Implementation
{
    public class EventRepository : IEventRepository
    {
        private readonly UnicContext _context;

        public EventRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<Event?> GetByIdAsync(int eventId)
        {
            return await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId);
        }

        public async Task<Event?> GetByIdWithDetailsAsync(int eventId)
        {
            return await _context.Events
                .Include(e => e.EventSchedules)
                .Include(e => e.EventImages)
                .Include(e => e.Club)
                .Include(e => e.Attendances)
                .FirstOrDefaultAsync(e => e.EventId == eventId);
        }

        public async Task<Event?> GetByIdWithAttendeesAsync(int eventId)
        {
            return await _context.Events
                .Include(e => e.Attendances)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(e => e.EventId == eventId);
        }

        public async Task<IEnumerable<Event>> GetAllAsync(int pageNumber = 1, int pageSize = 10, string? status = null, int? clubId = null)
        {
            var query = _context.Events
                .Include(e => e.Attendances)
                .Include(e => e.EventSchedules)
                .Include(e => e.Club)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(e => e.Status == status);

            if (clubId.HasValue)
                query = query.Where(e => e.ClubId == clubId.Value);

            return await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(string? status = null, int? clubId = null)
        {
            var query = _context.Events.AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(e => e.Status == status);
            if (clubId.HasValue)
                query = query.Where(e => e.ClubId == clubId.Value);
            return await query.CountAsync();
        }

        public async Task<IEnumerable<Event>> GetUpcomingEventsAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Events
                .Where(e => e.StartDate > now && e.Status != "CANCELED")
                .ToListAsync();
        }

        public async Task<int> GetAttendeeCountAsync(int eventId)
        {
            // Chỉ đếm các status thực sự chiếm slot (loại WAITLIST + REJECTED + CANCELLED)
            var occupying = new[]
            {
                nameof(AttendanceStatus.REGISTERED),
                nameof(AttendanceStatus.PENDING),
                nameof(AttendanceStatus.PRESENT),
                nameof(AttendanceStatus.CHECKED_IN),
                nameof(AttendanceStatus.ABSENT),
            };
            return await _context.Attendances
                .Where(a => a.EventId == eventId && occupying.Contains(a.AttendanceStatus))
                .CountAsync();
        }

        // Atomic: trừ 1 slot nếu còn chỗ — trả về true nếu thành công
        public async Task<bool> TryDecrementSlotAsync(int eventId)
        {
            var rows = await _context.Events
                .Where(e => e.EventId == eventId && e.AvailableSlots > 0)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.AvailableSlots, e => e.AvailableSlots - 1));
            return rows == 1;
        }

        // Atomic Direct-Promote: chuyển người WAITLIST lâu nhất thành targetStatus
        // KHÔNG nhả slot vào pool — chống Slot Stealing & Double Cancel
        public async Task<bool> TryDirectPromoteOldestWaitlistAsync(int eventId, string targetStatus)
        {
            var oldestId = await _context.Attendances
                .Where(a => a.EventId == eventId
                         && a.AttendanceStatus == nameof(AttendanceStatus.WAITLIST))
                .OrderBy(a => a.RegistrationDate)
                .Select(a => (int?)a.AttendId)
                .FirstOrDefaultAsync();

            if (oldestId == null) return false;

            // Guard condition chống Double Cancel: chỉ update nếu vẫn còn WAITLIST
            var rows = await _context.Attendances
                .Where(a => a.AttendId == oldestId.Value
                         && a.AttendanceStatus == nameof(AttendanceStatus.WAITLIST))
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(a => a.AttendanceStatus, targetStatus));

            return rows == 1;
        }

        // Cộng slot lại — chỉ gọi khi không có ai ở WAITLIST
        public async Task IncrementSlotAsync(int eventId)
        {
            await _context.Events
                .Where(e => e.EventId == eventId && e.MaxAttendees.HasValue)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.AvailableSlots, e => e.AvailableSlots + 1));
        }

        // Set AvailableSlots trực tiếp — dùng cho recalc
        public async Task SetAvailableSlotsAsync(int eventId, int value)
        {
            await _context.Events
                .Where(e => e.EventId == eventId)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.AvailableSlots, value));
        }

        public async Task AddAsync(Event @event)
        {
            await _context.Events.AddAsync(@event);
        }

        public void Update(Event @event)
        {
            _context.Events.Update(@event);
        }

        // Proactive batch status sync — called by Background Service
        public async Task<int> BulkSyncStatusAsync()
        {
            var now = DateTime.UtcNow;
            var terminalStatuses = new[] { "ENDED", "CANCELED", "COMPLETED" };
            int total = 0;

            // Rule 1: REGISTRATION_OPEN + RegEndDate passed → REGISTRATION_CLOSED
            total += await _context.Events
                .Where(e => e.Status == "REGISTRATION_OPEN"
                         && e.RegistrationEndDate.HasValue
                         && e.RegistrationEndDate.Value <= now)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.Status, "REGISTRATION_CLOSED"));

            // Rule 2a: ONGOING + EndDate passed → COMPLETED
            total += await _context.Events
                .Where(e => e.Status == "ONGOING"
                         && e.EndDate.HasValue
                         && e.EndDate.Value <= now)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.Status, "COMPLETED"));

            // Rule 2b: Non-terminal + EndDate passed → ENDED
            total += await _context.Events
                .Where(e => !terminalStatuses.Contains(e.Status)
                         && e.EndDate.HasValue
                         && e.EndDate.Value <= now)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.Status, "ENDED"));

            return total;
        }
    }
}
