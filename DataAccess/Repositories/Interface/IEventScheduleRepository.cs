using DataAccess.Models;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IEventScheduleRepository
    {
        Task AddAsync(EventSchedule schedule);
        void Update(EventSchedule schedule);
    }
}
