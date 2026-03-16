using Microsoft.AspNetCore.Authorization;

namespace Presentation.Authorization
{
    /// <summary>
    /// Authorization requirement for club-scoped policy checks.
    /// The clubId is resolved at runtime from the route parameter.
    /// </summary>
    public class ClubMemberRequirement : IAuthorizationRequirement
    {
        public string PolicyTitle { get; }

        public ClubMemberRequirement(string policyTitle)
        {
            PolicyTitle = policyTitle;
        }
    }
}
