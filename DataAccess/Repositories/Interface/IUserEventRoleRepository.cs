using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IUserEventRoleRepository
    {
        Task<UserEventRole?> GetByEventAndUserAsync(int eventId, Guid userId);
        Task<List<UserEventRole>> GetByEventIdAsync(int eventId);
        Task<UserEventRole> AddAsync(UserEventRole userEventRole);
        Task<bool> UpdateAsync(UserEventRole userEventRole);
        Task<bool> DeleteAsync(int id);
        Task<UserEventRole?> GetByIdAsync(int id);
        Task SetMemberPoliciesAsync(int memberId, IEnumerable<string> policyNames);
    }
}
