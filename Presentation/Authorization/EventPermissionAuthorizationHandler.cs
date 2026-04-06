using DataAccess.Repositories.Interface;
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
    /// Handles EventPermissionRequirement by checking:
    ///   1. System Admin → always allowed
    ///   2. Club Manager (from club_roles claim) → always allowed
    ///   3. Event Member with matching policy → allowed
    ///   4. Otherwise → denied
    /// 
    /// Route parameters expected: {clubId} and {id} (eventId).
    /// </summary>
    public class EventPermissionAuthorizationHandler : AuthorizationHandler<EventPermissionRequirement>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EventPermissionAuthorizationHandler(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
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
            EventPermissionRequirement requirement)
        {
            // 1. System Admin → always pass
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                context.Fail();
                return;
            }

            // Parse route parameters
            var routeValues = httpContext.Request.RouteValues;

            if (!routeValues.TryGetValue("clubId", out var clubIdObj)
                || !int.TryParse(clubIdObj?.ToString(), out var clubId))
            {
                context.Fail();
                return;
            }

            // 2. Club Manager → always pass
            if (IsClubManager(context.User, clubId))
            {
                context.Succeed(requirement);
                return;
            }

            // Need eventId for event-level check
            if (!routeValues.TryGetValue("id", out var eventIdObj)
                || !int.TryParse(eventIdObj?.ToString(), out var eventId))
            {
                context.Fail();
                return;
            }

            // Need userId
            var userIdClaim = context.User.FindFirst("UserId")
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirst("sub")
                ?? context.User.FindFirst("userId");

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                context.Fail();
                return;
            }

            // 3. Event Member with matching policy
            var member = await _unitOfWork.EventMembers.GetByEventAndUserAsync(eventId, userId);
            if (member == null)
            {
                context.Fail();
                return;
            }

            // If no specific policy required, just being a member is enough
            if (string.IsNullOrEmpty(requirement.PolicyTitle))
            {
                context.Succeed(requirement);
                return;
            }

            // Collect policies from role + member-specific overrides
            var rolePolicies = member.EventRole?.EventRolePolicies?
                .Select(p => p.Policy?.Name)
                .Where(p => p != null) ?? Enumerable.Empty<string>();

            var memberPolicies = member.EventMemberPolicies?
                .Select(p => p.Policy?.Name)
                .Where(p => p != null) ?? Enumerable.Empty<string>();

            var allPolicies = rolePolicies.Union(memberPolicies).ToList();

            if (allPolicies.Contains(requirement.PolicyTitle, StringComparer.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }

        private static bool IsClubManager(ClaimsPrincipal user, int clubId)
        {
            var clubRolesClaim = user.FindFirst("club_roles")?.Value;
            if (string.IsNullOrEmpty(clubRolesClaim)) return false;

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var roles = JsonSerializer.Deserialize<List<ClubRoleClaimDto>>(clubRolesClaim, options);
                return roles != null && roles.Any(r =>
                    r.ClubId == clubId &&
                    (r.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
                     r.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase)));
            }
            catch
            {
                return false;
            }
        }
    }
}
