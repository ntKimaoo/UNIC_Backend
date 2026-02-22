using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNIC.DataAccess.Models
{
    public class ClubRolePolicy
    {
        public int ClubRoleId { get; set; }
        public ClubRole ClubRole { get; set; }
        public int PolicyId { get; set; }
        public Policy Policy { get; set; }
    }
}
