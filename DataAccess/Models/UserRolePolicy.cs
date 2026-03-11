using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNIC.DataAccess.Models
{
    public class UserRolePolicy
    {
        public int RoleId { get; set; }
        public UserRole Role { get; set; }
        public int PolicyId { get; set; }
        public Policy Policy { get; set; }
    }
}
