using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.DTOs
{
    public class UserInfoDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Avatar { get; set; }
        public string? StudentId { get; set; }
        public string? Major { get; set; }
        public string? Status { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<UserClubRoleDto> ClubRoles { get; set; } = new List<UserClubRoleDto>();
    }

    public class UserClubRoleDto
    {
        public int ClubId { get; set; }
        public string RoleName { get; set; } = null!;
        public int Level { get; set; }
    }
}
