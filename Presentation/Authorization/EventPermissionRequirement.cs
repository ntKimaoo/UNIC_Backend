using Microsoft.AspNetCore.Authorization;

namespace Presentation.Authorization
{
    /// <summary>
    /// Authorization requirement for event-scoped policy checks.
    /// The clubId and eventId are resolved at runtime from route parameters.
    /// </summary>
    public class EventPermissionRequirement : IAuthorizationRequirement
    {
        public string PolicyTitle { get; }

        public EventPermissionRequirement(string policyTitle)
        {
            PolicyTitle = policyTitle;
        }
    }
}
