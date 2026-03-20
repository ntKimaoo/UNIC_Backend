using Microsoft.AspNetCore.Authorization;
using System;

namespace Presentation.Authorization
{
    /// <summary>
    /// Requires the authenticated user to have a specific policy globally.
    /// Usage: [RequireUserPolicy("ViewClubs")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireUserPolicyAttribute : AuthorizeAttribute
    {
        public RequireUserPolicyAttribute(string policyTitle)
        {
            Policy = $"Policy_{policyTitle}";
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireClubPolicyAttribute : AuthorizeAttribute
    {
        public RequireClubPolicyAttribute(string policyTitle)
        {
            Policy = $"Policy_{policyTitle}";
        }
    }

    /// <summary>
    /// Requires the authenticated user to have a specific policy within the club
    /// identified by the {clubId} route parameter.
    /// Usage: [RequireMemberPolicy("viewrole")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireMemberPolicyAttribute : AuthorizeAttribute
    {
        public RequireMemberPolicyAttribute(string policyTitle)
        {
            // ClubPolicy_ prefix signals DynamicPolicyProvider to use ClubMemberRequirement
            Policy = $"ClubPolicy_{policyTitle}";
        }
    }
}
