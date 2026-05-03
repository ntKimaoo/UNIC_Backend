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

        public async Task UpdateEventRoleAsync(EventRole role)
        {
            _context.EventRoles.Update(role);
            await _context.SaveChangesAsync();
        }

        public async Task SetEventRolePoliciesAsync(int eventRoleId, List<int> policyIds)
        {
            // Remove existing policies
            var existing = await _context.EventRolePolicies
                .Where(erp => erp.EventRoleId == eventRoleId)
                .ToListAsync();
            _context.EventRolePolicies.RemoveRange(existing);

            // Add new policies
            var newPolicies = policyIds.Select(pid => new EventRolePolicy
            {
                EventRoleId = eventRoleId,
                PolicyId = pid
            });
            await _context.EventRolePolicies.AddRangeAsync(newPolicies);
            await _context.SaveChangesAsync();
        }

        public async Task SetEventMemberPoliciesAsync(int eventMemberId, List<int> policyIds)
        {
            // Remove existing direct policies
            var existing = await _context.EventMemberPolicies
                .Where(emp => emp.EventMemberId == eventMemberId)
                .ToListAsync();
            _context.EventMemberPolicies.RemoveRange(existing);

            // Add new policies
            var newPolicies = policyIds.Select(pid => new EventMemberPolicy
            {
                EventMemberId = eventMemberId,
                PolicyId = pid
            });
            await _context.EventMemberPolicies.AddRangeAsync(newPolicies);
            await _context.SaveChangesAsync();
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

        public async Task<List<int>> GetPolicyIdsByNamesAsync(List<string> policyNames)
        {
            var normalized = policyNames.Select(n => n.ToLower().Trim()).ToList();
            return await _context.Policies
                .Where(p => normalized.Contains(p.Name.ToLower()))
                .Select(p => p.Id)
                .ToListAsync();
        }

        // ── My Events ──

        public async Task<List<int>> GetUserParticipatingEventIdsAsync(Guid userId, string? search)
        {
            // Events where user is an attendee
            var attendeeIds = _context.Attendances
                .Where(a => a.UserId == userId)
                .Select(a => a.EventId);

            // Events where user is an event member (collaborator)
            var memberIds = _context.EventMembers
                .Where(em => em.UserId == userId)
                .Select(em => em.EventId);

            var query = attendeeIds.Union(memberIds).Distinct();

            // If search provided, join with Events to filter by name
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower().Trim();
                query = query.Where(eid =>
                    _context.Events.Any(e => e.EventId == eid &&
                        e.EventName.ToLower().Contains(searchLower)));
            }

            return await query.ToListAsync();
        }

        public async Task<List<Event>> GetEventsByIdsPagedAsync(List<int> eventIds, int page, int pageSize)
        {
            return await _context.Events
                .Include(e => e.Club)
                .Where(e => eventIds.Contains(e.EventId))
                .OrderByDescending(e => e.StartDate ?? e.CreatedAt)
                .ThenByDescending(e => e.EventId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Dictionary<int, (string Status, DateTime? CheckInTime)>> GetUserAttendancesAsync(Guid userId, List<int> eventIds)
        {
            var attendances = await _context.Attendances
                .Where(a => a.UserId == userId && eventIds.Contains(a.EventId))
                .Select(a => new { a.EventId, a.AttendanceStatus, a.CheckInTime })
                .ToListAsync();

            return attendances.ToDictionary(
                a => a.EventId,
                a => (a.AttendanceStatus, a.CheckInTime));
        }

        public async Task<Dictionary<int, EventMember>> GetUserEventMembershipsAsync(Guid userId, List<int> eventIds)
        {
            var members = await _context.EventMembers
                .Include(em => em.EventRole)
                    .ThenInclude(er => er!.EventRolePolicies)
                    .ThenInclude(erp => erp.Policy)
                .Include(em => em.EventMemberPolicies)!
                    .ThenInclude(emp => emp.Policy)
                .Where(em => em.UserId == userId && eventIds.Contains(em.EventId))
                .ToListAsync();

            return members.ToDictionary(m => m.EventId);
        }
    }
}
