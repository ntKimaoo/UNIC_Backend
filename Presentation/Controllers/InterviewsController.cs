using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models.Meeting.Enums;
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
        private readonly IAiAnalysisService _aiService;

        public InterviewsController(IInterviewService service, IAiAnalysisService aiService)
        {
            _service = service;
            _aiService = aiService;
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
            [FromQuery] int? campaignId,
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

        /// <summary>
        /// GET /api/interviews/{id}/time-slots – Lấy danh sách time slots đề xuất
        /// </summary>
        [HttpGet("{id}/time-slots")]
        public async Task<IActionResult> GetTimeSlots(int id)
        {
            try
            {
                var slots = await _service.GetTimeSlotsAsync(id);
                return Ok(new { success = true, data = slots });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/interviews/{id}/confirm-time-slot – Candidate chọn và xác nhận time slot
        /// </summary>
        [HttpPost("{id}/confirm-time-slot")]
        public async Task<IActionResult> ConfirmTimeSlot(int id, [FromBody] ConfirmTimeSlotDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var result = await _service.ConfirmTimeSlotAsync(id, dto);
                return Ok(new { success = true, message = "Time slot confirmed", data = result });
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


        /// <summary>
        /// GET /api/interviews/campaign/{campaignId}/criteria – Lấy bộ tiêu chí (auto-seed default nếu chưa có)
        /// </summary>
        [HttpGet("campaign/{campaignId}/criteria")]
        public async Task<IActionResult> GetCampaignCriteria(int campaignId)
        {
            var criteria = await _service.GetCampaignCriteriaAsync(campaignId);
            return Ok(new { success = true, data = criteria });
        }

        /// <summary>
        /// POST /api/interviews/campaign/{campaignId}/criteria – Thêm tiêu chí mới
        /// </summary>
        [HttpPost("campaign/{campaignId}/criteria")]
        public async Task<IActionResult> CreateCriterion(int campaignId, [FromBody] CreateEvaluationCriterionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            var result = await _service.CreateCriterionAsync(campaignId, dto);
            return Ok(new { success = true, message = "Criterion created", data = result });
        }

        /// <summary>
        /// PUT /api/interviews/criteria/{criterionId} – Sửa tiêu chí
        /// </summary>
        [HttpPut("criteria/{criterionId}")]
        public async Task<IActionResult> UpdateCriterion(int criterionId, [FromBody] UpdateEvaluationCriterionDto dto)
        {
            var result = await _service.UpdateCriterionAsync(criterionId, dto);
            if (result == null)
                return NotFound(new { success = false, message = "Criterion not found" });

            return Ok(new { success = true, message = "Criterion updated", data = result });
        }

        /// <summary>
        /// DELETE /api/interviews/criteria/{criterionId} – Xoá tiêu chí
        /// </summary>
        [HttpDelete("criteria/{criterionId}")]
        public async Task<IActionResult> DeleteCriterion(int criterionId)
        {
            var ok = await _service.DeleteCriterionAsync(criterionId);
            if (!ok)
                return NotFound(new { success = false, message = "Criterion not found" });

            return Ok(new { success = true, message = "Criterion deleted" });
        }

        /// <summary>
        /// PUT /api/interviews/{id}/assignments/{assignmentId}/criteria – Phân tiêu chí cho PV viên
        /// </summary>
        [HttpPut("{id}/assignments/{assignmentId}/criteria")]
        public async Task<IActionResult> AssignCriteria(int id, int assignmentId, [FromBody] AssignCriteriaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            var ok = await _service.AssignCriteriaToInterviewerAsync(id, assignmentId, dto);
            if (!ok)
                return NotFound(new { success = false, message = "Assignment not found" });

            return Ok(new { success = true, message = "Criteria assigned" });
        }


        /// <summary>
        /// POST /api/interviews/{id}/assignments/{assignmentId}/criteria-feedback – Gửi đánh giá theo tiêu chí
        /// </summary>
        [HttpPost("{id}/assignments/{assignmentId}/criteria-feedback")]
        public async Task<IActionResult> SubmitCriteriaFeedback(int id, int assignmentId, [FromBody] SubmitCriteriaFeedbackDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var ok = await _service.SubmitCriteriaFeedbackAsync(id, assignmentId, dto);
                if (!ok)
                    return NotFound(new { success = false, message = "Assignment not found" });

                return Ok(new { success = true, message = "Criteria feedback submitted" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/interviews/{id}/assignments/{assignmentId}/criteria-scores – Lấy danh sách điểm tiêu chí đã chấm
        /// </summary>
        [HttpGet("{id}/assignments/{assignmentId}/criteria-scores")]
        public async Task<IActionResult> GetCriteriaScores(int id, int assignmentId)
        {
            try
            {
                var scores = await _service.GetCriteriaScoresByAssignmentAsync(id, assignmentId);
                return Ok(new { success = true, data = scores });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "Assignment not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/interviews/{id}/evaluation-summary – Tổng hợp đánh giá theo tiêu chí
        /// </summary>
        [HttpGet("{id}/evaluation-summary")]
        public async Task<IActionResult> GetEvaluationSummary(int id)
        {
            var summary = await _service.GetEvaluationSummaryAsync(id);
            if (summary == null)
                return NotFound(new { success = false, message = "Interview schedule not found" });

            return Ok(new { success = true, data = summary });
        }

        /// <summary>
        /// GET /api/interviews/campaign/{campaignId}/comparison – So sánh ứng viên
        /// </summary>
        [HttpGet("campaign/{campaignId}/comparison")]
        public async Task<IActionResult> GetCampaignComparison(int campaignId)
        {
            var comparison = await _service.GetCampaignComparisonAsync(campaignId);
            return Ok(new { success = true, data = comparison });
        }


        /// <summary>
        /// POST /api/interviews/campaign/{campaignId}/decisions – Gửi quyết định Accept/Reject/Waitlist
        /// </summary>
        [HttpPost("campaign/{campaignId}/decisions")]
        public async Task<IActionResult> SubmitDecisions(int campaignId, [FromBody] SubmitDecisionsDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var result = await _service.SubmitDecisionsAsync(campaignId, dto);
                return Ok(new { success = true, message = "Decisions submitted", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/interviews/campaign/{campaignId}/publish – Công bố / lên lịch công bố kết quả
        /// </summary>
        [HttpPost("campaign/{campaignId}/publish")]
        public async Task<IActionResult> PublishResults(int campaignId, [FromBody] PublishResultDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var result = await _service.PublishResultsAsync(campaignId, dto);
                return Ok(new { success = true, message = "Results published", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/interviews/campaign/{campaignId}/publish-status – Trạng thái công bố
        /// </summary>
        [HttpGet("campaign/{campaignId}/publish-status")]
        public async Task<IActionResult> GetPublishStatus(int campaignId)
        {
            var status = await _service.GetPublishStatusAsync(campaignId);
            if (status == null)
                return NotFound(new { success = false, message = "No decisions found for this campaign" });

            return Ok(new { success = true, data = status });
        }

        /// <summary>
        /// GET /api/interviews/campaign/{campaignId}/ai-analysis – Phân tích AI toàn bộ ứng viên
        /// </summary>
        [HttpGet("campaign/{campaignId}/ai-analysis")]
        public async Task<IActionResult> GetAiAnalysis(int campaignId)
        {
            try
            {
                var result = await _aiService.AnalyzeCampaignCandidatesAsync(campaignId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/interviews/campaign/{campaignId}/ai-analysis/generate – Scan ứng viên chưa được phân tích, gọi AI & lưu DB
        /// </summary>
        [HttpPost("campaign/{campaignId}/ai-analysis/generate")]
        public async Task<IActionResult> GenerateAiAnalysis(int campaignId, [FromQuery] bool force = false)
        {
            try
            {
                var result = await _aiService.GenerateAiAnalysisAsync(campaignId, force);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/interviews/schedule/{scheduleId}/ai-analysis/generate – Scan 1 ứng viên, gọi AI & lưu DB
        /// </summary>
        [HttpPost("schedule/{scheduleId}/ai-analysis/generate")]
        public async Task<IActionResult> GenerateSingleAiAnalysis(int scheduleId, [FromQuery] bool force = false)
        {
            try
            {
                var result = await _aiService.GenerateSingleAiAnalysisAsync(scheduleId, force);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/interviews/campaign/{campaignId}/ai-search – Tìm kiếm ứng viên bằng AI
        /// </summary>
        [HttpPost("campaign/{campaignId}/ai-search")]
        public async Task<IActionResult> AiSearchCandidates(int campaignId, [FromBody] AiSearchRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var result = await _aiService.SearchCandidatesAsync(campaignId, dto);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
