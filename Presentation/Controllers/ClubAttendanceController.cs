using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace UNIC.Presentation.Controllers
{
    /// <summary>
    /// Club-scoped attendance management endpoints (Manager-only actions).
    /// Route: /api/club/{clubId}/events
    /// </summary>
    [ApiController]
    [Route("api/club/{clubId}/events")]
    [Authorize]
    public class ClubAttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IEventService _eventService;

        public ClubAttendanceController(IAttendanceService attendanceService, IEventService eventService)
        {
            _attendanceService = attendanceService;
            _eventService = eventService;
        }

        private class ClubRoleClaimDto
        {
            public int ClubId { get; set; }
            public string RoleName { get; set; } = string.Empty;
            public int Level { get; set; }
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

        /// <summary>
        /// Approve a pending registration (Manager only)
        /// </summary>
        [HttpPost("{id}/approve/{userId}")]
        public async Task<IActionResult> ApproveRegistration(int clubId, int id, Guid userId)
        {
            try
            {
                if (!IsClubManager(clubId))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "Bạn không có quyền quản lý sự kiện này." });

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                await _attendanceService.ApproveRegistrationAsync(id, userId);
                return Ok(new { message = "Đã duyệt đăng ký thành công." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Reject a pending registration (Manager only)
        /// </summary>
        [HttpPost("{id}/reject/{userId}")]
        public async Task<IActionResult> RejectRegistration(int clubId, int id, Guid userId)
        {
            try
            {
                if (!IsClubManager(clubId))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "Bạn không có quyền quản lý sự kiện này." });

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                await _attendanceService.RejectRegistrationAsync(id, userId);
                return Ok(new { message = "Đã từ chối đăng ký." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Bulk approve multiple pending registrations (Manager only)
        /// </summary>
        [HttpPost("{id}/approve-bulk")]
        public async Task<IActionResult> BulkApproveRegistrations(int clubId, int id, [FromBody] List<Guid> userIds)
        {
            try
            {
                if (!IsClubManager(clubId))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "Bạn không có quyền quản lý sự kiện này." });

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
        /// Generate check-in code for an event (Manager only)
        /// </summary>
        [HttpPost("{id}/checkin-code")]
        public async Task<ActionResult<CheckInCodeResponse>> GenerateCheckInCode(int clubId, int id)
        {
            try
            {
                if (!IsClubManager(clubId))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "Bạn không có quyền tạo mã điểm danh." });

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
        /// Check in a participant by scanning their QR code (Manager only)
        /// </summary>
        [HttpPost("{id}/checkin-qr")]
        public async Task<ActionResult<CheckInByQrResponse>> CheckInByQr(int clubId, int id, [FromBody] CheckInByQrRequest request)
        {
            try
            {
                if (!IsClubManager(clubId))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "Bạn không có quyền điểm danh cho sự kiện của CLB này." });

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
        /// Evaluate a member's performance at an event (Manager only)
        /// </summary>
        [HttpPost("{id}/evaluate")]
        public async Task<IActionResult> EvaluateMember(int clubId, int id, [FromBody] EvaluateMemberRequest request)
        {
            try
            {
                if (!IsClubManager(clubId))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "Bạn không có quyền đánh giá thành viên." });

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
        /// Get all attendees for an event (Manager only)
        /// </summary>
        [HttpGet("{id}/attendees")]
        public async Task<ActionResult<IEnumerable<AttendanceDetailDto>>> GetEventAttendees(int clubId, int id)
        {
            try
            {
                if (!IsClubManager(clubId))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "Bạn không có quyền xem danh sách tham gia." });

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
