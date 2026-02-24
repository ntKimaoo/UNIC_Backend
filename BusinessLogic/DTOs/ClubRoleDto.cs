using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    public class CreateClubRoleDto
    {
        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; }

        public string? Description { get; set; }

        public int Level { get; set; } = 0;

        /// <summary>
        /// Danh sách ID của các policy muốn gán cho role này
        /// </summary>
        public List<int> policies { get; set; } = new();
    }

    public class UpdateClubRoleDto
    {
        [MaxLength(50)]
        public string? RoleName { get; set; }

        public string? Description { get; set; }

        public int? Level { get; set; }

        /// <summary>
        /// Danh sách ID policy mới (ghi đè toàn bộ).
        /// Nếu null thì không thay đổi policies.
        /// </summary>
        public List<int>? PolicyIds { get; set; }
    }

    public class ClubRoleResponseDto
    {
        public int ClubRoleId { get; set; }
        public string RoleName { get; set; }
        public string? Description { get; set; }
        public int Level { get; set; }
        public int MemberCount { get; set; }
        public List<PolicyResponseDto> Policies { get; set; } = new();
    }

    public class PolicyResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
    }
}
