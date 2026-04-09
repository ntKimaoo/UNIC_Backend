using DataAccess.Models;

namespace UNIC.DataAccess.Models
{
    /// <summary>
    /// Join table: EventMember ↔ Policy (direct policy assignment).
    /// Pattern copy từ ClubMemberPolicy.
    /// </summary>
    public class EventMemberPolicy
    {
        public int EventMemberId { get; set; }
        public EventMember EventMember { get; set; } = null!;
        public int PolicyId { get; set; }
        public Policy Policy { get; set; } = null!;
    }
}
