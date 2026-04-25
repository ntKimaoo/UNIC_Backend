using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IEventRepository
    {
        Task<Event?> GetByIdAsync(int eventId);
        Task<Event?> GetByIdWithDetailsAsync(int eventId);
        Task<Event?> GetByIdWithAttendeesAsync(int eventId);
        Task<IEnumerable<Event>> GetAllAsync(int pageNumber = 1, int pageSize = 10);
        Task<IEnumerable<Event>> GetUpcomingEventsAsync();
        Task<int> GetAttendeeCountAsync(int eventId);
        Task<bool> TryDecrementSlotAsync(int eventId);
        Task<bool> TryDirectPromoteOldestWaitlistAsync(int eventId, string targetStatus);
        Task IncrementSlotAsync(int eventId);
        Task SetAvailableSlotsAsync(int eventId, int value);
        Task AddAsync(Event @event);
        void Update(Event @event);
        Task<int> BulkSyncStatusAsync();
    }
}
