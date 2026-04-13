using BusinessLogic.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IEventPermissionService
    {
        /// <summary>Check user có policy trong event (bao gồm Club Manager auto pass).</summary>
        Task<bool> HasEventPolicyAsync(Guid userId, int eventId, string policyTitle);

        /// <summary>Lấy tổng hợp quyền của user trên event.</summary>
        Task<EventPermissionSummaryDto> GetUserEventPermissionsAsync(Guid userId, int eventId);

        // EventMember management
        Task<IEnumerable<EventMemberDto>> GetEventMembersAsync(int eventId);
        Task<EventMemberDto> AddEventMemberAsync(int eventId, AddEventMemberRequest request, Guid assignedBy);
        Task RemoveEventMemberAsync(int eventMemberId);
        Task UpdateEventMemberRoleAsync(int eventMemberId, int? eventRoleId);

        // EventRole management
        Task<IEnumerable<EventRoleDto>> GetEventRolesAsync(int eventId);
        Task<EventRoleDto> CreateEventRoleAsync(int eventId, CreateEventRoleRequest request);
        Task RemoveEventRoleAsync(int eventRoleId);
        Task<EventRoleDto> UpdateEventRoleAsync(int eventRoleId, string roleName, string? description);
        Task SetEventRolePoliciesAsync(int eventRoleId, List<string> policyNames);

        // Auto-create creator on event creation
        Task CreateCreatorRoleAndAssignAsync(int eventId, Guid creatorUserId);

        // My Events
        Task<MyEventsPagedResult> GetMyEventsAsync(Guid userId, string? search, int page, int pageSize);
    }
}
