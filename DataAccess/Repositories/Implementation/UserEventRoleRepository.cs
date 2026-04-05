using DataAccess.Models;
using UNIC.DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Implementation
{
    public class UserEventRoleRepository : IUserEventRoleRepository
    {
        private readonly UnicContext _context;
        private static readonly string[] EventPolicyGroups = { "EventManagement", "AttendanceManagement" };

        public UserEventRoleRepository(UnicContext context)
        {
            _context = context;
        }

        private IQueryable<Policy> EventPoliciesQuery()
        {
            return _context.Policies.Where(p => p.PolicyGroup != null && EventPolicyGroups.Contains(p.PolicyGroup.Name));
        }

        public async Task<UserEventRole?> GetByEventAndUserAsync(int eventId, Guid userId)
        {
            return await _context.UserEventRoles
                .Include(uer => uer.EventRole)
                    .ThenInclude(er => er.EventRolePolicies)
                        .ThenInclude(erp => erp.Policy)
                .Include(uer => uer.EventMemberPolicies)
                    .ThenInclude(emp => emp.Policy)
                .FirstOrDefaultAsync(uer => uer.EventId == eventId && uer.UserId == userId);
        }

        public async Task<List<UserEventRole>> GetByEventIdAsync(int eventId)
        {
            return await _context.UserEventRoles
                .Include(uer => uer.User)
                .Include(uer => uer.EventRole)
                    .ThenInclude(er => er.EventRolePolicies)
                        .ThenInclude(erp => erp.Policy)
                .Include(uer => uer.EventMemberPolicies)
                    .ThenInclude(emp => emp.Policy)
                .Where(uer => uer.EventId == eventId)
                .ToListAsync();
        }

        public async Task<UserEventRole> AddAsync(UserEventRole userEventRole)
        {
            _context.UserEventRoles.Add(userEventRole);
            await _context.SaveChangesAsync();
            return userEventRole;
        }

        public async Task<bool> UpdateAsync(UserEventRole userEventRole)
        {
            _context.UserEventRoles.Update(userEventRole);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.UserEventRoles.FindAsync(id);
            if (entity == null) return false;
            _context.UserEventRoles.Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<UserEventRole?> GetByIdAsync(int id)
        {
            return await _context.UserEventRoles
                .Include(uer => uer.EventRole)
                    .ThenInclude(er => er.EventRolePolicies)
                        .ThenInclude(erp => erp.Policy)
                .Include(uer => uer.EventMemberPolicies)
                    .ThenInclude(emp => emp.Policy)
                .FirstOrDefaultAsync(uer => uer.EventMemberId == id);
        }

        public async Task SetMemberPoliciesAsync(int memberId, IEnumerable<string> policyNames)
        {
            var existingPolicies = await _context.EventMemberPolicies
                .Where(emp => emp.EventMemberId == memberId)
                .ToListAsync();

            _context.EventMemberPolicies.RemoveRange(existingPolicies);

            if (policyNames != null && policyNames.Any())
            {
                var policyIds = await EventPoliciesQuery()
                    .Where(p => policyNames.Contains(p.Name))
                    .Select(p => p.Id)
                    .ToListAsync();

                var newPolicies = policyIds.Select(pid => new EventMemberPolicy
                {
                    EventMemberId = memberId,
                    PolicyId = pid
                });
                await _context.EventMemberPolicies.AddRangeAsync(newPolicies);
            }

            await _context.SaveChangesAsync();
        }
    }
}

