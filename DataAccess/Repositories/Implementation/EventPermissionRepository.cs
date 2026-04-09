using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Implementation
{
    public class EventPermissionRepository : IEventPermissionRepository
    {
        private readonly UnicContext _context;

        public EventPermissionRepository(UnicContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Check user có policy trong event — qua EventRole hoặc direct EventMemberPolicy.
        /// Pattern copy từ PolicyRepository.HasMemberPolicyInClubAsync.
        /// </summary>
        public async Task<bool> HasEventPolicyAsync(Guid userId, int eventId, string policyTitle)
        {
            var normalizedTitle = policyTitle.ToLower().Trim();

            // Check if user is event creator (Level 0 = full quyền)
            var isCreator = await _context.EventMembers
                .AnyAsync(em => em.UserId == userId
                    && em.EventId == eventId
                    && em.EventRole != null
                    && em.EventRole.Level == 0);
            if (isCreator) return true;

            // Check policy from event role
            var hasRolePolicy = await _context.EventMembers
                .Where(em => em.UserId == userId && em.EventId == eventId)
                .SelectMany(em => em.EventRole!.EventRolePolicies)
                .AnyAsync(erp => erp.Policy.Name == normalizedTitle);
            if (hasRolePolicy) return true;

            // Check direct member policy
            var hasMemberPolicy = await _context.EventMembers
                .Where(em => em.UserId == userId && em.EventId == eventId)
                .SelectMany(em => em.EventMemberPolicies!)
                .AnyAsync(emp => emp.Policy.Name == normalizedTitle);

            return hasMemberPolicy;
        }

        public async Task<bool> IsEventCreatorAsync(Guid userId, int eventId)
        {
            return await _context.EventMembers
                .AnyAsync(em => em.UserId == userId
                    && em.EventId == eventId
                    && em.EventRole != null
                    && em.EventRole.Level == 0);
        }

        public async Task<bool> IsEventMemberAsync(Guid userId, int eventId)
        {
            return await _context.EventMembers
                .AnyAsync(em => em.UserId == userId && em.EventId == eventId);
        }

        // ── EventMember CRUD ──

        public async Task<IEnumerable<EventMember>> GetEventMembersAsync(int eventId)
        {
            return await _context.EventMembers
                .Include(em => em.User)
                .Include(em => em.EventRole)
                .Include(em => em.EventMemberPolicies)!.ThenInclude(emp => emp.Policy)
                .Where(em => em.EventId == eventId)
                .ToListAsync();
        }

        public async Task<EventMember?> GetEventMemberByIdAsync(int eventMemberId)
        {
            return await _context.EventMembers
                .Include(em => em.User)
                .Include(em => em.EventRole)
                .FirstOrDefaultAsync(em => em.EventMemberId == eventMemberId);
        }

        public async Task<EventMember?> GetEventMemberByUserAsync(int eventId, Guid userId)
        {
            return await _context.EventMembers
                .Include(em => em.EventRole)
                .FirstOrDefaultAsync(em => em.EventId == eventId && em.UserId == userId);
        }

        public async Task AddEventMemberAsync(EventMember member)
        {
            await _context.EventMembers.AddAsync(member);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveEventMemberAsync(int eventMemberId)
        {
            var member = await _context.EventMembers.FindAsync(eventMemberId);
            if (member != null)
            {
                _context.EventMembers.Remove(member);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateEventMemberRoleAsync(int eventMemberId, int? eventRoleId)
        {
            var member = await _context.EventMembers.FindAsync(eventMemberId);
            if (member != null)
            {
                member.EventRoleId = eventRoleId;
                await _context.SaveChangesAsync();
            }
        }

        // ── EventRole CRUD ──

        public async Task<IEnumerable<EventRole>> GetEventRolesAsync(int eventId)
        {
            return await _context.EventRoles
                .Include(er => er.EventRolePolicies).ThenInclude(erp => erp.Policy)
                .Where(er => er.EventId == eventId)
                .ToListAsync();
        }

        public async Task<EventRole?> GetEventRoleByIdAsync(int eventRoleId)
        {
            return await _context.EventRoles
                .Include(er => er.EventRolePolicies).ThenInclude(erp => erp.Policy)
                .FirstOrDefaultAsync(er => er.EventRoleId == eventRoleId);
        }

        public async Task AddEventRoleAsync(EventRole role)
        {
            await _context.EventRoles.AddAsync(role);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveEventRoleAsync(int eventRoleId)
        {
            var role = await _context.EventRoles.FindAsync(eventRoleId);
            if (role != null)
            {
                _context.EventRoles.Remove(role);
                await _context.SaveChangesAsync();
            }
        }

        // ── User permissions ──

        public async Task<IEnumerable<string>> GetUserEventPoliciesAsync(Guid userId, int eventId)
        {
            // Policies from event role
            var rolePolicies = await _context.EventMembers
                .Where(em => em.UserId == userId && em.EventId == eventId && em.EventRole != null)
                .SelectMany(em => em.EventRole!.EventRolePolicies)
                .Select(erp => erp.Policy.Name)
                .ToListAsync();

            // Direct member policies
            var memberPolicies = await _context.EventMembers
                .Where(em => em.UserId == userId && em.EventId == eventId)
                .SelectMany(em => em.EventMemberPolicies!)
                .Select(emp => emp.Policy.Name)
                .ToListAsync();

            return rolePolicies.Concat(memberPolicies).Distinct().ToList();
        }
    }
}
