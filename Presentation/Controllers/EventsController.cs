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

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        /// <summary>
        /// Create a new event
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<EventDetailDto>> CreateEvent([FromBody] CreateEventRequest request)
        {
            try
            {
                var eventDto = await _eventService.CreateEventAsync(request);
                return CreatedAtAction(nameof(GetEventById), new { id = eventDto.EventId }, eventDto);
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
        /// Update an existing event
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<EventDetailDto>> UpdateEvent(int id, [FromBody] UpdateEventRequest request)
        {
            try
            {
                if (id != request.EventId)
                {
                    return BadRequest(new { error = "Event ID in URL does not match request body" });
                }

                var eventDto = await _eventService.UpdateEventAsync(request);
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
