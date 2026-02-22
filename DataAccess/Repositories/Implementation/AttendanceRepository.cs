using DataAccess.Models;
using DataAccess.Repositories.Interface;
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
            return await _context.Attendances
                .Include(a => a.User)
                .Include(a => a.Event)
                .FirstOrDefaultAsync(a => a.EventId == eventId && a.UserId == userId);
        }

        public async Task<bool> IsUserRegisteredAsync(int eventId, Guid userId)
        {
            return await _context.Attendances
                .AnyAsync(a => a.EventId == eventId && a.UserId == userId);
        }

        public async Task<IEnumerable<Attendance>> GetAttendeesByEventAsync(int eventId)
        {
            return await _context.Attendances
                .Include(a => a.User)
                .Where(a => a.EventId == eventId)
                .OrderBy(a => a.RegistrationDate)
                .ToListAsync();
        }

        public async Task AddAsync(Attendance attendance)
        {
            await _context.Attendances.AddAsync(attendance);
        }

        public void Update(Attendance attendance)
        {
            _context.Attendances.Update(attendance);
        }
    }
}
