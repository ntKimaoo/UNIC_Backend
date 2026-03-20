using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNIC.DataAccess.Models
{
    public class ClubMemberPolicy
    {
        public int ClubMemberId { get; set; }
        public UserClubRole ClubMember { get; set; }
        public int PolicyId { get; set; }
        public Policy Policy { get; set; }
    }
}
