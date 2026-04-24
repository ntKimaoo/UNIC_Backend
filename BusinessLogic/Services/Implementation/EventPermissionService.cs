using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using UNIC.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementation
{
    public class EventPermissionService : IEventPermissionService
    {
        private readonly IEventPermissionRepository _eventPermRepo;
        private readonly IEventRepository _eventRepo;

        public EventPermissionService(
            IEventPermissionRepository eventPermRepo,
            IEventRepository eventRepo)
        {
            _eventPermRepo = eventPermRepo;
            _eventRepo = eventRepo;
        }

        /// <summary>
        /// Check user có policy trong event.
        /// Club Manager check nằm ở EventPolicyHandler, không ở đây.
        /// </summary>
        public async Task<bool> HasEventPolicyAsync(Guid userId, int eventId, string policyTitle)
        {
            return await _eventPermRepo.HasEventPolicyAsync(userId, eventId, policyTitle);
        }

        public async Task<EventPermissionSummaryDto> GetUserEventPermissionsAsync(Guid userId, int eventId)
        {
            var isCreator = await _eventPermRepo.IsEventCreatorAsync(userId, eventId);
            var isMember = await _eventPermRepo.IsEventMemberAsync(userId, eventId);
            var policies = isMember
                ? (await _eventPermRepo.GetUserEventPoliciesAsync(userId, eventId)).ToList()
                : new List<string>();

            var member = await _eventPermRepo.GetEventMemberByUserAsync(eventId, userId);

            return new EventPermissionSummaryDto
            {
                IsCreator = isCreator,
                IsEventMember = isMember,
                IsClubManager = false, // Set by controller using its own IsClubManager() logic
                RoleName = member?.EventRole?.RoleName,
                Policies = isCreator
                    ? new List<string> { "*" } // creator = full quyền
                    : policies
            };
        }

        // ── EventMember CRUD ──

        public async Task<IEnumerable<EventMemberDto>> GetEventMembersAsync(int eventId)
        {
            var members = await _eventPermRepo.GetEventMembersAsync(eventId);
            return members.Select(MapToDto);
        }

        public async Task<EventMemberDto> AddEventMemberAsync(int eventId, AddEventMemberRequest request, Guid assignedBy)
        {
            // Kiểm tra thành viên đã là cộng tác viên chưa
            var existing = await _eventPermRepo.GetEventMemberByUserAsync(eventId, request.UserId);
            if (existing != null)
                throw new InvalidOperationException("Thành viên đã là cộng tác viên của sự kiện.");

            var member = new EventMember
            {
                EventId = eventId,
                UserId = request.UserId,
                EventRoleId = request.EventRoleId,
                AssignedBy = assignedBy,
                AssignedAt = DateTime.Now
            };

            await _eventPermRepo.AddEventMemberAsync(member);

            var saved = await _eventPermRepo.GetEventMemberByUserAsync(eventId, request.UserId);
            return MapToDto(saved!);
        }

        public async Task RemoveEventMemberAsync(int eventMemberId)
        {
            // Guard: cannot remove Creator
            var member = await _eventPermRepo.GetEventMemberByIdAsync(eventMemberId)
                ?? throw new InvalidOperationException("Event member not found.");
            if (member.EventRole != null && member.EventRole.Level == 0)
                throw new InvalidOperationException("Cannot remove the Creator.");

            await _eventPermRepo.RemoveEventMemberAsync(eventMemberId);
        }

        public async Task UpdateEventMemberRoleAsync(int eventMemberId, int? eventRoleId)
        {
            // Guard: cannot change Creator's role
            var member = await _eventPermRepo.GetEventMemberByIdAsync(eventMemberId)
                ?? throw new InvalidOperationException("Event member not found.");
            if (member.EventRole != null && member.EventRole.Level == 0)
                throw new InvalidOperationException("Cannot change the Creator's role.");

            await _eventPermRepo.UpdateEventMemberRoleAsync(eventMemberId, eventRoleId);
        }

        // ── EventRole CRUD ──

        public async Task<IEnumerable<EventRoleDto>> GetEventRolesAsync(int eventId)
        {
            var roles = await _eventPermRepo.GetEventRolesAsync(eventId);
            return roles.Select(r => new EventRoleDto
            {
                EventRoleId = r.EventRoleId,
                EventId = r.EventId,
                RoleName = r.RoleName,
                Description = r.Description,
                Level = r.Level,
                Policies = r.EventRolePolicies?.Select(p => p.Policy.Name).ToList() ?? new()
            });
        }

        public async Task<EventRoleDto> CreateEventRoleAsync(int eventId, CreateEventRoleRequest request)
        {
            // Guard: duplicate role name
            var existingRoles = await _eventPermRepo.GetEventRolesAsync(eventId);
            if (existingRoles.Any(r => r.RoleName.Equals(request.RoleName, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Role name already exists in this event.");

            var role = new EventRole
            {
                EventId = eventId,
                RoleName = request.RoleName,
                Description = request.Description,
                Level = request.Level
            };

            if (request.PolicyIds?.Any() == true)
            {
                role.EventRolePolicies = request.PolicyIds.Select(pid => new EventRolePolicy
                {
                    PolicyId = pid
                }).ToList();
            }

            await _eventPermRepo.AddEventRoleAsync(role);

            var saved = await _eventPermRepo.GetEventRoleByIdAsync(role.EventRoleId);
            return new EventRoleDto
            {
                EventRoleId = saved!.EventRoleId,
                EventId = saved.EventId,
                RoleName = saved.RoleName,
                Description = saved.Description,
                Level = saved.Level,
                Policies = saved.EventRolePolicies?.Select(p => p.Policy.Name).ToList() ?? new()
            };
        }

        public async Task RemoveEventRoleAsync(int eventRoleId)
        {
            // Guard: cannot delete Creator role
            var role = await _eventPermRepo.GetEventRoleByIdAsync(eventRoleId)
                ?? throw new Exception("Event role not found.");
            if (role.Level == 0)
                throw new InvalidOperationException("Cannot delete the Creator role.");

            await _eventPermRepo.RemoveEventRoleAsync(eventRoleId);
        }

        public async Task<EventRoleDto> UpdateEventRoleAsync(int eventRoleId, string roleName, string? description)
        {
            var role = await _eventPermRepo.GetEventRoleByIdAsync(eventRoleId)
                ?? throw new Exception("Event role not found.");

            // Guard: cannot modify Creator role
            if (role.Level == 0)
                throw new InvalidOperationException("Cannot modify the Creator role.");

            role.RoleName = roleName;
            role.Description = description;
            await _eventPermRepo.UpdateEventRoleAsync(role);

            var saved = await _eventPermRepo.GetEventRoleByIdAsync(eventRoleId);
            return new EventRoleDto
            {
                EventRoleId = saved!.EventRoleId,
                EventId = saved.EventId,
                RoleName = saved.RoleName,
                Description = saved.Description,
                Level = saved.Level,
                Policies = saved.EventRolePolicies?.Select(p => p.Policy.Name).ToList() ?? new()
            };
        }

        public async Task SetEventRolePoliciesAsync(int eventRoleId, List<string> policyNames)
        {
            // Resolve policy names to IDs
            var policyIds = await _eventPermRepo.GetPolicyIdsByNamesAsync(policyNames);
            await _eventPermRepo.SetEventRolePoliciesAsync(eventRoleId, policyIds);
        }

        // ── Auto-create Creator ──

        public async Task CreateCreatorRoleAndAssignAsync(int eventId, Guid creatorUserId)
        {
            var creatorRole = new EventRole
            {
                EventId = eventId,
                RoleName = "Creator",
                Description = "Người tạo sự kiện — toàn quyền",
                Level = 0
            };
            await _eventPermRepo.AddEventRoleAsync(creatorRole);

            var member = new EventMember
            {
                EventId = eventId,
                UserId = creatorUserId,
                EventRoleId = creatorRole.EventRoleId,
                AssignedAt = DateTime.Now
            };
            await _eventPermRepo.AddEventMemberAsync(member);
        }

        // ── My Events ──

        public async Task<MyEventsPagedResult> GetMyEventsAsync(Guid userId, string? search, int page, int pageSize)
        {
            // 1. Get all event IDs where user participates (attendee or collaborator)
            var allEventIds = await _eventPermRepo.GetUserParticipatingEventIdsAsync(userId, search);
            var total = allEventIds.Count;

            // 2. Get paged events
            var events = await _eventPermRepo.GetEventsByIdsPagedAsync(allEventIds, page, pageSize);
            var pagedEventIds = events.Select(e => e.EventId).ToList();

            // 3. Batch load participation data
            var attendances = await _eventPermRepo.GetUserAttendancesAsync(userId, pagedEventIds);
            var memberships = await _eventPermRepo.GetUserEventMembershipsAsync(userId, pagedEventIds);

            // 4. Map to DTOs
            var items = events.Select(e =>
            {
                var isAttendee = attendances.ContainsKey(e.EventId);
                var isCollaborator = memberships.ContainsKey(e.EventId);

                // Compute effective policies for collaborator
                var policies = new List<string>();
                string? roleName = null;
                if (isCollaborator)
                {
                    var member = memberships[e.EventId];
                    roleName = member.EventRole?.RoleName;

                    // Creator (Level 0) = full access
                    if (member.EventRole?.Level == 0)
                    {
                        policies.Add("*");
                    }
                    else
                    {
                        // Role policies
                        if (member.EventRole?.EventRolePolicies != null)
                            policies.AddRange(member.EventRole.EventRolePolicies.Select(p => p.Policy.Name));
                        // Direct member policies
                        if (member.EventMemberPolicies != null)
                            policies.AddRange(member.EventMemberPolicies.Select(p => p.Policy.Name));
                        policies = policies.Distinct().ToList();
                    }
                }

                return new MyEventItemDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    ImageUrl = e.ImageUrl,
                    Location = e.Location,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    Status = e.Status,
                    ClubName = e.Club?.ClubName,
                    ClubId = e.ClubId,
                    IsAttendee = isAttendee,
                    AttendanceStatus = isAttendee ? attendances[e.EventId].Status : null,
                    IsCollaborator = isCollaborator,
                    RoleName = roleName,
                    Policies = policies
                };
            }).ToList();

            return new MyEventsPagedResult
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        // ── Helpers ──

        private static EventMemberDto MapToDto(EventMember em) => new()
        {
            EventMemberId = em.EventMemberId,
            EventId = em.EventId,
            UserId = em.UserId,
            UserName = em.User?.FullName ?? "",
            UserAvatar = em.User?.Avatar,
            UserEmail = em.User?.Email,
            EventRoleId = em.EventRoleId,
            RoleName = em.EventRole?.RoleName,
            RoleLevel = em.EventRole?.Level,
            AssignedAt = em.AssignedAt,
            DirectPolicies = em.EventMemberPolicies?.Select(p => p.Policy.Name).ToList() ?? new()
        };
    }
}
