using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IUnitOfWork : IDisposable
    {
        IEventRepository Events { get; }
        IAttendanceRepository Attendances { get; }
        IEventScheduleRepository EventSchedules { get; }
        IUserRepository Users { get; }
        IUserEventRoleRepository EventMembers { get; }
        IEventRoleRepository EventRoles { get; }

        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
