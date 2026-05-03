using Microsoft.AspNetCore.Authorization;
using System;

namespace Presentation.Authorization
{
    /// <summary>
    /// Requires the authenticated user to hold a specific policy
    /// inside the club identified by the {clubId} route parameter.
    ///
    /// Usage: [RequireClubPolicy("viewrole")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireClubPolicyAttribute : AuthorizeAttribute
    {
        public RequireClubPolicyAttribute(string policyTitle)
        {
            // DynamicPolicyProvider resolves ClubPolicy_<policyTitle>
            Policy = $"ClubPolicy_{policyTitle}";
        }
    }

    /// <summary>
    /// Requires the authenticated user to have one or more of the specified
    /// system roles (matched against the "UserRole" claim in the JWT).
    ///
    /// Usage:  [RequireRole("SystemAdmin")]
    ///         [RequireRole("SystemAdmin", "Moderator")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireRoleAttribute : AuthorizeAttribute
    {
        public RequireRoleAttribute(params string[] roles)
        {
            // DynamicPolicyProvider resolves Role_<role1>,<role2>,...
            Policy = $"Role_{string.Join(",", roles)}";
        }
    }

    /// <summary>
    /// Requires the authenticated user to pass either:
    ///   • a club-scoped policy check (<paramref name="policyTitle"/>), OR
    ///   • a system role check (any of <paramref name="roles"/>).
    ///
    /// Usage: [RequireClubPolicyOrRole("editRole", "SystemAdmin", "Moderator")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireClubPolicyOrRoleAttribute : AuthorizeAttribute
    {
        public RequireClubPolicyOrRoleAttribute(string policyTitle, params string[] roles)
        {
            // DynamicPolicyProvider resolves ClubPolicyOrRole_<policyTitle>|<role1>,<role2>,...
            Policy = $"ClubPolicyOrRole_{policyTitle}|{string.Join(",", roles)}";
        }
    }

    /// <summary>
    /// Requires the authenticated user to have a specific policy
    /// inside the event identified by the {eventId} or {id} route parameter.
    /// Falls back to Club Manager check (auto-pass for managers).
    ///
    /// Usage: [RequireEventPolicy("editevent")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireEventPolicyAttribute : AuthorizeAttribute
    {
        public RequireEventPolicyAttribute(string policyTitle)
        {
            // DynamicPolicyProvider resolves EventPolicy_<policyTitle>
            Policy = $"EventPolicy_{policyTitle}";
        }
    }
}