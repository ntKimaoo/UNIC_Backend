using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

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
            // Allow Global Admins
            if (User.IsInRole("Admin")) return true;

            var clubRolesClaim = User.FindFirst("club_roles")?.Value;
            if (string.IsNullOrEmpty(clubRolesClaim))
                return false;

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var roles = JsonSerializer.Deserialize<List<ClubRoleClaimDto>>(clubRolesClaim, options);
                return roles != null && roles.Any(r => r.ClubId == clubId && (r.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase) || r.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase)));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Create a new event. Attach an 'image' file to upload it to Cloudinary —
        /// the URL will be saved automatically. Do NOT pass ImageUrl manually.
        /// </summary>
        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<EventDetailDto>> CreateEvent([FromForm] CreateEventRequest request, IFormFile? image)
        {
            try
            {
                if (request.ClubId.HasValue && !IsClubManager(request.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "You do not have Manager permissions for this club." });

                // Upload image to Cloudinary first (if provided), then pass URL to service
                string? imageUrl = null;
                if (image != null && image.Length > 0)
                {
                    imageUrl = await _fileStorageService.SaveFileAsync(image, "uniclub/events");
                }

                var eventDto = await _eventService.CreateEventAsync(request, imageUrl);
                return CreatedAtAction(nameof(GetEventById), new { id = eventDto.EventId }, eventDto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the event", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing event, with an optional new image uploaded to Cloudinary.
        /// Send as multipart/form-data with event fields + optional 'image' file.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<EventDetailDto>> UpdateEvent(int id, [FromForm] UpdateEventRequest request, IFormFile? image)
        {
            try
            {
                if (id != request.EventId)
                    return BadRequest(new { error = "Event ID in URL does not match request body" });

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId.HasValue && !IsClubManager(existingEvent.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "You do not have Manager permissions for this event's club." });

                // If an image was included, upload it and update ImageUrl
                if (image != null && image.Length > 0)
                {
                    request.ImageUrl = await _fileStorageService.SaveFileAsync(image, "uniclub/events");
                }

                var eventDto = await _eventService.UpdateEventAsync(request);
                return Ok(eventDto);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the event", details = ex.Message });
            }
        }

        /// <summary>
        /// Get event details by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<EventDetailDto>> GetEventById(int id)
        {
            try
            {
                var eventDto = await _eventService.GetEventByIdAsync(id);
                return Ok(eventDto);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the event", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all events with pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventDetailDto>>> GetAllEvents(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(new { error = "Invalid pagination parameters. PageNumber must be >= 1, PageSize must be between 1 and 100" });
                }

                var events = await _eventService.GetAllEventsAsync(pageNumber, pageSize);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving events", details = ex.Message });
            }
        }

        /// <summary>
        /// Standalone image (re)upload for an event — useful to replace the image without editing other fields.
        /// Uploads to Cloudinary and updates Event.ImageUrl immediately.
        /// </summary>
        [HttpPost("{id}/image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadEventImage(int id, IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                    return BadRequest(new { error = "No image file provided." });

                // Get event (ensure it exists)
                var eventDto = await _eventService.GetEventByIdAsync(id);
                if (eventDto == null)
                    return NotFound(new { error = $"Event {id} not found." });

                // Upload to Cloudinary
                var imageUrl = await _fileStorageService.SaveFileAsync(image, "uniclub/events");

                // Persist via UpdateEvent (sends imageUrl only by patching via UpdateEventRequest)
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

                return Ok(new
                {
                    message = "Image uploaded successfully.",
                    eventId = id,
                    imageUrl
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while uploading the image.", details = ex.Message });
            }
        }

        /// <summary>
        /// Add a session to an event
        /// </summary>
        [HttpPost("{id}/sessions")]
        [Authorize]
        public async Task<ActionResult<SessionDto>> CreateSession(int id, [FromBody] CreateSessionRequest request)
        {
            try
            {
                if (id != request.EventId)
                {
                    return BadRequest(new { error = "Event ID in URL does not match request body" });
                }

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId.HasValue && !IsClubManager(existingEvent.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "You do not have Manager permissions for this event's club." });

                var sessionDto = await _eventService.CreateSessionAsync(request);
                return CreatedAtAction(nameof(GetEventById), new { id = request.EventId }, sessionDto);
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
                return StatusCode(500, new { error = "An error occurred while creating the session", details = ex.Message });
            }
        }

        /// <summary>
        /// Open registration for an event
        /// </summary>
        [HttpPatch("{id}/open-registration")]
        [Authorize]
        public async Task<ActionResult<EventDetailDto>> OpenRegistration(int id, [FromBody] OpenRegistrationRequest request)
        {
            try
            {
                if (id != request.EventId)
                {
                    return BadRequest(new { error = "Event ID in URL does not match request body" });
                }

                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId.HasValue && !IsClubManager(existingEvent.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "You do not have Manager permissions for this event's club." });

                var eventDto = await _eventService.OpenRegistrationAsync(request);
                return Ok(eventDto);
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
                return StatusCode(500, new { error = "An error occurred while opening registration", details = ex.Message });
            }
        }

        /// <summary>
        /// Get QR code image for check-in token (used in email; no auth required so email clients can load the image).
        /// </summary>
        [HttpGet("qr/{token}")]
        [AllowAnonymous]
        public IActionResult GetQrCodeImage(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest();
            var pngBytes = _qrCodeGeneratorService.GetQrCodePngBytes(token);
            if (pngBytes == null || pngBytes.Length == 0)
                return NotFound();
            return File(pngBytes, "image/png");
        }

        /// <summary>
        /// Register a user for an event
        /// </summary>
        [HttpPost("{id}/register")]
        [Authorize]
        public async Task<IActionResult> RegisterEvent(int id, [FromBody] int? _dummy = null)
        {
            try
            {
                // In a real application, userId comes from Claims (User.FindFirstValue(ClaimTypes.NameIdentifier))
                // For this demo/test purpose, if no request body specifies userId and Auth is missing,
                // we'll assume the frontend passes userId in some way, but here according to our eventApi.ts:
                // it calls /events/{eventId}/register without body. We would pull from User claims here.

                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdString))
                {
                    // Fallback for testing if authorization is not fully hooked up in swagger/local yet
                    // Let's assume we read from a custom header or fake it, but we should return Unauthorized.
                    return Unauthorized(new { error = "User is not logged in." });
                }

                var apiBaseUrl = $"{Request.Scheme}://{Request.Host}";
                await _eventService.RegisterForEventAsync(id, userIdString, apiBaseUrl);
                return Ok(new { message = "Registration successful. Please check your email." });
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
                return StatusCode(500, new { error = "An error occurred during registration", details = ex.Message });
            }
        }

        /// <summary>
        /// Start an event and generate check-in code
        /// </summary>
        [HttpPut("{id}/start")]
        [Authorize]
        public async Task<IActionResult> StartEvent(int id)
        {
            try
            {
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId.HasValue && !IsClubManager(existingEvent.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "You do not have Manager permissions for this event's club." });

                var result = await _eventService.StartEventAsync(id);
                return Ok(new { checkInCode = result.checkInCode, expiresAt = result.expiresAt });
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
                return StatusCode(500, new { error = "An error occurred while starting the event", details = ex.Message });
            }
        }

        public class CheckInRequestDto
        {
            public int EventId { get; set; }
            public string CheckInCode { get; set; }
        }

        /// <summary>
        /// Check-in a user to an event
        /// </summary>
        [HttpPost("checkin")]
        public async Task<IActionResult> CheckInEvent([FromBody] CheckInRequestDto request)
        {
            try
            {
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized(new { error = "User is not logged in." });
                }

                await _eventService.CheckInEventAsync(request.EventId, userIdString, request.CheckInCode);
                return Ok(new { message = "Check-in successful." });
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
                return StatusCode(500, new { error = "An error occurred during check-in", details = ex.Message });
            }
        }

        /// <summary>
        /// Complete an event
        /// </summary>
        [HttpPut("{id}/complete")]
        [Authorize]
        public async Task<IActionResult> CompleteEvent(int id)
        {
            try
            {
                var existingEvent = await _eventService.GetEventByIdAsync(id);
                if (existingEvent.ClubId.HasValue && !IsClubManager(existingEvent.ClubId.Value))
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "You do not have Manager permissions for this event's club." });

                await _eventService.CompleteEventAsync(id);
                return Ok(new { message = "Event completed." });
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
                return StatusCode(500, new { error = "An error occurred while completing the event", details = ex.Message });
            }
        }
    }
}
