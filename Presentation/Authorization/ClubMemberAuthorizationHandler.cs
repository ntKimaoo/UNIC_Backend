using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Presentation.Authorization
{
    /// <summary>
    /// Authorization handler that checks if the authenticated user has a required
    /// policy within the club identified by the {clubId} route parameter.
    /// </summary>
    public class ClubMemberAuthorizationHandler : AuthorizationHandler<ClubMemberRequirement>
    {
        private readonly IPolicyService _policyService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClubMemberAuthorizationHandler(
            IPolicyService policyService,
            IHttpContextAccessor httpContextAccessor)
        {
            _policyService = policyService;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ClubMemberRequirement requirement)
        {
            // 1. Resolve user ID from claims
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirst("sub")
                ?? context.User.FindFirst("userId");

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                context.Fail();
                return;
            }

            // 2. Read {clubId} from the route
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                context.Fail();
                return;
            }

            var routeValues = httpContext.Request.RouteValues;
            if (!routeValues.TryGetValue("clubId", out var clubIdObj)
                || !int.TryParse(clubIdObj?.ToString(), out var clubId))
            {
                // No {clubId} in this route — fail gracefully
                context.Fail();
                return;
            }

            // 3. Club-scoped policy check
            var hasPolicy = await _policyService.HasMemberPolicyInClubAsync(
                userId, clubId, requirement.PolicyTitle);

            if (hasPolicy)
                context.Succeed(requirement);
            else
                context.Fail();
        }
    }
}
