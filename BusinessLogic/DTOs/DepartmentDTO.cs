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
}
