using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UNIC.Presentation.Controllers
{
    /// <summary>
    /// Club-scoped attendance management endpoints.
    /// Route: /api/club/{clubId}/events
    /// Actions use [RequireEventPolicy] for granular event-level permission.
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

        /// <summary>
        /// Approve a pending registration
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
        /// Reject a pending registration
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
        /// Bulk approve multiple pending registrations
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
        /// Generate check-in code for an event
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
        /// Check in a participant by scanning their QR code
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
        /// Evaluate a member's performance at an event
        /// </summary>
        [HttpPost("{id}/evaluate")]
        [RequireEventPolicy("approveattendance")]
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
        /// Get all attendees for an event
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
