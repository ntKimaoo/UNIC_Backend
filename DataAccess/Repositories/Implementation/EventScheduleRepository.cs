using DataAccess.Models;
using DataAccess.Repositories.Interface;
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
    }
}
