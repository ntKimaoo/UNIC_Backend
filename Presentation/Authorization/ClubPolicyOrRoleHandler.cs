using BusinessLogic.Services.Interface;
using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Presentation.Authorization
{
    /// <summary>
    /// Handles <see cref="ClubPolicyOrRoleRequirement"/> with OR logic:
    ///   1. Role check  – succeeds when the user's UserRole claim matches any required role.
    ///   2. Club-policy check – succeeds when the user holds the required policy inside
    ///      the club identified by the {clubId} route parameter.
    /// Authorization passes when at least one condition is true.
    /// </summary>
    public class ClubPolicyOrRoleHandler : AuthorizationHandler<ClubPolicyOrRoleRequirement>
    {
        private readonly IPolicyService _policyService;
        private readonly IUserService _userService;
        private readonly IClubService _clubService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IClubMemberService _memberService;

        public ClubPolicyOrRoleHandler(
            IPolicyService policyService,
            IHttpContextAccessor httpContextAccessor, IUserService userService, IClubService clubService, IClubMemberService memberService)
        {
            _policyService = policyService;
            _httpContextAccessor = httpContextAccessor;
            _userService = userService;
            _clubService = clubService;
            _memberService = memberService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ClubPolicyOrRoleRequirement requirement)
        {
            // ── 1. Role  ───────────────────────────────────────
            if (requirement.Roles.Length > 0)
            {
                // JwtService adds UserRole names as ClaimTypes.Role.
                // We also support a custom "UserRole" claim for flexibility.
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
                   ?? context.User.FindFirst("UserId");

                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return;
                }

                var userRoles = await _policyService.GetUserRole(userId);
                var roleSet = userRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (requirement.Roles.Any(r => roleSet.Contains(r)))
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            // ── 2. Club-scoped policy check ───────────────────────────────────────
            if (!string.IsNullOrEmpty(requirement.PolicyTitle))
            {
                // Resolve userId from common claim types
                var userIdClaim = context.User.FindFirst("UserId")
                    ?? context.User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? context.User.FindFirst("sub")
                    ?? context.User.FindFirst("userId");

                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    context.Fail();
                    return;
                }

                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    context.Fail();
                    return;
                }

                // clubId must exist in the route, e.g. /api/clubs/{clubId}/...
                var routeValues = httpContext.Request.RouteValues;
                if (!routeValues.TryGetValue("clubId", out var clubIdObj)
                    || !int.TryParse(clubIdObj?.ToString(), out var clubId))
                {
                    context.Fail();
                    return;
                }
                var userRoles = await _policyService.GetUserRole(userId);
                var isAdmin = userRoles.Any(r => string.Equals(r, "admin", StringComparison.OrdinalIgnoreCase));

                if (!isAdmin)
                {
                    if (await _clubService.isDeleted(clubId)
                        || !(await _userService.isActiveAccount(userId))
                        || !(await _memberService.IsMemberActiveAsync(userId, clubId)))
                    {
                        context.Fail();
                        return;
                    }
                }
                var hasPolicy = await _policyService.HasMemberPolicyInClubAsync(
                    userId, clubId, requirement.PolicyTitle);

                if (hasPolicy)
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            // Neither condition passed
            context.Fail();
        }
    }
}
