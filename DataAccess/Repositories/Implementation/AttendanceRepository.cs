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
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly UnicContext _context;

        public AttendanceRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<Attendance?> GetByEventAndUserAsync(int eventId, Guid userId)
        {
            // Ưu tiên record active (PENDING/REGISTERED/WAITLIST) trước REJECTED/CANCELLED
            var terminalStatuses = new[] { "REJECTED", "CANCELLED" };
            return await _context.Attendances
                .Include(a => a.User)
                .Include(a => a.Event)
                .Where(a => a.EventId == eventId && a.UserId == userId)
                .OrderBy(a => terminalStatuses.Contains(a.AttendanceStatus) ? 1 : 0)
                .ThenByDescending(a => a.RegistrationDate)
                .FirstOrDefaultAsync();
        }

        public async Task<Attendance?> GetByCheckInTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            return await _context.Attendances
                .Include(a => a.User)
                .Include(a => a.Event)
                .FirstOrDefaultAsync(a => a.CheckInToken == token);
        }

        public async Task<bool> IsUserRegisteredAsync(int eventId, Guid userId)
        {
            // Cho phép đăng ký lại nếu đã bị REJECTED hoặc CANCELLED
            var terminalStatuses = new[] { "REJECTED", "CANCELLED" };
            return await _context.Attendances
                .AnyAsync(a => a.EventId == eventId && a.UserId == userId
                    && !terminalStatuses.Contains(a.AttendanceStatus));
        }

        public async Task<IEnumerable<Attendance>> GetAttendeesByEventAsync(int eventId)
        {
            var excludedStatuses = new[] { "REJECTED", "CANCELLED" };
            return await _context.Attendances
                .Include(a => a.User)
                .Where(a => a.EventId == eventId
                    && !excludedStatuses.Contains(a.AttendanceStatus))
                .OrderBy(a => a.RegistrationDate)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Attendance> Items, int TotalCount)> GetAttendeesByEventAsync(
            int eventId, string? statusFilter, int page = 1, int pageSize = 50)
        {
            var excludedStatuses = new[] { "REJECTED", "CANCELLED" };
            var query = _context.Attendances
                .Include(a => a.User)
                .Where(a => a.EventId == eventId
                    && !excludedStatuses.Contains(a.AttendanceStatus));

            if (!string.IsNullOrEmpty(statusFilter))
                query = query.Where(a => a.AttendanceStatus == statusFilter);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(a => a.RegistrationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(Attendance attendance)
        {
            await _context.Attendances.AddAsync(attendance);
        }

        public void Update(Attendance attendance)
        {
            _context.Attendances.Update(attendance);
        }

        public async Task<Attendance?> GetOldestWaitlistedAttendeeAsync(int eventId)
        {
            return await _context.Attendances
                .Include(a => a.User)
                .Where(a => a.EventId == eventId && a.AttendanceStatus == nameof(AttendanceStatus.WAITLIST))
                .OrderBy(a => a.RegistrationDate)
                .FirstOrDefaultAsync();
        }
    }
}
