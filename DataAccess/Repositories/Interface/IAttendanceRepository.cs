using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IAttendanceRepository
    {
        Task<Attendance?> GetByEventAndUserAsync(int eventId, Guid userId);
        Task<Attendance?> GetByCheckInTokenAsync(string token);
        Task<bool> IsUserRegisteredAsync(int eventId, Guid userId);
        Task<IEnumerable<Attendance>> GetAttendeesByEventAsync(int eventId);

        // New: server-side filter + pagination
        Task<(IEnumerable<Attendance> Items, int TotalCount)> GetAttendeesByEventAsync(
            int eventId, string? statusFilter, int page = 1, int pageSize = 50);

        Task AddAsync(Attendance attendance);
        void Update(Attendance attendance);
        Task<Attendance?> GetOldestWaitlistedAttendeeAsync(int eventId);
    }
}
