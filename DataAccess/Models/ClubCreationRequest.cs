using DataAccess.Models;
using System;
using System.Collections.Generic;

namespace UNIC.DataAccess.Models
{
    public class ClubCreationRequest
    {
        public int RequestId { get; set; }

        public Guid UserId { get; set; }

        public string ClubName { get; set; }

        public string Description { get; set; }

        public string Reason { get; set; }

        public string Status { get; set; } // Pending, Approved, Rejected

        public DateTime CreatedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public Guid? ReviewedBy { get; set; }

        public string? AdminComment { get; set; }


        public User User { get; set; }

        public User ReviewedByUser { get; set; }
    }
}