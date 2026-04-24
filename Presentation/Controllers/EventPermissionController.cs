using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace UNIC.Presentation.Controllers
{
    /// <summary>
    /// Event team (ban tổ chức) management endpoints.
    /// Route: /api/club/{clubId}/events/{eventId}/team
    /// </summary>
    [ApiController]
    [Route("api/club/{clubId}/events/{eventId}")]
    [Authorize]
    public class EventPermissionController : ControllerBase
    {
        private readonly IEventPermissionService _eventPermService;
        private readonly IEventService _eventService;

        public EventPermissionController(
            IEventPermissionService eventPermService,
            IEventService eventService)
        {
            _eventPermService = eventPermService;
            _eventService = eventService;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst("UserId")
                ?? User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
        }

        // ── Team Members ──

        /// <summary>Lấy danh sách ban tổ chức event</summary>
        [HttpGet("members")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<ActionResult<IEnumerable<EventMemberDto>>> GetTeamMembers(int clubId, int eventId)
        {
            try
            {
                var ev = await _eventService.GetEventByIdAsync(eventId);
                if (ev.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                var members = await _eventPermService.GetEventMembersAsync(eventId);
                return Ok(members);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        /// <summary>Thêm thành viên vào ban tổ chức</summary>
        [HttpPost("members")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<ActionResult<EventMemberDto>> AddTeamMember(
            int clubId, int eventId, [FromBody] AddEventMemberRequest request)
        {
            try
            {
                var ev = await _eventService.GetEventByIdAsync(eventId);
                if (ev.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                var userId = GetUserId();
                var member = await _eventPermService.AddEventMemberAsync(eventId, request, userId);
                return Ok(member);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>Xóa thành viên khỏi ban tổ chức</summary>
        [HttpDelete("members/{memberId}")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<IActionResult> RemoveTeamMember(int clubId, int eventId, int memberId)
        {
            try
            {
                await _eventPermService.RemoveEventMemberAsync(memberId);
                return NoContent();
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>Đổi role cho thành viên ban tổ chức</summary>
        [HttpPut("members/{memberId}/role")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<IActionResult> UpdateMemberRole(
            int clubId, int eventId, int memberId, [FromBody] UpdateEventMemberRoleRequest request)
        {
            try
            {
                await _eventPermService.UpdateEventMemberRoleAsync(memberId, request.EventRoleId);
                return Ok(new { message = "Đã cập nhật role." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        // ── Event Roles ──

        /// <summary>Lấy danh sách roles của event</summary>
        [HttpGet("roles")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<ActionResult<IEnumerable<EventRoleDto>>> GetEventRoles(int clubId, int eventId)
        {
            try
            {
                var roles = await _eventPermService.GetEventRolesAsync(eventId);
                return Ok(roles);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        /// <summary>Tạo role mới cho event</summary>
        [HttpPost("roles")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<ActionResult<EventRoleDto>> CreateEventRole(
            int clubId, int eventId, [FromBody] CreateEventRoleRequest request)
        {
            try
            {
                var role = await _eventPermService.CreateEventRoleAsync(eventId, request);
                return Ok(role);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>Xóa role khỏi event</summary>
        [HttpDelete("roles/{roleId}")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<IActionResult> RemoveEventRole(int clubId, int eventId, int roleId)
        {
            try
            {
                await _eventPermService.RemoveEventRoleAsync(roleId);
                return Ok(new { success = true });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>Cập nhật role event (tên, mô tả)</summary>
        [HttpPut("roles/{roleId}")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<ActionResult<EventRoleDto>> UpdateEventRole(
            int clubId, int eventId, int roleId, [FromBody] UpdateEventRoleRequest request)
        {
            try
            {
                var role = await _eventPermService.UpdateEventRoleAsync(roleId, request.RoleName, request.Description);
                return Ok(role);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>Set policies cho role event</summary>
        [HttpPut("roles/{roleId}/policies")]
        [RequireEventPolicy("managecollaborator")]
        public async Task<IActionResult> SetEventRolePolicies(
            int clubId, int eventId, int roleId, [FromBody] List<string> policies)
        {
            try
            {
                await _eventPermService.SetEventRolePoliciesAsync(roleId, policies);
                return Ok(new { message = "Cập nhật quyền thành công." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        // ── Permissions Check ──

        /// <summary>Xem quyền của mình trên event</summary>
        [HttpGet("my-permissions")]
        public async Task<ActionResult<EventPermissionSummaryDto>> GetMyPermissions(int clubId, int eventId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                    return Unauthorized(new { error = "User not authenticated." });

                var permissions = await _eventPermService.GetUserEventPermissionsAsync(userId, eventId);
                return Ok(permissions);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        /// <summary>Xem role + policies của mình trên event (dùng bởi frontend useEventPermission hook)</summary>
        [HttpGet("my-role")]
        public async Task<IActionResult> GetMyRole(int clubId, int eventId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                    return Unauthorized(new { error = "User not authenticated." });

                var permissions = await _eventPermService.GetUserEventPermissionsAsync(userId, eventId);
                // Return shape expected by frontend: { role, policies }
                string? role = permissions.IsClubManager ? "ADMIN"
                             : permissions.IsEventMember ? (permissions.RoleName ?? "MEMBER")
                             : null;
                return Ok(new { role, policies = permissions.Policies });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }
    }
}
