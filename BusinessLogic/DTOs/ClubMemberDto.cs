using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DataAccess.Models;
namespace BusinessLogic.DTOs
{
    /// <summary>
    /// Request để thêm user vào club
    /// </summary>
    public class AddUserToClubDto
    {
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Danh sách ClubRoleId để assign cho user (tùy chọn)
        /// </summary>
        public List<int> ClubRoleIds { get; set; } = new();
    }

    /// <summary>
    /// Request để cập nhật role của member trong club
    /// </summary>
    public class UpdateMemberRoleDto
    {
        public List<int> ClubRoleIds { get; set; } = new();
    }

    /// <summary>
    /// Request để gán/cập nhật danh sách policies của member
    /// </summary>
    public class MemberPolicyDto
    {
        [Required]
        public List<int> PolicyIds { get; set; } = new();
    }

    /// <summary>
    /// Thông tin department mà user tham gia trong club
    /// </summary>
    public class DepartmentInfoDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string? Description { get; set; }
        public string? DeptRoleName { get; set; }
        public DateTime AssignedAt { get; set; }
    }

    /// <summary>
    /// Response thông tin một member trong club
    /// </summary>
    public class ClubMemberResponseDto
    {
        public int ClubMemberId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? Avatar { get; set; }
        public string? StudentId { get; set; }
        public int ClubId { get; set; }
        public List<ClubRoleInfoDto> Roles { get; set; } = new();
        public DateTime JoinDate { get; set; }
        public string Status { get; set; }
        public Guid? AssignedBy { get; set; }

        /// <summary>
        /// Danh sách departments trong club mà user thuộc về
        /// </summary>
        public List<DepartmentInfoDto> Departments { get; set; } = new();
    }

    public class ClubRoleInfoDto
    {
        public int ClubRoleId { get; set; }
        public string RoleName { get; set; }
        public int Level { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}
