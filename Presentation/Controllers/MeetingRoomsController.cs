using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    public class MeetingRoomsController : ControllerBase
    {
        private readonly IInterviewService _service;

        public MeetingRoomsController(IInterviewService service)
        {
            _service = service;
        }

        // ═══════════════════════════════════════════════════════════
        //  CRUD
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// POST /api/meeting-rooms – Tạo phòng họp mới (bất kỳ loại)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMeetingRoomDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var result = await _service.CreateStandaloneRoomAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, new
                {
                    success = true,
                    message = "Meeting room created successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/meeting-rooms/{id} – Lấy thông tin phòng theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var room = await _service.GetRoomByIdAsync(id);
            if (room == null)
                return NotFound(new { success = false, message = "Meeting room not found" });

            return Ok(new { success = true, data = room });
        }

        /// <summary>
        /// GET /api/meeting-rooms/code/{roomCode} – Lấy thông tin phòng theo RoomCode
        /// </summary>
        [HttpGet("code/{roomCode}")]
        public async Task<IActionResult> GetByCode(string roomCode)
        {
            var room = await _service.GetRoomByCodeAsync(roomCode);
            if (room == null)
                return NotFound(new { success = false, message = "Meeting room not found" });

            return Ok(new { success = true, data = room });
        }

        // ═══════════════════════════════════════════════════════════
        //  Join / Leave / Close
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// POST /api/meeting-rooms/{roomCode}/join – Tham gia phòng
        /// </summary>
        [HttpPost("{roomCode}/join")]
        public async Task<IActionResult> JoinRoom(string roomCode, [FromBody] JoinRoomDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var result = await _service.JoinRoomAsync(roomCode, dto);
                return Ok(new { success = true, message = "Joined room", data = result });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "Room not found" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/meeting-rooms/{roomCode}/leave – Rời phòng
        /// </summary>
        [HttpPost("{roomCode}/leave")]
        public async Task<IActionResult> LeaveRoom(string roomCode, [FromBody] LeaveRoomDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            var ok = await _service.LeaveRoomAsync(roomCode, dto);
            if (!ok)
                return NotFound(new { success = false, message = "Room or participant not found" });

            return Ok(new { success = true, message = "Left room" });
        }

        /// <summary>
        /// POST /api/meeting-rooms/{roomCode}/close – Đóng phòng
        /// </summary>
        [HttpPost("{roomCode}/close")]
        public async Task<IActionResult> CloseRoom(string roomCode)
        {
            var ok = await _service.CloseRoomAsync(roomCode);
            if (!ok)
                return NotFound(new { success = false, message = "Room not found" });

            return Ok(new { success = true, message = "Room closed" });
        }

        // ═══════════════════════════════════════════════════════════
        //  Participants & Events
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// GET /api/meeting-rooms/{roomCode}/participants – Danh sách participants
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
        /// GET /api/meeting-rooms/{roomCode}/events – Lịch sử sự kiện phòng
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
    }
}
