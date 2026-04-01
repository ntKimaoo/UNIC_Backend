using System;
using System.Collections.Generic;
using UNIC.DataAccess.Models;

namespace DataAccess.Models
{
    public class EventRolePolicy
    {
        public int EventRoleId { get; set; }
        public EventRole EventRole { get; set; }
        public int PolicyId { get; set; }
        public Policy Policy { get; set; }
    }
}
