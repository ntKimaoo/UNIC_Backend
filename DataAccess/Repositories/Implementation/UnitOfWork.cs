using DataAccess.Models;
using DataAccess.Repositories.Interface;
using System;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Implementation
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly UnicContext _context;
        private IEventRepository? _events;
        private IAttendanceRepository? _attendances;
        private IEventScheduleRepository? _eventSchedules;
        private IUserRepository? _users;

        public UnitOfWork(UnicContext context)
        {
            _context = context;
        }

        public IEventRepository Events => 
            _events ??= new EventRepository(_context);

        public IAttendanceRepository Attendances => 
            _attendances ??= new AttendanceRepository(_context);

        public IEventScheduleRepository EventSchedules => 
            _eventSchedules ??= new EventScheduleRepository(_context);

        public IUserRepository Users => 
            _users ??= new UserRepository(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
