using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UNIC.Presentation.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IFileStorageService _fileStorageService;

        public EventsController(IEventService eventService, IFileStorageService fileStorageService)
        {
            _eventService = eventService;
            _fileStorageService = fileStorageService;
        }

        /// <summary>
        /// Create a new event. Attach an 'image' file to upload it to Cloudinary —
        /// the URL will be saved automatically. Do NOT pass ImageUrl manually.
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<EventDetailDto>> CreateEvent([FromForm] CreateEventRequest request, IFormFile? image)
        {
            try
            {
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
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<EventDetailDto>> UpdateEvent(int id, [FromForm] UpdateEventRequest request, IFormFile? image)
        {
            try
            {
                if (id != request.EventId)
                    return BadRequest(new { error = "Event ID in URL does not match request body" });

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
        public async Task<ActionResult<SessionDto>> CreateSession(int id, [FromBody] CreateSessionRequest request)
        {
            try
            {
                if (id != request.EventId)
                {
                    return BadRequest(new { error = "Event ID in URL does not match request body" });
                }

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
        public async Task<ActionResult<EventDetailDto>> OpenRegistration(int id, [FromBody] OpenRegistrationRequest request)
        {
            try
            {
                if (id != request.EventId)
                {
                    return BadRequest(new { error = "Event ID in URL does not match request body" });
                }

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
    }
}
