using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace UNIC.Presentation.Controllers
{
    /// <summary>
    /// User-facing attendance endpoints (register, cancel, check-in, QR).
    /// Club-scoped management actions are in ClubAttendanceController.
    /// </summary>
    [ApiController]
    [Route("api/events")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
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
        /// Check in to an event using a code
        /// </summary>
        [HttpPost("{id}/checkin")]
        [Authorize]
        public async Task<IActionResult> CheckIn(int id, [FromBody] CheckInRequest request)
        {
            try
            {
                if (id != request.EventId) return BadRequest(new { error = "Mã sự kiện không khớp." });

                var token = request?.Code?.Trim();
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
        /// Evaluate a member's attendance for an event (Manager/Admin only)
        /// </summary>
        [HttpPost("{id}/evaluate")]
        // [Authorize(Roles = "Manager,Admin")] // Uncomment when authentication is fully set up
        public async Task<IActionResult> EvaluateMember(int id, [FromBody] EvaluateMemberRequest request)
        {
            // TODO: implement _attendanceService.EvaluateMemberAsync when service method is available
            return StatusCode(501, new { error = "Not implemented yet." });
        }
    }
}
