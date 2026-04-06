using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using DataAccess.Models;

namespace UNIC.DataAccess.Models
{
    public class Policy
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public virtual IList<ClubRolePolicy> ClubRolePolicies { get; set; }
        public virtual IList<ClubMemberPolicy> ClubMemberPolicies { get; set; }

        public virtual IList<EventRolePolicy> EventRolePolicies { get; set; }
        public virtual IList<EventMemberPolicy> EventMemberPolicies { get; set; }
        public int? PolicyGroupId { get; set; }
        public PolicyGroup PolicyGroup { get; set; }
    }
}
