using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Presentation.Authorization
{
    /// <summary>
    /// Handles EventPolicyRequirement.
    /// Authorization flow:
    ///   1. Admin hệ thống → pass
    ///   2. Club Manager (from route {clubId}, JWT club_roles) → pass
    ///   3. Event permission check (Creator/Role/DirectPolicy) → pass
    ///   4. Fail
    ///
    /// Tận dụng logic IsClubManager từ ClubEventsController.
    /// </summary>
    public class EventPolicyHandler : AuthorizationHandler<EventPolicyRequirement>
    {
        private readonly IEventPermissionService _eventPermService;
        private readonly IPolicyService _policyService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EventPolicyHandler(
            IEventPermissionService eventPermService,
            IPolicyService policyService,
            IHttpContextAccessor httpContextAccessor)
        {
            _eventPermService = eventPermService;
            _policyService = policyService;
            _httpContextAccessor = httpContextAccessor;
        }

        private class ClubRoleClaimDto
        {
            public int ClubId { get; set; }
            public string RoleName { get; set; } = string.Empty;
            public int Level { get; set; }
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            EventPolicyRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) { context.Fail(); return; }

            // Resolve userId
            var userIdClaim = context.User.FindFirst("UserId")
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirst("sub");

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                context.Fail();
                return;
            }

            // ── 1. Admin hệ thống → pass ──
            var userRoles = await _policyService.GetUserRole(userId);
            if (userRoles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
            {
                context.Succeed(requirement);
                return;
            }

            // Resolve clubId from route
            var routeValues = httpContext.Request.RouteValues;
            int? clubId = null;
            if (routeValues.TryGetValue("clubId", out var clubIdObj)
                && int.TryParse(clubIdObj?.ToString(), out var cid))
            {
                clubId = cid;
            }

            // ── 2. Club Manager → pass ──
            if (clubId.HasValue)
            {
                // Check if club member has ANY of the required policies
                foreach (var pt in requirement.PolicyTitles)
                {
                    var hasClubPolicy = await _policyService.HasMemberPolicyInClubAsync(userId, clubId.Value, pt);
                    if (hasClubPolicy)
                    {
                        context.Succeed(requirement);
                        return;
                    }
                }

                // Also check IsClubManager from JWT claims (Level 0)
                var clubRolesClaim = context.User.FindFirst("club_roles")?.Value;
                if (!string.IsNullOrEmpty(clubRolesClaim))
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var roles = JsonSerializer.Deserialize<List<ClubRoleClaimDto>>(clubRolesClaim, options);
                        if (roles != null && roles.Any(r => r.ClubId == clubId.Value
                            && (r.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase)
                                || r.Level == 0)))
                        {
                            context.Succeed(requirement);
                            return;
                        }
                    }
                    catch { /* ignore parse errors */ }
                }
            }

            // ── 3. Event permission check ──
            // Try to resolve eventId from route (could be {eventId} or {id})
            int? eventId = null;
            if (routeValues.TryGetValue("eventId", out var eventIdObj)
                && int.TryParse(eventIdObj?.ToString(), out var eid))
            {
                eventId = eid;
            }
            else if (routeValues.TryGetValue("id", out var idObj)
                && int.TryParse(idObj?.ToString(), out var id))
            {
                eventId = id;
            }

            if (!eventId.HasValue)
            {
                context.Fail();
                return;
            }

            // Check if user has ANY of the required event policies
            foreach (var pt in requirement.PolicyTitles)
            {
                var hasEventPolicy = await _eventPermService.HasEventPolicyAsync(
                    userId, eventId.Value, pt);
                if (hasEventPolicy)
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            context.Fail();
        }
    }
}
