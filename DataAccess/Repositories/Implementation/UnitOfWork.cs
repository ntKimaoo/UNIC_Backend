using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore.Storage;
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
        private IUserEventRoleRepository? _eventMembers;
        private IEventRoleRepository? _eventRoles;

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

        public IUserEventRoleRepository EventMembers => 
            _eventMembers ??= new UserEventRoleRepository(_context);

        public IEventRoleRepository EventRoles => 
            _eventRoles ??= new EventRoleRepository(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
