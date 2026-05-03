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
using Presentation.Authorization;

namespace UNIC.Presentation.Controllers
{
    /// <summary>
    /// Club-scoped event management endpoints.
    /// Route: /api/club/{clubId}/events
    /// Actions use [RequireEventPolicy] for granular event-level permission.
    /// CreateEvent uses IsClubManager (no eventId exists yet).
    /// </summary>
    [ApiController]
    [Route("api/club/{clubId}/events")]
    //[Authorize]
    public class ClubEventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IEventPermissionService _eventPermissionService;

        public ClubEventsController(
            IEventService eventService,
            IFileStorageService fileStorageService,
            IEventPermissionService eventPermissionService)
        {
            _eventService = eventService;
            _fileStorageService = fileStorageService;
            _eventPermissionService = eventPermissionService;
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst("UserId")
                ?? User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
        }

        // IsClubManager chỉ dùng cho CreateEvent (chưa có eventId)
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
                var managerNames = new[] { "Manager", "Admin", "Club Manager", "Quản lý", "Chủ nhiệm" };
                return roles != null && roles.Any(r => r.ClubId == clubId && managerNames.Any(m => r.RoleName.Equals(m, StringComparison.OrdinalIgnoreCase)));
            }
            catch { return false; }
        }

        /// <summary>
        /// Create a new event for a club (Manager only — no eventId exists yet)
        /// Auto-creates Creator role and assigns to the creator.
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<EventDetailDto>> CreateEvent(int clubId, [FromForm] CreateEventRequest request, IFormFile? image)
        {
            try
            {
                //if (!IsClubManager(clubId))
                //    return StatusCode(StatusCodes.Status403Forbidden, new { error = "You do not have Manager permissions for this club." });

                // Override ClubId from route
                request.ClubId = clubId;

                string? imageUrl = null;
                if (image != null && image.Length > 0)
                {
                    imageUrl = await _fileStorageService.SaveFileAsync(image, "uniclub/events");
                }

                var eventDto = await _eventService.CreateEventAsync(request, imageUrl);

                // Auto-create Creator role and assign to the user who created this event
                var userId = GetUserId();
                if (userId != Guid.Empty)
                {
                    await _eventPermissionService.CreateCreatorRoleAndAssignAsync(eventDto.EventId, userId);
                }

                return CreatedAtAction("GetEventById", "Events", new { id = eventDto.EventId }, eventDto);
            }
            catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { error = "An error occurred", details = ex.Message, inner = ex.InnerException?.Message }); }
        }

        /// <summary>
        /// Update an event within a club
        /// </summary>
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        //[RequireEventPolicy("editevent")]
        public async Task<ActionResult<EventDetailDto>> UpdateEvent(int clubId, int id, [FromForm] UpdateEventRequest request, IFormFile? image)
        {
            try
            {
                if (id != request.EventId) return BadRequest(new { error = "Event ID mismatch" });

                // Verify the event belongs to this club
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                if (image != null && image.Length > 0)
                {
                    request.ImageUrl = await _fileStorageService.SaveFileAsync(image, "uniclub/events");
                }

                var eventDto = await _eventService.UpdateEventAsync(request);
                return Ok(eventDto);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Upload image for an event within a club
        /// </summary>
        [HttpPost("{id}/image")]
        [Consumes("multipart/form-data")]
        //[RequireEventPolicy("editevent")]
        public async Task<IActionResult> UploadEventImage(int clubId, int id, IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0) return BadRequest(new { error = "No image file provided." });

                var eventDto = await _eventService.GetEventByIdAsync(id);
                if (eventDto.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

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

        /// <summary>
        /// Create a session for an event within a club
        /// </summary>
        [HttpPost("{id}/sessions")]
        //[RequireEventPolicy("managesession")]
        public async Task<ActionResult<SessionDto>> CreateSession(int clubId, int id, [FromBody] CreateSessionRequest request)
        {
            try
            {
                if (id != request.EventId) return BadRequest(new { error = "Event ID mismatch" });

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                var sessionDto = await _eventService.CreateSessionAsync(request);
                return Ok(sessionDto);
            }
            catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { error = msg });
            }
        }

        /// <summary>
        /// Update an existing session for an event within a club
        /// </summary>
        [HttpPut("{id}/sessions/{scheduleId}")]
        //[RequireEventPolicy("managesession")]
        public async Task<ActionResult<SessionDto>> UpdateSession(int clubId, int id, int scheduleId, [FromBody] UpdateSessionRequest request)
        {
            try
            {
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                request.ScheduleId = scheduleId;
                request.EventId    = id;
                var sessionDto = await _eventService.UpdateSessionAsync(request);
                return Ok(sessionDto);
            }
            catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (DomainException    ex) { return BadRequest(new { error = ex.Message }); }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { error = msg });
            }
        }

        /// <summary>
        /// Delete a session for an event within a club
        /// </summary>
        [HttpDelete("{id}/sessions/{scheduleId}")]
        //[RequireEventPolicy("managesession")]
        public async Task<IActionResult> DeleteSession(int clubId, int id, int scheduleId)
        {
            try
            {
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                await _eventService.DeleteSessionAsync(scheduleId, id);
                return NoContent();
            }
            catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { error = msg });
            }
        }


        /// <summary>
        /// Open registration for an event within a club
        /// </summary>
        [HttpPatch("{id}/open-registration")]
        //[RequireEventPolicy("openregistration")]
        public async Task<ActionResult<EventDetailDto>> OpenRegistration(int clubId, int id, [FromBody] OpenRegistrationRequest request)
        {
            try
            {
                if (id != request.EventId) return BadRequest(new { error = "Event ID mismatch" });

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                var eventDto = await _eventService.OpenRegistrationAsync(request);
                return Ok(eventDto);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Start an event (generates check-in code)
        /// </summary>
        [HttpPut("{id}/start")]
        //[RequireEventPolicy("startevent")]
        public async Task<IActionResult> StartEvent(int clubId, int id)
        {
            try
            {
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                var result = await _eventService.StartEventAsync(id);
                return Ok(new { checkInCode = result.checkInCode, expiresAt = result.expiresAt });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Complete an event
        /// </summary>
        [HttpPut("{id}/complete")]
        //[RequireEventPolicy("completeevent")]
        public async Task<IActionResult> CompleteEvent(int clubId, int id)
        {
            try
            {
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                await _eventService.CompleteEventAsync(id);
                return Ok(new { message = "Event completed." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>
        /// Cancel an event — sets status to CANCELED, marks all registrations as CANCELLED
        /// </summary>
        [HttpPut("{id}/cancel")]
        //[RequireEventPolicy("editevent")]
        public async Task<IActionResult> CancelEvent(int clubId, int id)
        {
            try
            {
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                await _eventService.CancelEventAsync(id);
                return Ok(new { message = "Sự kiện đã được hủy." });
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>Get sessions of an event within a club</summary>
        [HttpGet("{id}/sessions")]
        [RequireEventPolicy("managesession")]
        public async Task<IActionResult> GetSessions(int clubId, int id)
        {
            try
            {
                var ev = await _eventService.GetEventByIdAsync(id);
                if (ev.ClubId != clubId)
                    return BadRequest(new { error = "Event does not belong to this club." });

                return Ok(ev.Sessions ?? new List<SessionDto>());
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }
    }
}
