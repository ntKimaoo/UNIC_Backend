using DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IEventRoleRepository
    {
        Task<EventRole?> GetByIdAsync(int eventRoleId, int eventId);
        Task<IEnumerable<EventRole>> GetAllAsync(int eventId);
        Task<bool> RoleNameExistsAsync(string roleName, int eventId);
        Task<EventRole> CreateAsync(EventRole eventRole);
        Task<bool> UpdateAsync(EventRole eventRole);
        Task<bool> DeleteAsync(int eventRoleId);
        Task SetPoliciesAsync(int eventRoleId, IEnumerable<string> policyNames);
        Task<List<string>> GetEventPolicyNamesAsync();
        Task<List<int>> GetEventPolicyIdsAsync();
    }
}
