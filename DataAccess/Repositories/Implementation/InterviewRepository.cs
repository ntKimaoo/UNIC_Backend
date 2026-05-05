using DataAccess.Context;
using DataAccess.Models.Meeting;
using DataAccess.Models.Meeting.Enums;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Implementation
{
    public class InterviewRepository : IInterviewRepository
    {
        private readonly MeetingDbContext _context;

        public InterviewRepository(MeetingDbContext context)
        {
            _context = context;
        }

        // ═══════════════════════════════════════════════════════════
        //  InterviewSchedule
        // ═══════════════════════════════════════════════════════════

        public async Task<InterviewSchedule?> GetScheduleByIdAsync(int id)
        {
            return await _context.InterviewSchedules
                .Include(s => s.Assignments)
                    .ThenInclude(a => a.CriteriaScores)
                        .ThenInclude(cs => cs.EvaluationCriterion)
                .Include(s => s.MeetingRoom)
                .Include(s => s.ProposedTimeSlots)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<InterviewSchedule>> GetSchedulesAsync(
            int? campaignId, string? status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.InterviewSchedules
                .Include(s => s.Assignments)
                    .ThenInclude(a => a.CriteriaScores)
                        .ThenInclude(cs => cs.EvaluationCriterion)
                .Include(s => s.MeetingRoom)
                .Include(s => s.ProposedTimeSlots)
                .AsQueryable();

            if (campaignId.HasValue)
                query = query.Where(s => s.CampaignId == campaignId.Value);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<InterviewStatus>(status, true, out var parsed))
                query = query.Where(s => s.Status == parsed);

            if (fromDate.HasValue)
                query = query.Where(s => s.ScheduledAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(s => s.ScheduledAt <= toDate.Value);

            return await query
                .OrderByDescending(s => s.ScheduledAt)
                .ToListAsync();
        }

        public async Task<InterviewSchedule> CreateScheduleAsync(InterviewSchedule schedule)
        {
            await _context.InterviewSchedules.AddAsync(schedule);
            await _context.SaveChangesAsync();
            return schedule;
        }

        public async Task<bool> UpdateScheduleAsync(InterviewSchedule schedule)
        {
            try
            {
                _context.InterviewSchedules.Update(schedule);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteScheduleAsync(int id)
        {
            var schedule = await _context.InterviewSchedules.FindAsync(id);
            if (schedule == null) return false;

            _context.InterviewSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> AutoCompleteExpiredInterviewsAsync(int gracePeriodMinutes)
        {
            var now = DateTime.UtcNow.AddHours(7);
            var maxPossibleStartDate = now.AddMinutes(-gracePeriodMinutes);

            var candidates = await _context.InterviewSchedules
                .Include(s => s.MeetingRoom)
                .Where(s => (s.Status == InterviewStatus.InProgress || s.Status == InterviewStatus.Confirmed)
                            && s.ScheduledAt <= maxPossibleStartDate)
                .ToListAsync();

            var expiredSchedules = candidates
                .Where(s => s.ScheduledAt?.AddMinutes(s.DurationMinutes + gracePeriodMinutes) <= now)
                .ToList();

            if (!expiredSchedules.Any())
                return 0;

            foreach (var schedule in expiredSchedules)
            {
                schedule.Status = InterviewStatus.Completed;

                if (schedule.MeetingRoom != null && schedule.MeetingRoom.Status != RoomStatus.Closed)
                {
                    schedule.MeetingRoom.Status = RoomStatus.Closed;
                    schedule.MeetingRoom.EndedAt = now;
                }
            }

            await _context.SaveChangesAsync();
            return expiredSchedules.Count;
        }

        public async Task<int> AutoCancelUnconfirmedInterviewsAsync(TimeSpan maxWaitTime, TimeSpan minTimeBeforeEarliestSlot)
        {
            var now = DateTime.UtcNow.AddHours(7);

            var unconfirmedSchedules = await _context.InterviewSchedules
                .Include(s => s.ProposedTimeSlots)
                .Where(s => s.Status == InterviewStatus.Scheduled || s.Status == InterviewStatus.Rescheduled)
                .ToListAsync();

            var schedulesToCancel = new List<InterviewSchedule>();

            foreach (var schedule in unconfirmedSchedules)
            {
                bool shouldCancel = false;

                // Điều kiện 1: Đã quá thời gian chờ từ lúc tạo/cập nhật lịch (vd 1 ngày)
                var lastUpdated = schedule.UpdatedAt ?? schedule.CreatedAt ?? now;
                if (now - lastUpdated > maxWaitTime)
                {
                    shouldCancel = true;
                }
                else
                {
                    // Điều kiện 2: Gần đến thời gian của slot gần nhất (vd trước 5 tiếng)
                    DateTime? earliestProposedTime = null;
                    
                    if (schedule.ProposedTimeSlots != null && schedule.ProposedTimeSlots.Any())
                    {
                        earliestProposedTime = schedule.ProposedTimeSlots.Min(p => p.ProposedAt);
                    }
                    else if (schedule.ScheduledAt.HasValue)
                    {
                        earliestProposedTime = schedule.ScheduledAt.Value;
                    }

                    if (earliestProposedTime.HasValue && earliestProposedTime.Value <= now.Add(minTimeBeforeEarliestSlot))
                    {
                        shouldCancel = true;
                    }
                }

                if (shouldCancel)
                {
                    schedulesToCancel.Add(schedule);
                }
            }

            if (!schedulesToCancel.Any())
                return 0;

            foreach (var schedule in schedulesToCancel)
            {
                schedule.Status = InterviewStatus.Cancelled;
                schedule.CancelReason = "Hệ thống tự động hủy do ứng viên không xác nhận lịch phỏng vấn đúng hạn.";
                schedule.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            return schedulesToCancel.Count;
        }

        public async Task<IEnumerable<InterviewSchedule>> AutoRescheduleNoInterviewerAsync(TimeSpan hoursBeforeStart)
        {
            var now = DateTime.UtcNow.AddHours(7);
            var threshold = now.Add(hoursBeforeStart);

            var schedules = await _context.InterviewSchedules
                .Include(s => s.Assignments)
                .Where(s => s.Status == InterviewStatus.Confirmed
                            && s.ScheduledAt.HasValue
                            && s.ScheduledAt.Value > now
                            && s.ScheduledAt.Value <= threshold
                            && !s.Assignments.Any())
                .ToListAsync();

            if (!schedules.Any())
                return schedules;

            foreach (var s in schedules)
            {
                s.ScheduledAt = s.ScheduledAt!.Value.AddDays(1);
                s.Status = InterviewStatus.Rescheduled;
                s.RescheduleReason = "Hệ thống tự động dời lịch do chưa có phỏng vấn viên được phân công.";
                s.UpdatedAt = now;
                s.ReminderSentAt = null;
                s.RoomOpenedEmailSentAt = null;
            }

            await _context.SaveChangesAsync();
            return schedules;
        }

        public async Task<IEnumerable<InterviewSchedule>> GetSchedulesNeedingReminderAsync(TimeSpan reminderBefore)
        {
            var now = DateTime.UtcNow.AddHours(7);
            var reminderThreshold = now.Add(reminderBefore);

            return await _context.InterviewSchedules
                .Include(s => s.Assignments)
                .Include(s => s.MeetingRoom)
                .Where(s => (s.Status == InterviewStatus.Scheduled || s.Status == InterviewStatus.Confirmed)
                            && s.ReminderSentAt == null
                            && s.ScheduledAt <= reminderThreshold
                            && s.ScheduledAt > now)
                .ToListAsync();
        }

        public async Task<IEnumerable<InterviewSchedule>> GetCompletedSchedulesWithPendingFeedbackAsync()
        {
            return await _context.InterviewSchedules
                .Include(s => s.Assignments)
                .Where(s => s.Status == InterviewStatus.Completed
                            && s.FeedbackNudgeSentAt == null
                            && s.Assignments.Any(a => a.FeedbackSubmittedAt == null))
                .ToListAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  InterviewAssignment
        // ═══════════════════════════════════════════════════════════

        public async Task<InterviewAssignment?> GetAssignmentByIdAsync(int id)
        {
            return await _context.InterviewAssignments
                .Include(a => a.CriteriaScores)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<InterviewAssignment>> GetAssignmentsByScheduleIdAsync(int scheduleId)
        {
            return await _context.InterviewAssignments
                .Include(a => a.CriteriaScores)
                .Where(a => a.InterviewScheduleId == scheduleId)
                .OrderBy(a => a.AssignedAt)
                .ToListAsync();
        }

        public async Task<InterviewAssignment> CreateAssignmentAsync(InterviewAssignment assignment)
        {
            await _context.InterviewAssignments.AddAsync(assignment);
            await _context.SaveChangesAsync();
            return assignment;
        }

        public async Task<bool> UpdateAssignmentAsync(InterviewAssignment assignment)
        {
            try
            {
                _context.InterviewAssignments.Update(assignment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteAssignmentAsync(int id)
        {
            var assignment = await _context.InterviewAssignments.FindAsync(id);
            if (assignment == null) return false;

            _context.InterviewAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
            return true;
        }

        // ═══════════════════════════════════════════════════════════
        //  MeetingRoom
        // ═══════════════════════════════════════════════════════════

        public async Task<MeetingRoom?> GetRoomByIdAsync(int roomId)
        {
            return await _context.MeetingRooms
                .Include(r => r.Participants)
                .Include(r => r.Events)
                .FirstOrDefaultAsync(r => r.Id == roomId);
        }

        public async Task<MeetingRoom?> GetRoomByScheduleIdAsync(int scheduleId)
        {
            return await _context.MeetingRooms
                .Include(r => r.Participants)
                .Include(r => r.Events)
                .FirstOrDefaultAsync(r => r.InterviewScheduleId == scheduleId);
        }

        public async Task<MeetingRoom?> GetRoomByCodeAsync(string roomCode)
        {
            return await _context.MeetingRooms
                .Include(r => r.Participants)
                .Include(r => r.Events)
                .FirstOrDefaultAsync(r => r.RoomCode == roomCode);
        }

        public async Task<MeetingRoom> CreateRoomAsync(MeetingRoom room)
        {
            await _context.MeetingRooms.AddAsync(room);
            await _context.SaveChangesAsync();
            return room;
        }

        public async Task<bool> UpdateRoomAsync(MeetingRoom room)
        {
            try
            {
                _context.MeetingRooms.Update(room);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        // ═══════════════════════════════════════════════════════════
        //  RoomParticipant
        // ═══════════════════════════════════════════════════════════

        public async Task<int> SyncRoomStatusesAsync(TimeSpan preOpenDuration)
        {
            var thresholdTime = DateTime.UtcNow.AddHours(7).Add(preOpenDuration);
            var roomsToActivate = await _context.MeetingRooms
                .Include(r => r.InterviewSchedule)
                    .ThenInclude(s => s!.Assignments)
                .Where(r => r.Status == RoomStatus.Idle
                            && r.RoomType == RoomType.Interview
                            && r.InterviewSchedule != null
                            && r.InterviewSchedule.Status == InterviewStatus.Confirmed
                            && r.InterviewSchedule.ScheduledAt <= thresholdTime
                            && r.InterviewSchedule.Assignments.Any())
                .ToListAsync();

            if (!roomsToActivate.Any())
                return 0;

            foreach (var room in roomsToActivate)
            {
                room.Status = RoomStatus.Active;
                if (room.InterviewSchedule != null)
                {
                    room.InterviewSchedule.Status = InterviewStatus.InProgress;
                    room.InterviewSchedule.UpdatedAt = DateTime.UtcNow.AddHours(7);
                }
            }

            await _context.SaveChangesAsync();
            return roomsToActivate.Count;
        }

        public async Task<RoomParticipant?> GetActiveParticipantAsync(int roomId, Guid userId)
        {
            return await _context.RoomParticipants
                .Where(p => p.MeetingRoomId == roomId
                         && p.UserId == userId
                         && p.ConnectionState == ParticipantConnectionState.Joined)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<RoomParticipant>> GetParticipantsByRoomIdAsync(int roomId)
        {
            return await _context.RoomParticipants
                .Where(p => p.MeetingRoomId == roomId)
                .OrderBy(p => p.JoinedAt)
                .ToListAsync();
        }

        public async Task<RoomParticipant> CreateParticipantAsync(RoomParticipant participant)
        {
            await _context.RoomParticipants.AddAsync(participant);
            await _context.SaveChangesAsync();
            return participant;
        }

        public async Task<bool> UpdateParticipantAsync(RoomParticipant participant)
        {
            try
            {
                _context.RoomParticipants.Update(participant);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        // ═══════════════════════════════════════════════════════════
        //  RoomEvent
        // ═══════════════════════════════════════════════════════════

        public async Task<IEnumerable<RoomEvent>> GetEventsByRoomIdAsync(int roomId)
        {
            return await _context.RoomEvents
                .Where(e => e.MeetingRoomId == roomId)
                .OrderBy(e => e.OccurredAt)
                .ToListAsync();
        }

        public async Task<RoomEvent> CreateEventAsync(RoomEvent roomEvent)
        {
            await _context.RoomEvents.AddAsync(roomEvent);
            await _context.SaveChangesAsync();
            return roomEvent;
        }

        // ═══════════════════════════════════════════════════════════
        //  EvaluationCriterion
        // ═══════════════════════════════════════════════════════════

        public async Task<IEnumerable<EvaluationCriterion>> GetCriteriaByCampaignIdAsync(int campaignId)
        {
            return await _context.EvaluationCriteria
                .Where(c => c.CampaignId == campaignId && !c.IsDraft)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<EvaluationCriterion>> GetCriteriaForAssignmentAsync(int campaignId, int assignmentId)
        {
            // Non-draft criteria explicitly assigned to this interviewer
            var assignedNonDraftIds = await _context.CriteriaScores
                .Where(s => s.InterviewAssignmentId == assignmentId && !s.EvaluationCriterion.IsDraft)
                .Select(s => s.EvaluationCriterionId)
                .Distinct()
                .ToListAsync();

            if (assignedNonDraftIds.Count == 0)
            {
                // No specific assignment → show ALL non-draft criteria + any drafts for this assignment
                return await _context.EvaluationCriteria
                    .Where(c => c.CampaignId == campaignId &&
                                (!c.IsDraft || c.CriteriaScores.Any(s => s.InterviewAssignmentId == assignmentId)))
                    .OrderBy(c => c.IsDraft).ThenBy(c => c.Name)
                    .ToListAsync();
            }

            // Specific assignment → return only those non-draft criteria + any drafts for this assignment
            return await _context.EvaluationCriteria
                .Where(c => c.CampaignId == campaignId &&
                            (assignedNonDraftIds.Contains(c.Id) ||
                             (c.IsDraft && c.CriteriaScores.Any(s => s.InterviewAssignmentId == assignmentId))))
                .OrderBy(c => c.IsDraft).ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<EvaluationCriterion?> GetCriterionByIdAsync(int id)
        {
            return await _context.EvaluationCriteria.FindAsync(id);
        }

        public async Task<EvaluationCriterion> CreateCriterionAsync(EvaluationCriterion criterion)
        {
            await _context.EvaluationCriteria.AddAsync(criterion);
            await _context.SaveChangesAsync();
            return criterion;
        }

        public async Task<bool> UpdateCriterionAsync(EvaluationCriterion criterion)
        {
            try
            {
                _context.EvaluationCriteria.Update(criterion);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteCriterionAsync(int id)
        {
            var criterion = await _context.EvaluationCriteria.FindAsync(id);
            if (criterion == null) return false;

            _context.EvaluationCriteria.Remove(criterion);
            await _context.SaveChangesAsync();
            return true;
        }

        // ═══════════════════════════════════════════════════════════
        //  CriteriaScore
        // ═══════════════════════════════════════════════════════════

        public async Task<CriteriaScore> CreateCriteriaScoreAsync(CriteriaScore score)
        {
            await _context.CriteriaScores.AddAsync(score);
            await _context.SaveChangesAsync();
            return score;
        }

        public async Task<CriteriaScore?> GetCriteriaScoreAsync(int assignmentId, int criterionId)
        {
            return await _context.CriteriaScores
                .FirstOrDefaultAsync(cs => cs.InterviewAssignmentId == assignmentId
                                        && cs.EvaluationCriterionId == criterionId);
        }

        public async Task<bool> UpdateCriteriaScoreAsync(CriteriaScore score)
        {
            try
            {
                _context.CriteriaScores.Update(score);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<IEnumerable<CriteriaScore>> GetCriteriaScoresByAssignmentIdAsync(int assignmentId)
        {
            return await _context.CriteriaScores
                .Include(cs => cs.EvaluationCriterion)
                .Where(cs => cs.InterviewAssignmentId == assignmentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<CriteriaScore>> GetCriteriaScoresByScheduleIdAsync(int scheduleId)
        {
            return await _context.CriteriaScores
                .Include(cs => cs.EvaluationCriterion)
                .Include(cs => cs.InterviewAssignment)
                .Where(cs => cs.InterviewAssignment.InterviewScheduleId == scheduleId)
                .ToListAsync();
        }

        public async Task DeleteCriteriaScoresByAssignmentIdAsync(int assignmentId)
        {
            // Chỉ xóa non-draft scores; draft scores được quản lý riêng
            var scores = await _context.CriteriaScores
                .Include(cs => cs.EvaluationCriterion)
                .Where(cs => cs.InterviewAssignmentId == assignmentId && !cs.EvaluationCriterion.IsDraft)
                .ToListAsync();

            if (scores.Any())
            {
                _context.CriteriaScores.RemoveRange(scores);
                await _context.SaveChangesAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  CampaignDecision
        // ═══════════════════════════════════════════════════════════

        public async Task<IEnumerable<CampaignDecision>> GetDecisionsByCampaignIdAsync(int campaignId)
        {
            return await _context.CampaignDecisions
                .Where(d => d.CampaignId == campaignId)
                .OrderBy(d => d.DecidedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<CampaignDecision>> GetScheduledDecisionsDueAsync(DateTime now)
        {
            return await _context.CampaignDecisions
                .Where(d => d.PublishStatus == PublishStatus.Scheduled
                            && d.ScheduledPublishAt.HasValue
                            && d.ScheduledPublishAt.Value <= now)
                .ToListAsync();
        }

        public async Task<CampaignDecision?> GetDecisionByScheduleIdAsync(int scheduleId)
        {
            return await _context.CampaignDecisions
                .FirstOrDefaultAsync(d => d.InterviewScheduleId == scheduleId);
        }

        public async Task<CampaignDecision> CreateDecisionAsync(CampaignDecision decision)
        {
            await _context.CampaignDecisions.AddAsync(decision);
            await _context.SaveChangesAsync();
            return decision;
        }

        public async Task<bool> UpdateDecisionAsync(CampaignDecision decision)
        {
            try
            {
                _context.CampaignDecisions.Update(decision);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        // ═══════════════════════════════════════════════════════════
        //  ProposedTimeSlot
        // ═══════════════════════════════════════════════════════════

        public async Task<IEnumerable<ProposedTimeSlot>> GetTimeSlotsByScheduleIdAsync(int scheduleId)
        {
            return await _context.ProposedTimeSlots
                .Where(t => t.InterviewScheduleId == scheduleId)
                .OrderBy(t => t.ProposedAt)
                .ToListAsync();
        }

        public async Task<ProposedTimeSlot?> GetTimeSlotByIdAsync(int id)
        {
            return await _context.ProposedTimeSlots.FindAsync(id);
        }

        public async Task<ProposedTimeSlot> CreateTimeSlotAsync(ProposedTimeSlot slot)
        {
            await _context.ProposedTimeSlots.AddAsync(slot);
            await _context.SaveChangesAsync();
            return slot;
        }

        public async Task<bool> UpdateTimeSlotAsync(ProposedTimeSlot slot)
        {
            try
            {
                _context.ProposedTimeSlots.Update(slot);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task DeleteTimeSlotsByScheduleIdAsync(int scheduleId)
        {
            var slots = await _context.ProposedTimeSlots
                .Where(t => t.InterviewScheduleId == scheduleId)
                .ToListAsync();

            if (slots.Any())
            {
                _context.ProposedTimeSlots.RemoveRange(slots);
                await _context.SaveChangesAsync();
            }
        }

        // ════════════════════════════════════════════════════════════
        //  AiAnalysisResult
        // ════════════════════════════════════════════════════════════

        public async Task<IEnumerable<AiCandidateAnalysisResult>> GetAiAnalysisResultsByCampaignIdAsync(int campaignId)
        {
            return await _context.AiCandidateAnalysisResults
                .Where(a => a.CampaignId == campaignId)
                .OrderByDescending(a => a.AnalyzedAt)
                .ToListAsync();
        }

        public async Task<AiCandidateAnalysisResult> CreateAiAnalysisResultAsync(AiCandidateAnalysisResult result)
        {
            await _context.AiCandidateAnalysisResults.AddAsync(result);
            await _context.SaveChangesAsync();
            return result;
        }

        public async Task<IEnumerable<AiCandidateAnalysisResult>> CreateAiAnalysisResultsAsync(IEnumerable<AiCandidateAnalysisResult> results)
        {
            await _context.AiCandidateAnalysisResults.AddRangeAsync(results);
            await _context.SaveChangesAsync();
            return results;
        }

        public async Task DeleteAiAnalysisResultsByCampaignIdAsync(int campaignId)
        {
            var existing = await _context.AiCandidateAnalysisResults
                .Where(r => r.CampaignId == campaignId)
                .ToListAsync();
            
            if (existing.Any())
            {
                _context.AiCandidateAnalysisResults.RemoveRange(existing);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<AiCandidateAnalysisResult?> GetAiAnalysisResultByScheduleIdAsync(int scheduleId)
        {
            return await _context.AiCandidateAnalysisResults
                .FirstOrDefaultAsync(r => r.InterviewScheduleId == scheduleId);
        }

        public async Task DeleteAiAnalysisResultsByScheduleIdAsync(int scheduleId)
        {
            var existing = await _context.AiCandidateAnalysisResults
                .Where(r => r.InterviewScheduleId == scheduleId)
                .ToListAsync();
            
            if (existing.Any())
            {
                _context.AiCandidateAnalysisResults.RemoveRange(existing);
                await _context.SaveChangesAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Auto-remove unconfirmed secondary interviewers
        // ═══════════════════════════════════════════════════════════

        public async Task<int> AutoRemoveUnconfirmedSecondaryInterviewersAsync(TimeSpan beforeStart)
        {
            var now = DateTime.UtcNow.AddHours(7);
            var threshold = now.Add(beforeStart);

            // Lấy các lịch Confirmed sắp diễn ra (trong khoảng beforeStart)
            var schedules = await _context.InterviewSchedules
                .Include(s => s.Assignments)
                .Where(s => s.Status == InterviewStatus.Confirmed
                            && s.ScheduledAt.HasValue
                            && s.ScheduledAt.Value > now
                            && s.ScheduledAt.Value <= threshold
                            && s.Assignments.Count > 1) // Chỉ xử lý khi có > 1 interviewer
                .ToListAsync();

            if (!schedules.Any())
                return 0;

            int removedCount = 0;

            foreach (var schedule in schedules)
            {
                var confirmedOrLead = schedule.Assignments
                    .Where(a => a.HasConfirmed || a.Role == InterviewerRole.Lead)
                    .ToList();

                // Chỉ xóa nếu sau khi xóa vẫn còn ít nhất 1 interviewer
                if (confirmedOrLead.Count == 0) continue;

                var toRemove = schedule.Assignments
                    .Where(a => !a.HasConfirmed && a.Role != InterviewerRole.Lead)
                    .ToList();

                if (!toRemove.Any()) continue;

                _context.InterviewAssignments.RemoveRange(toRemove);
                removedCount += toRemove.Count;
            }

            if (removedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return removedCount;
        }
    }
}

