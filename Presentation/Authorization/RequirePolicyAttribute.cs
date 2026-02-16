using Microsoft.AspNetCore.Authorization;
using System;

namespace Presentation.Authorization
{
    /// <summary>
    /// Attribute to require a specific policy for accessing an endpoint
    /// Usage: [RequirePolicy("ViewClubs")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequirePolicyAttribute : AuthorizeAttribute
    {
        public RequirePolicyAttribute(string policyTitle)
        {
            // Set the Policy property to use our dynamic policy provider
            Policy = $"Policy_{policyTitle}";
        }
    }
}
