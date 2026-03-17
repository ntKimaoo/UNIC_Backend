using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
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
    [ApiController]
    [Route("api/events")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IEventService _eventService;

        public AttendanceController(IAttendanceService attendanceService, IEventService eventService)
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
        /// Register the current user for an event
        /// </summary>
        [HttpPost("{id}/register")]
        [Authorize]
        public async Task<IActionResult> RegisterMember(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { error = "Invalid token" });

                await _attendanceService.RegisterMemberAsync(new EventRegistrationRequest { EventId = id, UserId = userId });
                return Ok(new { message = "Yêu cầu đăng ký đã được ghi nhận." });
            }
            catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (ConflictException ex) { return Conflict(new { error = ex.Message }); }
            catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { error = "Lỗi khi đăng ký", details = ex.Message }); }
        }

        /// <summary>
        /// Approve a pending registration (Manager only)
        /// </summary>
        [HttpPost("{id}/approve/{userId}")]
        [Authorize]
        public async Task<IActionResult> ApproveRegistration(int id, Guid userId)
        {
            try
            {
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId.HasValue && !IsClubManager(existingEvent.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "Bạn không có quyền quản lý sự kiện này." });

                await _attendanceService.ApproveRegistrationAsync(id, userId);
                return Ok(new { message = "Đã duyệt đăng ký thành công." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Reject a pending registration (Manager only)
        /// </summary>
        [HttpPost("{id}/reject/{userId}")]
        [Authorize]
        public async Task<IActionResult> RejectRegistration(int id, Guid userId)
        {
            try
            {
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId.HasValue && !IsClubManager(existingEvent.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "Bạn không có quyền quản lý sự kiện này." });

                await _attendanceService.RejectRegistrationAsync(id, userId);
                return Ok(new { message = "Đã từ chối đăng ký." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Cancel own registration for an event
        /// </summary>
        [HttpPost("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelRegistration(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { error = "Invalid token" });

                await _attendanceService.CancelRegistrationAsync(id, userId);
                return Ok(new { message = "Đã hủy tham gia sự kiện." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Generate check-in code for an event (Manager only)
        /// </summary>
        [HttpPost("{id}/checkin-code")]
        public async Task<ActionResult<CheckInCodeResponse>> GenerateCheckInCode(int id)
        {
            try
            {
                var response = await _attendanceService.GenerateCheckInCodeAsync(id);
                return Ok(response);
            }
            catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { error = "Lỗi khi tạo mã điểm danh", details = ex.Message }); }
        }

        /// <summary>
        /// Get current user's QR code content for event check-in
        /// </summary>
        [HttpGet("{id}/my-checkin-qr")]
        [Authorize]
        public async Task<ActionResult<CheckInQrResponse>> GetMyCheckInQr(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { error = "Invalid token" });

            var response = await _attendanceService.GetMyCheckInQrAsync(id, userId);
            if (response == null)
                return NotFound(new { error = "Bạn chưa đăng ký sự kiện này." });

            return Ok(response);
        }

        /// <summary>
        /// Check in a participant by scanning their QR code (organizer/Manager only).
        /// </summary>
        [HttpPost("{id}/checkin-qr")]
        [Authorize]
        public async Task<ActionResult<CheckInByQrResponse>> CheckInByQr(int id, [FromBody] CheckInByQrRequest request)
        {
            try
            {
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId.HasValue && !IsClubManager(existingEvent.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "Bạn không có quyền điểm danh cho sự kiện của CLB này." });

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
        /// Check in to an event using a code
        /// </summary>
        [HttpPost("{id}/checkin")]
        [Authorize]
        public async Task<IActionResult> CheckIn(int id, [FromBody] CheckInRequest request)
        {
            try
            {
                if (id != request.EventId) return BadRequest(new { error = "Mã sự kiện không khớp." });

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { error = "Invalid token" });
                
                request.UserId = userId;
                await _attendanceService.CheckInMemberAsync(request);
                return Ok(new { message = "Điểm danh thành công." });
            }
            catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { error = "Lỗi khi điểm danh", details = ex.Message }); }
        }

        /// <summary>
        /// Evaluate a member's performance at an event (Manager only)
        /// </summary>
        [HttpPost("{id}/evaluate")]
        [Authorize]
        public async Task<IActionResult> EvaluateMember(int id, [FromBody] EvaluateMemberRequest request)
        {
            try
            {
                if (id != request.EventId) return BadRequest(new { error = "Mã sự kiện không khớp." });

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId.HasValue && !IsClubManager(existingEvent.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "Bạn không có quyền đánh giá thành viên." });

                await _attendanceService.EvaluateMemberAsync(request);
                return Ok(new { message = "Đã đánh giá thành viên thành công." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Get all attendees for an event
        /// </summary>
        [HttpGet("{id}/attendees")]
        public async Task<ActionResult<IEnumerable<AttendanceDetailDto>>> GetEventAttendees(int id)
        {
            try
            {
                var attendees = await _attendanceService.GetEventAttendeesAsync(id);
                return Ok(attendees);
            }
            catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { error = "Lỗi khi lấy danh sách tham gia", details = ex.Message }); }
        }
    }
}
