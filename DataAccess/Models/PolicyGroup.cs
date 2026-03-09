using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNIC.DataAccess.Models
{
    public class PolicyGroup
    {
        public int PolicyGroupId { get; set; }
        public string Name { get; set; } = null!;
        public string Title { get; set; } = null!;
        public virtual ICollection<Policy> Policies { get; set; }
    }
}
