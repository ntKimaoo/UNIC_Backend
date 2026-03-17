using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System;

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

        public EventsController(IEventService eventService, IQRCodeGeneratorService qrCodeGeneratorService)
        {
            _eventService = eventService;
            _qrCodeGeneratorService = qrCodeGeneratorService;
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

        /// <summary>
        /// Get all events with pagination (public)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventDetailDto>>> GetAllEvents([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) return BadRequest(new { error = "Page number must be greater than 0." });
            if (pageSize < 1 || pageSize > 100) return BadRequest(new { error = "Page size must be between 1 and 100." });
            try
            {
                var events = await _eventService.GetAllEventsAsync(pageNumber, pageSize);
                return Ok(events);
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
