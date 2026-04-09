using System;
using System.Collections.Generic;

namespace BusinessLogic.DTOs
{
    // ── Response DTOs ──

    public class EventRoleDto
    {
        public int EventRoleId { get; set; }
        public int EventId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Level { get; set; }
        public List<string> Policies { get; set; } = new();
    }

    public class EventMemberDto
    {
        public int EventMemberId { get; set; }
        public int EventId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatar { get; set; }
        public string? UserEmail { get; set; }
        public int? EventRoleId { get; set; }
        public string? RoleName { get; set; }
        public int? RoleLevel { get; set; }
        public DateTime AssignedAt { get; set; }
        public List<string> DirectPolicies { get; set; } = new();
    }

    public class EventPermissionSummaryDto
    {
        public bool IsCreator { get; set; }
        public bool IsEventMember { get; set; }
        public bool IsClubManager { get; set; }
        public string? RoleName { get; set; }
        public List<string> Policies { get; set; } = new();
    }

    // ── Request DTOs ──

    public class AddEventMemberRequest
    {
        public Guid UserId { get; set; }
        public int? EventRoleId { get; set; }
    }

    public class UpdateEventMemberRoleRequest
    {
        public int? EventRoleId { get; set; }
    }

    public class CreateEventRoleRequest
    {
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Level { get; set; } = 1;
        public List<int>? PolicyIds { get; set; }
    }
}
