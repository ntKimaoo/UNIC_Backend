using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UNIC.Presentation.Controllers
{
    /// <summary>
    /// Public event endpoints (read-only & non-club-scoped).
    /// Club-scoped management actions are in ClubEventsController.
    /// </summary>
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IQRCodeGeneratorService _qrCodeGeneratorService;
        private readonly IEventPermissionService _eventPermService;

        public EventsController(
            IEventService eventService,
            IQRCodeGeneratorService qrCodeGeneratorService,
            IEventPermissionService eventPermService)
        {
            _eventService = eventService;
            _qrCodeGeneratorService = qrCodeGeneratorService;
            _eventPermService = eventPermService;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst("UserId")
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
        }

        /// <summary>
        /// Get all events the current user participates in (as attendee or collaborator).
        /// Returns policies per event for frontend action gating.
        /// </summary>
        [HttpGet("my-events")]
        [Authorize]
        public async Task<ActionResult<MyEventsPagedResult>> GetMyEvents(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                    return Unauthorized(new { error = "User not authenticated." });

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;

                var result = await _eventPermService.GetMyEventsAsync(userId, search, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        /// <summary>
        /// Get event by ID (public)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<EventDetailDto>> GetEventById(int id)
        {
            try
            {
                var eventDto = await _eventService.GetEventByIdAsync(id);
                return Ok(eventDto);
            }
            catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { error = "An error occurred", details = ex.Message }); }
        }

        [HttpGet]
        public async Task<ActionResult> GetAllEvents(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] int? clubId = null)
        {
            if (pageNumber < 1) return BadRequest(new { error = "Page number must be greater than 0." });
            if (pageSize < 1 || pageSize > 100) return BadRequest(new { error = "Page size must be between 1 and 100." });
            try
            {
                // Nếu user đã đăng nhập → truyền userId để hiển thị cả event nội bộ CLB
                var userId = GetUserId();
                Guid? userIdParam = userId != Guid.Empty ? userId : null;

                var events = await _eventService.GetAllEventsAsync(pageNumber, pageSize, status, clubId, userIdParam);
                var total = await _eventService.GetTotalEventsCountAsync(status, clubId, userIdParam);
                return Ok(new { items = events, total, page = pageNumber, pageSize });
            }
            catch (Exception ex) { return StatusCode(500, new { error = "An error occurred", details = ex.Message }); }
        }

        /// <summary>
        /// Get QR code image (public, anonymous)
        /// </summary>
        [HttpGet("qr/{token}")]
        [AllowAnonymous]
        public IActionResult GetQrCodeImage(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return BadRequest();
            var pngBytes = _qrCodeGeneratorService.GetQrCodePngBytes(token);
            if (pngBytes == null || pngBytes.Length == 0) return NotFound();
            return File(pngBytes, "image/png");
        }
    }
}
