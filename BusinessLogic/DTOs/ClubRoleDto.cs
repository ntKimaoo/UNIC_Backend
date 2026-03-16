using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    public class ClubStructureRoleDto
    {
        public int ClubRoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Level { get; set; }
        public int MemberCount { get; set; }
        public List<PolicyResponseDto> Policies { get; set; } = new();
    }

    public class ClubStructureDepartmentDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ClubStructureRoleDto? Manager { get; set; }
        public List<ClubStructureRoleDto> Roles { get; set; } = new();
    }

    public class ClubStructureResponseDto
    {
        public List<ClubStructureRoleDto> StandaloneRoles { get; set; } = new();
        public List<ClubStructureDepartmentDto> Departments { get; set; } = new();
    }

    public class CreateClubRoleDto
    {
        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; }

        public string? Description { get; set; }

        public int Level { get; set; } = 0;

        public int? DepartmentId { get; set; }

        public List<int> policies { get; set; } = new();
    }

    public class UpdateClubRoleDto
    {
        [MaxLength(50)]
        public string? RoleName { get; set; }

        public string? Description { get; set; }

        public int? Level { get; set; }

        public int? DepartmentId { get; set; }

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
        public int clubId { get; set; }
        public int? DepartmentId { get; set; }
    }

    public class PolicyResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int? PolicyGroupId { get; set; }
    }
}
