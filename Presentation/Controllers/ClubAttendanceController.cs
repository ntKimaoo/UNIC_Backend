using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using DataAccess.Enums;
using DataAccess.Repositories.Interface;
using Microsoft.AspNetCore.Authorization;
using Presentation.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace UNIC.Presentation.Controllers
{
    /// <summary>
    /// Club-scoped attendance management endpoints.
    /// Route: /api/club/{clubId}/events
    /// Permission: System Admin > Club Manager > EventCollaborator(role)
    /// </summary>
    [ApiController]
    [Route("api/club/{clubId}/events")]
    [Authorize]
    public class ClubAttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IEventService _eventService;
        private readonly IUnitOfWork _unitOfWork;

        public ClubAttendanceController(
            IAttendanceService attendanceService,
            IEventService eventService,
            IUnitOfWork unitOfWork)
        {
            _attendanceService = attendanceService;
            _eventService = eventService;
            _unitOfWork = unitOfWork;
        }

        #region Permission Helpers

        private class ClubRoleClaimDto
        {
            public int ClubId { get; set; }
            public string RoleName { get; set; } = string.Empty;
            public int Level { get; set; }
        }

        private Guid? GetCurrentUserId()
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value
                   ?? User.FindFirst("UserId")?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }

        private bool IsClubManager(int clubId)
        {
            if (User.IsInRole("Admin")) return true;
            var clubRolesClaim = User.FindFirst("club_roles")?.Value;
            if (string.IsNullOrEmpty(clubRolesClaim)) return false;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var roles = JsonSerializer.Deserialize<List<ClubRoleClaimDto>>(clubRolesClaim, options);
                return roles != null && roles.Any(r => r.ClubId == clubId && (r.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase) || r.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase)));
            }
            catch { return false; }
        }

        private async Task<bool> HasEventPermission(int clubId, int eventId, params string[] requiredPolicies)
        {
            if (User.IsInRole("Admin")) return true;
            if (IsClubManager(clubId)) return true;

            var userId = GetCurrentUserId();
            if (userId == null) return false;

            var collab = await _unitOfWork.EventMembers.GetByEventAndUserAsync(eventId, userId.Value);
            if (collab == null) return false;

            if (requiredPolicies.Length == 0) return true;

            var rolePolicies = collab.EventRole?.EventRolePolicies?.Select(p => p.Policy?.Name) ?? Enumerable.Empty<string>();
            var memberPolicies = collab.EventMemberPolicies?.Select(p => p.Policy?.Name) ?? Enumerable.Empty<string>();
            var allUserPolicies = rolePolicies.Union(memberPolicies).Where(p => p != null).ToList();

            return requiredPolicies.Any(p => allUserPolicies.Contains(p, StringComparer.OrdinalIgnoreCase));
        }

        private ObjectResult Forbidden(string message = "Bạn không có quyền thực hiện hành động này.")
            => StatusCode(StatusCodes.Status403Forbidden, new { error = message });

        #endregion

        /// <summary>
        /// Approve registration — CREATOR, MANAGER, COORDINATOR
        /// </summary>
        [HttpPost("{id}/approve/{userId}")]
        [RequireEventPolicy("approveattendance")]
        public async Task<IActionResult> ApproveRegistration(int clubId, int id, Guid userId)
        {
            try
            {

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                await _attendanceService.ApproveRegistrationAsync(id, userId);
                return Ok(new { message = "Đã duyệt đăng ký thành công." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Reject registration — CREATOR, MANAGER, COORDINATOR
        /// </summary>
        [HttpPost("{id}/reject/{userId}")]
        [RequireEventPolicy("approveattendance")]
        public async Task<IActionResult> RejectRegistration(int clubId, int id, Guid userId)
        {
            try
            {

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                await _attendanceService.RejectRegistrationAsync(id, userId);
                return Ok(new { message = "Đã từ chối đăng ký." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Bulk approve — CREATOR, MANAGER, COORDINATOR
        /// </summary>
        [HttpPost("{id}/approve-bulk")]
        [RequireEventPolicy("approveattendance")]
        public async Task<IActionResult> BulkApproveRegistrations(int clubId, int id, [FromBody] List<Guid> userIds)
        {
            try
            {

                if (userIds == null || userIds.Count == 0)
                    return BadRequest(new { error = "Danh sách userId không được rỗng." });

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                var approvedCount = await _attendanceService.BulkApproveAsync(id, userIds);
                return Ok(new { message = $"Đã duyệt {approvedCount}/{userIds.Count} đăng ký.", approvedCount });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        /// <summary>
        /// Generate check-in code — CREATOR, MANAGER, COORDINATOR, CHECKER
        /// </summary>
        [HttpPost("{id}/checkin-code")]
        [RequireEventPolicy("checkin")]
        public async Task<ActionResult<CheckInCodeResponse>> GenerateCheckInCode(int clubId, int id)
        {
            try
            {

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                var response = await _attendanceService.GenerateCheckInCodeAsync(id);
                return Ok(response);
            }
            catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { error = "Lỗi khi tạo mã điểm danh", details = ex.Message }); }
        }

        /// <summary>
        /// Check in by QR — CREATOR, MANAGER, COORDINATOR, CHECKER
        /// </summary>
        [HttpPost("{id}/checkin-qr")]
        [RequireEventPolicy("checkin")]
        public async Task<ActionResult<CheckInByQrResponse>> CheckInByQr(int clubId, int id, [FromBody] CheckInByQrRequest request)
        {
            try
            {

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                var token = request?.Token?.Trim();
                if (string.IsNullOrEmpty(token))
                    return BadRequest(new { error = "Mã QR không hợp lệ." });

                var response = await _attendanceService.CheckInByQrTokenAsync(id, token);
                return Ok(response);
            }
            catch (NotFoundException) { return NotFound(new { error = "Mã QR không hợp lệ hoặc đã hết hạn." }); }
            catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { error = "Lỗi khi điểm danh bằng QR", details = ex.Message }); }
        }

        /// <summary>
        /// Evaluate member — CREATOR, MANAGER, COORDINATOR
        /// </summary>
        [HttpPost("{id}/evaluate")]
        [RequireEventPolicy("evaluatemember")]
        public async Task<IActionResult> EvaluateMember(int clubId, int id, [FromBody] EvaluateMemberRequest request)
        {
            try
            {

                if (id != request.EventId) return BadRequest(new { error = "Mã sự kiện không khớp." });

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                await _attendanceService.EvaluateMemberAsync(request);
                return Ok(new { message = "Đã đánh giá thành viên thành công." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Get attendees — CREATOR, MANAGER, COORDINATOR, CHECKER
        /// </summary>
        [HttpGet("{id}/attendees")]
        [RequireEventPolicy("viewattendance")]
        public async Task<ActionResult<IEnumerable<AttendanceDetailDto>>> GetEventAttendees(int clubId, int id)
        {
            try
            {

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                var attendees = await _attendanceService.GetEventAttendeesAsync(id);
                return Ok(attendees);
            }
            catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { error = "Lỗi khi lấy danh sách tham gia", details = ex.Message }); }
        }
    }
}
