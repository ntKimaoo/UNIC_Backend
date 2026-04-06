using System;
using System.Collections.Generic;
using UNIC.DataAccess.Models;

namespace DataAccess.Models
{
    public class EventMemberPolicy
    {
        public int EventMemberId { get; set; }
        public UserEventRole UserEventRole { get; set; }
        public int PolicyId { get; set; }
        public Policy Policy { get; set; }
    }
}
