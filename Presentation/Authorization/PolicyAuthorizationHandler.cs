using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Presentation.Authorization
{
    /// <summary>
    /// Authorization handler that checks if user has required policy
    /// </summary>
    public class PolicyAuthorizationHandler : AuthorizationHandler<PolicyRequirement>
    {
        private readonly IPolicyService _policyService;

        public PolicyAuthorizationHandler(IPolicyService policyService)
        {
            _policyService = policyService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PolicyRequirement requirement)
        {
            // Get user ID from claims
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirst("sub")
                ?? context.User.FindFirst("userId");

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                // User not authenticated or invalid user ID
                context.Fail();
                return;
            }

            // Check if user has the required policy
            var hasPolicy = await _policyService.HasUserPolicyAsync(userId, requirement.PolicyTitle);

            if (hasPolicy)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }
}
