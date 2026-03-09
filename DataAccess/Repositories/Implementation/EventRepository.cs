using DataAccess.Models;
using DataAccess.Repositories.Interface;
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
                .FirstOrDefaultAsync(e => e.EventId == eventId);
        }

        public async Task<Event?> GetByIdWithAttendeesAsync(int eventId)
        {
            return await _context.Events
                .Include(e => e.Attendances)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(e => e.EventId == eventId);
        }

        public async Task<IEnumerable<Event>> GetAllAsync(int pageNumber = 1, int pageSize = 10)
        {
            return await _context.Events
                .OrderByDescending(e => e.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetAttendeeCountAsync(int eventId)
        {
            return await _context.Attendances
                .Where(a => a.EventId == eventId && a.AttendanceStatus != "CANCELLED")
                .CountAsync();
        }

        public async Task AddAsync(Event @event)
        {
            await _context.Events.AddAsync(@event);
        }

        public void Update(Event @event)
        {
            _context.Events.Update(@event);
        }
    }
}
