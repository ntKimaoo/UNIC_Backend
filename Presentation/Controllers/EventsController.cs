using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UNIC.Presentation.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IQRCodeGeneratorService _qrCodeGeneratorService;

        public EventsController(IEventService eventService, IFileStorageService fileStorageService, IQRCodeGeneratorService qrCodeGeneratorService)
        {
            _eventService = eventService;
            _fileStorageService = fileStorageService;
            _qrCodeGeneratorService = qrCodeGeneratorService;
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

        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<EventDetailDto>> CreateEvent([FromForm] CreateEventRequest request, IFormFile? image)
        {
            try
            {
                if (request.ClubId.HasValue && !IsClubManager(request.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "You do not have Manager permissions for this club." });

                string? imageUrl = null;
                if (image != null && image.Length > 0)
                {
                    imageUrl = await _fileStorageService.SaveFileAsync(image, "uniclub/events");
                }

                var eventDto = await _eventService.CreateEventAsync(request, imageUrl);
                return CreatedAtAction(nameof(GetEventById), new { id = eventDto.EventId }, eventDto);
            }
            catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { error = "An error occurred", details = ex.Message }); }
        }

        [HttpPut("{id}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<EventDetailDto>> UpdateEvent(int id, [FromForm] UpdateEventRequest request, IFormFile? image)
        {
            try
            {
                if (id != request.EventId) return BadRequest(new { error = "Event ID mismatch" });
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId.HasValue && !IsClubManager(existingEvent.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "You do not have Manager permissions." });

                if (image != null && image.Length > 0)
                {
                    request.ImageUrl = await _fileStorageService.SaveFileAsync(image, "uniclub/events");
                }

                var eventDto = await _eventService.UpdateEventAsync(request);
                return Ok(eventDto);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

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

        [HttpPost("{id}/image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadEventImage(int id, IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0) return BadRequest(new { error = "No image file provided." });
                var eventDto = await _eventService.GetEventByIdAsync(id);
                var imageUrl = await _fileStorageService.SaveFileAsync(image, "uniclub/events");
                await _eventService.UpdateEventAsync(new UpdateEventRequest
                {
                    EventId = id,
                    EventName = eventDto.EventName,
                    Description = eventDto.Description,
                    Location = eventDto.Location,
                    StartDate = eventDto.StartDate,
                    EndDate = eventDto.EndDate,
                    ImageUrl = imageUrl
                });
                return Ok(new { message = "Image uploaded successfully.", imageUrl });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("{id}/sessions")]
        [Authorize]
        public async Task<ActionResult<SessionDto>> CreateSession(int id, [FromBody] CreateSessionRequest request)
        {
            try
            {
                if (id != request.EventId) return BadRequest(new { error = "Event ID mismatch" });
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId.HasValue && !IsClubManager(existingEvent.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "You do not have Manager permissions." });

                var sessionDto = await _eventService.CreateSessionAsync(request);
                return CreatedAtAction(nameof(GetEventById), new { id = request.EventId }, sessionDto);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPatch("{id}/open-registration")]
        [Authorize]
        public async Task<ActionResult<EventDetailDto>> OpenRegistration(int id, [FromBody] OpenRegistrationRequest request)
        {
            try
            {
                if (id != request.EventId) return BadRequest(new { error = "Event ID mismatch" });
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId.HasValue && !IsClubManager(existingEvent.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "You do not have Manager permissions." });

                var eventDto = await _eventService.OpenRegistrationAsync(request);
                return Ok(eventDto);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpGet("qr/{token}")]
        [AllowAnonymous]
        public IActionResult GetQrCodeImage(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return BadRequest();
            var pngBytes = _qrCodeGeneratorService.GetQrCodePngBytes(token);
            if (pngBytes == null || pngBytes.Length == 0) return NotFound();
            return File(pngBytes, "image/png");
        }

        [HttpPut("{id}/start")]
        [Authorize]
        public async Task<IActionResult> StartEvent(int id)
        {
            try
            {
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId.HasValue && !IsClubManager(existingEvent.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "You do not have Manager permissions." });

                var result = await _eventService.StartEventAsync(id);
                return Ok(new { checkInCode = result.checkInCode, expiresAt = result.expiresAt });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPut("{id}/complete")]
        [Authorize]
        public async Task<IActionResult> CompleteEvent(int id)
        {
            try
            {
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId.HasValue && !IsClubManager(existingEvent.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "You do not have Manager permissions." });

                await _eventService.CompleteEventAsync(id);
                return Ok(new { message = "Event completed." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }
    }
}
