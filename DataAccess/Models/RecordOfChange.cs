using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models
{
    public class RecordOfChange
    {
        public int Id { get; set; }
        public string EntityName { get; set; }
        public int EntityId { get; set; }   
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.Now;
        public Guid ChangedBy { get; set; }
        public virtual User ChangedByUser { get; set; }
        public string ChangeType { get; set; } // "CREATE", "UPDATE", "DELETE"
        public string Notification { get; set; } // Nguoi dung ... da thay doi ... cua ... tu ... thanh ...
        public int? ClubId { get; set; }
        public virtual Club Club { get; set; }
    }
}
