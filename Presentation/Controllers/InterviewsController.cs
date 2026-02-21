using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/interviews")]
    public class InterviewsController : ControllerBase
    {
        private readonly IInterviewService _service;

        public InterviewsController(IInterviewService service)
        {
            _service = service;
        }

        // ═══════════════════════════════════════════════════════════
        //  Interview Schedule CRUD
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// POST /api/interviews – Tạo lịch phỏng vấn từ ApplicationId
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInterviewScheduleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var result = await _service.CreateScheduleAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, new
                {
                    success = true,
                    message = "Interview schedule created successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/interviews – Danh sách lịch (filter theo campaignId, status, date)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? campaignId,
            [FromQuery] string? status,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var schedules = await _service.GetSchedulesAsync(campaignId, status, fromDate, toDate);
            return Ok(new { success = true, data = schedules });
        }

        /// <summary>
        /// GET /api/interviews/{id} – Chi tiết 1 lịch
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var schedule = await _service.GetScheduleByIdAsync(id);
            if (schedule == null)
                return NotFound(new { success = false, message = "Interview schedule not found" });

            return Ok(new { success = true, data = schedule });
        }

        /// <summary>
        /// PUT /api/interviews/{id} – Cập nhật thông tin lịch
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateInterviewScheduleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var result = await _service.UpdateScheduleAsync(id, dto);
                if (result == null)
                    return NotFound(new { success = false, message = "Interview schedule not found" });

                return Ok(new { success = true, message = "Updated successfully", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// PATCH /api/interviews/{id}/status – Đổi status (Confirm / Cancel / Reschedule)
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateInterviewStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var ok = await _service.UpdateScheduleStatusAsync(id, dto);
                if (!ok)
                    return NotFound(new { success = false, message = "Interview schedule not found" });

                return Ok(new { success = true, message = $"Status updated to {dto.Status}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// DELETE /api/interviews/{id} – Xoá lịch (chỉ khi còn Scheduled)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ok = await _service.DeleteScheduleAsync(id);
                if (!ok)
                    return NotFound(new { success = false, message = "Interview schedule not found" });

                return Ok(new { success = true, message = "Deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Interviewer Assignment
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// POST /api/interviews/{id}/assignments – Assign interviewer(s)
        /// </summary>
        [HttpPost("{id}/assignments")]
        public async Task<IActionResult> AssignInterviewers(int id, [FromBody] AssignInterviewersDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var result = await _service.AssignInterviewersAsync(id, dto);
                return Ok(new { success = true, message = "Interviewers assigned", data = result });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "Interview schedule not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/interviews/{id}/assignments – Danh sách interviewer
        /// </summary>
        [HttpGet("{id}/assignments")]
        public async Task<IActionResult> GetAssignments(int id)
        {
            var assignments = await _service.GetAssignmentsAsync(id);
            return Ok(new { success = true, data = assignments });
        }

        /// <summary>
        /// DELETE /api/interviews/{id}/assignments/{assignmentId} – Xoá 1 interviewer
        /// </summary>
        [HttpDelete("{id}/assignments/{assignmentId}")]
        public async Task<IActionResult> RemoveAssignment(int id, int assignmentId)
        {
            var ok = await _service.RemoveAssignmentAsync(id, assignmentId);
            if (!ok)
                return NotFound(new { success = false, message = "Assignment not found" });

            return Ok(new { success = true, message = "Assignment removed" });
        }

        /// <summary>
        /// PATCH /api/interviews/{id}/assignments/{assignmentId}/confirm – Xác nhận tham gia
        /// </summary>
        [HttpPatch("{id}/assignments/{assignmentId}/confirm")]
        public async Task<IActionResult> ConfirmAssignment(int id, int assignmentId)
        {
            var ok = await _service.ConfirmAssignmentAsync(id, assignmentId);
            if (!ok)
                return NotFound(new { success = false, message = "Assignment not found" });

            return Ok(new { success = true, message = "Confirmed" });
        }

        // ═══════════════════════════════════════════════════════════
        //  Meeting Room (by schedule)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// GET /api/interviews/{id}/room – Lấy thông tin phòng
        /// </summary>
        [HttpGet("{id}/room")]
        public async Task<IActionResult> GetRoom(int id)
        {
            var room = await _service.GetRoomByScheduleIdAsync(id);
            if (room == null)
                return NotFound(new { success = false, message = "Meeting room not found" });

            return Ok(new { success = true, data = room });
        }

        // ═══════════════════════════════════════════════════════════
        //  Feedback
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// POST /api/interviews/{id}/assignments/{assignmentId}/feedback – Submit feedback
        /// </summary>
        [HttpPost("{id}/assignments/{assignmentId}/feedback")]
        public async Task<IActionResult> SubmitFeedback(int id, int assignmentId, [FromBody] SubmitFeedbackDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var ok = await _service.SubmitFeedbackAsync(id, assignmentId, dto);
                if (!ok)
                    return NotFound(new { success = false, message = "Assignment not found" });

                return Ok(new { success = true, message = "Feedback submitted" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/interviews/{id}/feedback – Tổng hợp feedback
        /// </summary>
        [HttpGet("{id}/feedback")]
        public async Task<IActionResult> GetFeedbackSummary(int id)
        {
            var summary = await _service.GetFeedbackSummaryAsync(id);
            if (summary == null)
                return NotFound(new { success = false, message = "Interview schedule not found" });

            return Ok(new { success = true, data = summary });
        }
    }
}
