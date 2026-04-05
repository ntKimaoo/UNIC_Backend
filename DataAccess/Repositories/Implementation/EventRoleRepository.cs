using DataAccess.Models;
using UNIC.DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Implementation
{
    public class EventRoleRepository : IEventRoleRepository
    {
        private readonly UnicContext _context;
        private static readonly string[] EventPolicyGroups = { "EventManagement", "AttendanceManagement" };

        public EventRoleRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<EventRole?> GetByIdAsync(int eventRoleId, int eventId)
        {
            return await _context.EventRoles
                .Include(er => er.EventRolePolicies)
                .ThenInclude(erp => erp.Policy)
                .FirstOrDefaultAsync(er => er.EventRoleId == eventRoleId && er.EventId == eventId);
        }

        private IQueryable<Policy> EventPoliciesQuery()
        {
            return _context.Policies.Where(p => p.PolicyGroup != null && EventPolicyGroups.Contains(p.PolicyGroup.Name));
        }

        public async Task<List<string>> GetEventPolicyNamesAsync()
        {
            return await EventPoliciesQuery()
                .Select(p => p.Name)
                .ToListAsync();
        }

        public async Task<List<int>> GetEventPolicyIdsAsync()
        {
            return await EventPoliciesQuery()
                .Select(p => p.Id)
                .ToListAsync();
        }

        public async Task<IEnumerable<EventRole>> GetAllAsync(int eventId)
        {
            return await _context.EventRoles
                .Include(er => er.EventRolePolicies)
                .ThenInclude(erp => erp.Policy)
                .Where(er => er.EventId == eventId)
                .ToListAsync();
        }

        public async Task<bool> RoleNameExistsAsync(string roleName, int eventId)
        {
            return await _context.EventRoles
                .AnyAsync(er => er.RoleName == roleName && er.EventId == eventId);
        }

        public async Task<EventRole> CreateAsync(EventRole eventRole)
        {
            _context.EventRoles.Add(eventRole);
            await _context.SaveChangesAsync();
            return eventRole;
        }

        public async Task<bool> UpdateAsync(EventRole eventRole)
        {
            _context.EventRoles.Update(eventRole);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int eventRoleId)
        {
            var eventRole = await _context.EventRoles.FindAsync(eventRoleId);
            if (eventRole == null) return false;

            _context.EventRoles.Remove(eventRole);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task SetPoliciesAsync(int eventRoleId, IEnumerable<string> policyNames)
        {
            var existingPolicies = await _context.EventRolePolicies
                .Where(erp => erp.EventRoleId == eventRoleId)
                .ToListAsync();

            _context.EventRolePolicies.RemoveRange(existingPolicies);

            if (policyNames != null && policyNames.Any())
            {
                var policyIds = await EventPoliciesQuery()
                    .Where(p => policyNames.Contains(p.Name))
                    .Select(p => p.Id)
                    .ToListAsync();

                var newPolicies = policyIds.Select(pid => new EventRolePolicy
                {
                    EventRoleId = eventRoleId,
                    PolicyId = pid
                });
                await _context.EventRolePolicies.AddRangeAsync(newPolicies);
            }

            await _context.SaveChangesAsync();
        }
    }
}

