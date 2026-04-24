using Microsoft.AspNetCore.Authorization;

namespace Presentation.Authorization
{
    /// <summary>
    /// Custom authorization requirement for policy-based access control
    /// </summary>
    public class PolicyRequirement : IAuthorizationRequirement
    {
        public string PolicyTitle { get; }

        public PolicyRequirement(string policyTitle)
        {
            PolicyTitle = policyTitle;
        }
    }
}