using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNIC.BusinessLogic.DTOs
{
    public class ClubCreationRequestDto
    {
        public int RequestId { get; set; }

        public Guid UserId { get; set; }

        public string ClubName { get; set; }

        public string Description { get; set; }

        public string Reason { get; set; }

        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CreateClubCreationRequestDto
    {
        public Guid UserId { get; set; }

        public string ClubName { get; set; }

        public string Description { get; set; }

        public string Reason { get; set; }
    }

    public class UpdateClubCreationRequestDto
    {
        public string ClubName { get; set; }

        public string Description { get; set; }

        public string Reason { get; set; }

        public string Status { get; set; }
    }

    public class UpdateClubRequestStatusDto
    {
        public string Status { get; set; } = null!;
        public string? AdminComment { get; set; }
    }
}
