using DataAccess.Models;

namespace UNIC.DataAccess.Models
{
    /// <summary>
    /// Join table: EventRole ↔ Policy.
    /// Pattern copy từ ClubRolePolicy.
    /// </summary>
    public class EventRolePolicy
    {
        public int EventRoleId { get; set; }
        public EventRole EventRole { get; set; } = null!;
        public int PolicyId { get; set; }
        public Policy Policy { get; set; } = null!;
    }
}
