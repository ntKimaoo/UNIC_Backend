using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Implementation
{
    public class EventScheduleRepository : IEventScheduleRepository
    {
        private readonly UnicContext _context;

        public EventScheduleRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task AddAsync(EventSchedule schedule)
        {
            await _context.EventSchedules.AddAsync(schedule);
        }

        public void Update(EventSchedule schedule)
        {
            _context.EventSchedules.Update(schedule);
        }

        public void Delete(EventSchedule schedule)
        {
            _context.EventSchedules.Remove(schedule);
        }

        public async Task<EventSchedule?> GetByIdAsync(int scheduleId)
        {
            return await _context.EventSchedules
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);
        }
    }
}
