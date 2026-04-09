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
            await _eventPermRepo.RemoveEventMemberAsync(eventMemberId);
        }

        public async Task UpdateEventMemberRoleAsync(int eventMemberId, int? eventRoleId)
        {
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
            await _eventPermRepo.RemoveEventRoleAsync(eventRoleId);
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
