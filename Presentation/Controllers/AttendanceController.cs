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
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
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

        [HttpPost("{id}/checkin-by-barcode")]
        public async Task<ActionResult<CheckInByBarcodeResponse>> CheckInByBarcode(int id, [FromBody] CheckInByBarcodeRequest request)
        {
            try
            {
                var response = await _attendanceService.CheckInByBarcodeAsync(id, request.Barcode);
                return Ok(response);
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
                return StatusCode(500, new { error = "An error occurred while checking in by barcode", details = ex.Message });
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
