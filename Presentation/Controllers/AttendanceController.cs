using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
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
        /// Register a member for an event
        /// </summary>
        // [HttpPost("{id}/register")]
        // public async Task<IActionResult> RegisterMember(int id, [FromBody] EventRegistrationRequest request)
        // { ... commented out to resolve Swagger conflict with EventsController ... }

        /// <summary>
        /// Generate check-in code for an event (Manager only)
        /// </summary>
        [HttpPost("{id}/checkin-code")]
        // [Authorize(Roles = "Manager,Admin")] // Uncomment when authentication is fully set up
        public async Task<ActionResult<CheckInCodeResponse>> GenerateCheckInCode(int id)
        {
            try
            {
                var response = await _attendanceService.GenerateCheckInCodeAsync(id);
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while generating check-in code", details = ex.Message });
            }
        }

        /// <summary>
        /// Get current user's QR code content for event check-in (participant shows this QR at the event; organizer scans it).
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
        /// Check in a participant by scanning their QR code (organizer/Manager only). Token is the content read from the QR.
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
            catch (NotFoundException)
            {
                return NotFound(new
                {
                    error = "Mã QR không hợp lệ hoặc đã hết hạn.",
                    hint = "Nếu bạn vừa quét thành công (đã thấy PRESENT trên màn hình), có thể do quét liên tục. Hãy bỏ điện thoại ra khỏi mã QR sau khi quét một lần."
                });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi khi điểm danh bằng QR", details = ex.Message });
            }
        }

        /// <summary>
        /// Check in to an event
        /// </summary>
        // [HttpPost("{id}/checkin")]
        // public async Task<IActionResult> CheckIn(int id, [FromBody] CheckInRequest request)
        // { ... commented out to resolve Swagger conflict with EventsController ... }

        /// <summary>
        /// Evaluate a member's performance at an event
        /// </summary>
        [HttpPost("{id}/evaluate")]
        // [Authorize(Roles = "Manager,Admin")] // Uncomment when authentication is fully set up
        public async Task<IActionResult> EvaluateMember(int id, [FromBody] EvaluateMemberRequest request)
        {
            try
            {
                if (id != request.EventId)
                {
                    return BadRequest(new { error = "Event ID in URL does not match request body" });
                }

                await _attendanceService.EvaluateMemberAsync(request);
                return Ok(new { message = "Member evaluation completed successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while evaluating the member", details = ex.Message });
            }
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
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving attendees", details = ex.Message });
            }
        }
    }
}
