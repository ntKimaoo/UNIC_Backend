using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNIC.BusinessLogic.DTOs
{
    public class DepartmentManagerRoleDto
    {
        public int ClubRoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Level { get; set; }
    }

    public class DepartmentResponseDto
    {
        public int DepartmentId { get; set; }
        public int ClubId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ManagerRoleId { get; set; }
        public DepartmentManagerRoleDto? ManagerRole { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateDepartmentDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ManagerRoleLevel must be a non-negative number")]
        public int ManagerRoleLevel { get; set; } = 1;
    }

    public class UpdateDepartmentDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }
        
        public string? Description { get; set; }
        
        public int? ManagerRoleId { get; set; }
    }

    /// <summary>
    /// One department entry returned for a user's department list in a club.
    /// </summary>
    public class UserDepartmentDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? Description { get; set; }
        /// <summary>The user's own role inside this department (null if not a role-holder).</summary>
        public DepartmentMemberRoleDto? DepartmentRole { get; set; }
        /// <summary>All roles that belong to this department.</summary>
        public List<DepartmentMemberRoleDto> Roles { get; set; } = new();
        /// <summary>Total number of members in this department.</summary>
        public int MemberCount { get; set; }
    }

    /// <summary>
    /// Role info of a member inside a department.
    /// </summary>
    public class DepartmentMemberRoleDto
    {
        public int ClubRoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Level { get; set; }
    }

    /// <summary>
    /// One member entry returned by GET club/{clubId}/department/{departmentId}/all-members.
    /// </summary>
    public class DepartmentMemberDto
    {
        public int ClubMemberId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? StudentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        /// <summary>The member's club-wide role (ClubRole assigned via UserClubRole).</summary>
        public DepartmentMemberRoleDto? DepartmentRole { get; set; }
    }
}
