using Microsoft.AspNetCore.Authorization;

namespace Presentation.Authorization
{
    /// <summary>
    /// Authorization requirement that supports OR logic:
    /// Succeeds if the user has the required club-scoped policy
    /// OR if the user has one of the required system roles (from UserRole table).
    ///
    /// - PolicyTitle: if set, checks HasMemberPolicyInClubAsync(userId, clubId, policyTitle)
    /// - Roles: if set, checks whether the user's "UserRole" claim matches any of the roles
    ///
    /// Either condition being true is sufficient to pass authorization.
    /// </summary>
    public class ClubPolicyOrRoleRequirement : IAuthorizationRequirement
    {
        /// <summary>Title of the club-scoped policy to check. Null means skip policy check.</summary>
        public string? PolicyTitle { get; }

        /// <summary>System role names to accept. Empty means skip role check.</summary>
        public string[] Roles { get; }

        public ClubPolicyOrRoleRequirement(string? policyTitle, string[] roles)
        {
            PolicyTitle = policyTitle;
            Roles = roles ?? Array.Empty<string>();
        }
    }
}
