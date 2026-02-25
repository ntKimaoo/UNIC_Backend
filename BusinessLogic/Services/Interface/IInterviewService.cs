using BusinessLogic.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IInterviewService
    {
        // ── Interview Schedule ────────────────────────────────────
        Task<InterviewScheduleResponseDto> CreateScheduleAsync(CreateInterviewScheduleDto dto);
        Task<IEnumerable<InterviewScheduleResponseDto>> GetSchedulesAsync(int? campaignId, string? status, DateTime? fromDate, DateTime? toDate);
        Task<InterviewScheduleResponseDto?> GetScheduleByIdAsync(int id);
        Task<InterviewScheduleResponseDto?> UpdateScheduleAsync(int id, UpdateInterviewScheduleDto dto);
        Task<bool> UpdateScheduleStatusAsync(int id, UpdateInterviewStatusDto dto);
        Task<bool> DeleteScheduleAsync(int id);

        // ── Interviewer Assignment ────────────────────────────────
        Task<List<InterviewAssignmentResponseDto>> AssignInterviewersAsync(int scheduleId, AssignInterviewersDto dto);
        Task<IEnumerable<InterviewAssignmentResponseDto>> GetAssignmentsAsync(int scheduleId);
        Task<bool> RemoveAssignmentAsync(int scheduleId, int assignmentId);
        Task<bool> ConfirmAssignmentAsync(int scheduleId, int assignmentId);

        // ── Meeting Room ──────────────────────────────────────────
        Task<MeetingRoomResponseDto?> GetRoomByScheduleIdAsync(int scheduleId);
        Task<JoinRoomResponseDto> JoinRoomAsync(string roomCode, JoinRoomDto dto);
        Task<bool> LeaveRoomAsync(string roomCode, LeaveRoomDto dto);
        Task<IEnumerable<RoomParticipantResponseDto>> GetParticipantsAsync(string roomCode);
        Task<IEnumerable<RoomEventResponseDto>> GetEventsAsync(string roomCode);
        Task<bool> CloseRoomAsync(string roomCode);

        // ── Feedback ──────────────────────────────────────────────
        Task<bool> SubmitFeedbackAsync(int scheduleId, int assignmentId, SubmitFeedbackDto dto);
        Task<FeedbackSummaryResponseDto?> GetFeedbackSummaryAsync(int scheduleId);
    }
}
