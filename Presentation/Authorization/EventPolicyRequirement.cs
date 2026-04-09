using Microsoft.AspNetCore.Authorization;

namespace Presentation.Authorization
{
    /// <summary>
    /// Authorization requirement cho event-scoped policy.
    /// Pattern copy từ ClubPolicyOrRoleRequirement.
    /// </summary>
    public class EventPolicyRequirement : IAuthorizationRequirement
    {
        /// <summary>Tên policy cần check (editevent, checkin...)</summary>
        public string PolicyTitle { get; }

        public EventPolicyRequirement(string policyTitle)
        {
            PolicyTitle = policyTitle;
        }
    }
}
