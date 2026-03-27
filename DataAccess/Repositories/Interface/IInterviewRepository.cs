using DataAccess.Models.Meeting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Interface
{
    public interface IInterviewRepository
    {
        // ── InterviewSchedule ─────────────────────────────────────
        Task<InterviewSchedule?> GetScheduleByIdAsync(int id);
        Task<IEnumerable<InterviewSchedule>> GetSchedulesAsync(int? campaignId, string? status, DateTime? fromDate, DateTime? toDate);
        Task<InterviewSchedule> CreateScheduleAsync(InterviewSchedule schedule);
        Task<bool> UpdateScheduleAsync(InterviewSchedule schedule);
        Task<bool> DeleteScheduleAsync(int id);

        // ── InterviewAssignment ───────────────────────────────────
        Task<InterviewAssignment?> GetAssignmentByIdAsync(int id);
        Task<IEnumerable<InterviewAssignment>> GetAssignmentsByScheduleIdAsync(int scheduleId);
        Task<InterviewAssignment> CreateAssignmentAsync(InterviewAssignment assignment);
        Task<bool> UpdateAssignmentAsync(InterviewAssignment assignment);
        Task<bool> DeleteAssignmentAsync(int id);

        // ── MeetingRoom ───────────────────────────────────────────
        Task<MeetingRoom?> GetRoomByScheduleIdAsync(int scheduleId);
        Task<MeetingRoom?> GetRoomByCodeAsync(string roomCode);
        Task<MeetingRoom> CreateRoomAsync(MeetingRoom room);
        Task<bool> UpdateRoomAsync(MeetingRoom room);

        // ── RoomParticipant ───────────────────────────────────────
        Task<RoomParticipant?> GetActiveParticipantAsync(int roomId, Guid userId);
        Task<IEnumerable<RoomParticipant>> GetParticipantsByRoomIdAsync(int roomId);
        Task<RoomParticipant> CreateParticipantAsync(RoomParticipant participant);
        Task<bool> UpdateParticipantAsync(RoomParticipant participant);

        // ── RoomEvent ─────────────────────────────────────────────
        Task<IEnumerable<RoomEvent>> GetEventsByRoomIdAsync(int roomId);
        Task<RoomEvent> CreateEventAsync(RoomEvent roomEvent);

        // ── EvaluationCriterion ──────────────────────────────────
        Task<IEnumerable<EvaluationCriterion>> GetCriteriaByCampaignIdAsync(int campaignId);
        Task<EvaluationCriterion?> GetCriterionByIdAsync(int id);
        Task<EvaluationCriterion> CreateCriterionAsync(EvaluationCriterion criterion);
        Task<bool> UpdateCriterionAsync(EvaluationCriterion criterion);
        Task<bool> DeleteCriterionAsync(int id);

        // ── CriteriaScore ────────────────────────────────────────
        Task<CriteriaScore> CreateCriteriaScoreAsync(CriteriaScore score);
        Task<IEnumerable<CriteriaScore>> GetCriteriaScoresByAssignmentIdAsync(int assignmentId);
        Task<IEnumerable<CriteriaScore>> GetCriteriaScoresByScheduleIdAsync(int scheduleId);

        // ── CampaignDecision ─────────────────────────────────────
        Task<IEnumerable<CampaignDecision>> GetDecisionsByCampaignIdAsync(int campaignId);
        Task<CampaignDecision?> GetDecisionByScheduleIdAsync(int scheduleId);
        Task<CampaignDecision> CreateDecisionAsync(CampaignDecision decision);
        Task<bool> UpdateDecisionAsync(CampaignDecision decision);
    }
}

