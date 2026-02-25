using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    public class RoomsController : ControllerBase
    {
        private readonly IInterviewService _service;

        public RoomsController(IInterviewService service)
        {
            _service = service;
        }

        /// <summary>
        /// POST /api/rooms/{roomCode}/join – Join phòng
        /// </summary>
        [HttpPost("{roomCode}/join")]
        public async Task<IActionResult> JoinRoom(string roomCode, [FromBody] JoinRoomDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var result = await _service.JoinRoomAsync(roomCode, dto);
                return Ok(new { success = true, data = result });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "Room not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/rooms/{roomCode}/leave – Leave phòng
        /// </summary>
        [HttpPost("{roomCode}/leave")]
        public async Task<IActionResult> LeaveRoom(string roomCode, [FromBody] LeaveRoomDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            var ok = await _service.LeaveRoomAsync(roomCode, dto);
            if (!ok)
                return NotFound(new { success = false, message = "Participant not found in room" });

            return Ok(new { success = true, message = "Left room" });
        }

        /// <summary>
        /// GET /api/rooms/{roomCode}/participants – Danh sách participants
        /// </summary>
        [HttpGet("{roomCode}/participants")]
        public async Task<IActionResult> GetParticipants(string roomCode)
        {
            try
            {
                var participants = await _service.GetParticipantsAsync(roomCode);
                return Ok(new { success = true, data = participants });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "Room not found" });
            }
        }

        /// <summary>
        /// GET /api/rooms/{roomCode}/events – Event log
        /// </summary>
        [HttpGet("{roomCode}/events")]
        public async Task<IActionResult> GetEvents(string roomCode)
        {
            try
            {
                var events = await _service.GetEventsAsync(roomCode);
                return Ok(new { success = true, data = events });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "Room not found" });
            }
        }

        /// <summary>
        /// PATCH /api/rooms/{roomCode}/close – Đóng phòng
        /// </summary>
        [HttpPatch("{roomCode}/close")]
        public async Task<IActionResult> CloseRoom(string roomCode)
        {
            var success = await _service.CloseRoomAsync(roomCode);
            if (!success)
                return NotFound(new { success = false, message = "Room not found" });

            return Ok(new { success = true, message = "Room closed successfully" });
        }
    }
}
