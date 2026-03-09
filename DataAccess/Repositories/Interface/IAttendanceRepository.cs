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
        Task AddAsync(Attendance attendance);
        void Update(Attendance attendance);
    }
}
