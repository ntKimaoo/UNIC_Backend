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
        Task<MeetingRoomResponseDto> CreateStandaloneRoomAsync(CreateMeetingRoomDto dto);
        Task<MeetingRoomResponseDto?> GetRoomByIdAsync(int roomId);
        Task<MeetingRoomResponseDto?> GetRoomByCodeAsync(string roomCode);
        Task<MeetingRoomResponseDto?> GetRoomByScheduleIdAsync(int scheduleId);
        Task<JoinRoomResponseDto> JoinRoomAsync(string roomCode, JoinRoomDto dto);
        Task<bool> LeaveRoomAsync(string roomCode, LeaveRoomDto dto);
        Task<IEnumerable<RoomParticipantResponseDto>> GetParticipantsAsync(string roomCode);
        Task<IEnumerable<RoomEventResponseDto>> GetEventsAsync(string roomCode);
        Task<bool> CloseRoomAsync(string roomCode);

        // ── Feedback ──────────────────────────────────────────────
        Task<bool> SubmitFeedbackAsync(int scheduleId, int assignmentId, SubmitFeedbackDto dto);
        Task<FeedbackSummaryResponseDto?> GetFeedbackSummaryAsync(int scheduleId);

        // ── Evaluation Criteria ──────────────────────────────────
        Task<List<EvaluationCriterionDto>> GetCampaignCriteriaAsync(int campaignId);
        Task<EvaluationCriterionDto> CreateCriterionAsync(int campaignId, CreateEvaluationCriterionDto dto);
        Task<EvaluationCriterionDto?> UpdateCriterionAsync(int criterionId, UpdateEvaluationCriterionDto dto);
        Task<bool> DeleteCriterionAsync(int criterionId);
        Task<bool> AssignCriteriaToInterviewerAsync(int scheduleId, int assignmentId, AssignCriteriaDto dto);

        // ── Criteria-based Feedback ──────────────────────────────
        Task<bool> SubmitCriteriaFeedbackAsync(int scheduleId, int assignmentId, SubmitCriteriaFeedbackDto dto);
        Task<EvaluationSummaryDto?> GetEvaluationSummaryAsync(int scheduleId);
        Task<List<CandidateComparisonItemDto>> GetCampaignComparisonAsync(int campaignId);

        // ── Decisions & Publish ──────────────────────────────────
        Task<List<CampaignDecisionResponseDto>> SubmitDecisionsAsync(int campaignId, SubmitDecisionsDto dto);
        Task<PublishStatusResponseDto> PublishResultsAsync(int campaignId, PublishResultDto dto);
        Task<PublishStatusResponseDto?> GetPublishStatusAsync(int campaignId);
    }
}

