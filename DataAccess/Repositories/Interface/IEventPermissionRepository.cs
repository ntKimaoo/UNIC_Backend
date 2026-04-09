using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IEventPermissionRepository
    {
        /// <summary>Check user có policy trong event không (qua role hoặc direct).</summary>
        Task<bool> HasEventPolicyAsync(Guid userId, int eventId, string policyTitle);

        /// <summary>Check user có phải event creator (Level 0) không.</summary>
        Task<bool> IsEventCreatorAsync(Guid userId, int eventId);

        /// <summary>Check user có phải event member không.</summary>
        Task<bool> IsEventMemberAsync(Guid userId, int eventId);

        // EventMember CRUD
        Task<IEnumerable<EventMember>> GetEventMembersAsync(int eventId);
        Task<EventMember?> GetEventMemberByIdAsync(int eventMemberId);
        Task<EventMember?> GetEventMemberByUserAsync(int eventId, Guid userId);
        Task AddEventMemberAsync(EventMember member);
        Task RemoveEventMemberAsync(int eventMemberId);
        Task UpdateEventMemberRoleAsync(int eventMemberId, int? eventRoleId);

        // EventRole CRUD
        Task<IEnumerable<EventRole>> GetEventRolesAsync(int eventId);
        Task<EventRole?> GetEventRoleByIdAsync(int eventRoleId);
        Task AddEventRoleAsync(EventRole role);
        Task RemoveEventRoleAsync(int eventRoleId);

        // User permissions list
        Task<IEnumerable<string>> GetUserEventPoliciesAsync(Guid userId, int eventId);
    }
}
