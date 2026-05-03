using Microsoft.AspNetCore.Authorization;

namespace Presentation.Authorization
{
    /// <summary>
    /// Authorization requirement cho event-scoped policy.
    /// Supports OR logic: comma-separated titles means user needs ANY of them.
    /// </summary>
    public class EventPolicyRequirement : IAuthorizationRequirement
    {
        /// <summary>Tên policy cần check (editevent, checkin...)</summary>
        public string PolicyTitle { get; }

        /// <summary>Multiple policy titles (OR logic). User needs ANY one.</summary>
        public string[] PolicyTitles { get; }

        public EventPolicyRequirement(string policyTitle)
        {
            PolicyTitle = policyTitle;
            PolicyTitles = policyTitle.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
