using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models.Meeting;
using DataAccess.Models.Meeting.Enums;
using DataAccess.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementation
{
    public class InterviewService : IInterviewService
    {
        private readonly IInterviewRepository _repo;

        public InterviewService(IInterviewRepository repo)
        {
            _repo = repo;
        }

        // ═══════════════════════════════════════════════════════════
        //  Interview Schedule
        // ═══════════════════════════════════════════════════════════

        public async Task<InterviewScheduleResponseDto> CreateScheduleAsync(CreateInterviewScheduleDto dto)
        {
            var schedule = new InterviewSchedule
            {
                ApplicationId   = dto.ApplicationId,
                CandidateUserId = dto.CandidateUserId,
                CampaignId      = dto.CampaignId,
                CreatedByUserId = dto.CreatedByUserId,
                Title           = dto.Title,
                Description     = dto.Description,
                ScheduledAt     = dto.ScheduledAt,
                DurationMinutes = dto.DurationMinutes,
                Status          = InterviewStatus.Scheduled,
                CreatedAt       = DateTime.UtcNow
            };

            var created = await _repo.CreateScheduleAsync(schedule);

            // Auto-create MeetingRoom
            var room = new MeetingRoom
            {
                InterviewScheduleId = created.Id,
                RoomCode            = GenerateRoomCode(),
                Status              = RoomStatus.Idle,
                CreatedAt           = DateTime.UtcNow
            };
            await _repo.CreateRoomAsync(room);

            // Assign interviewers nếu có
            if (dto.Interviewers != null && dto.Interviewers.Count > 0)
            {
                foreach (var item in dto.Interviewers)
                {
                    if (!Enum.TryParse<InterviewerRole>(item.Role, true, out var role))
                        role = InterviewerRole.Interviewer;

                    var assignment = new InterviewAssignment
                    {
                        InterviewScheduleId = created.Id,
                        InterviewerUserId   = item.InterviewerUserId,
                        Role                = role,
                        AssignedAt          = DateTime.UtcNow
                    };
                    await _repo.CreateAssignmentAsync(assignment);
                }
            }

            // Reload with navigation
            var full = await _repo.GetScheduleByIdAsync(created.Id);
            return MapScheduleToDto(full!);
        }

        public async Task<IEnumerable<InterviewScheduleResponseDto>> GetSchedulesAsync(
            int? campaignId, string? status, DateTime? fromDate, DateTime? toDate)
        {
            var schedules = await _repo.GetSchedulesAsync(campaignId, status, fromDate, toDate);
            return schedules.Select(MapScheduleToDto);
        }

        public async Task<InterviewScheduleResponseDto?> GetScheduleByIdAsync(int id)
        {
            var schedule = await _repo.GetScheduleByIdAsync(id);
            return schedule == null ? null : MapScheduleToDto(schedule);
        }

        public async Task<InterviewScheduleResponseDto?> UpdateScheduleAsync(int id, UpdateInterviewScheduleDto dto)
        {
            var schedule = await _repo.GetScheduleByIdAsync(id);
            if (schedule == null) return null;

            if (!string.IsNullOrEmpty(dto.Title))
                schedule.Title = dto.Title;
            if (dto.Description != null)
                schedule.Description = dto.Description;
            if (dto.ScheduledAt.HasValue)
                schedule.ScheduledAt = dto.ScheduledAt.Value;
            if (dto.DurationMinutes.HasValue)
                schedule.DurationMinutes = dto.DurationMinutes.Value;

            schedule.UpdatedAt = DateTime.UtcNow;

            var ok = await _repo.UpdateScheduleAsync(schedule);
            if (!ok) return null;

            return MapScheduleToDto(schedule);
        }

        public async Task<bool> UpdateScheduleStatusAsync(int id, UpdateInterviewStatusDto dto)
        {
            var schedule = await _repo.GetScheduleByIdAsync(id);
            if (schedule == null) return false;

            if (!Enum.TryParse<InterviewStatus>(dto.Status, true, out var newStatus))
                throw new ArgumentException($"Invalid status: {dto.Status}");

            // Validate transitions
            switch (newStatus)
            {
                case InterviewStatus.Confirmed:
                    if (schedule.Status != InterviewStatus.Scheduled && schedule.Status != InterviewStatus.Rescheduled)
                        throw new InvalidOperationException("Chỉ có thể Confirm từ Scheduled hoặc Rescheduled.");
                    break;

                case InterviewStatus.InProgress:
                    if (schedule.Status != InterviewStatus.Confirmed && schedule.Status != InterviewStatus.Rescheduled)
                        throw new InvalidOperationException("Chỉ có thể bắt đầu phỏng vấn khi đã Confirm hoặc Reschedule.");
                    break;

                case InterviewStatus.Completed:
                    if (schedule.Status != InterviewStatus.InProgress)
                        throw new InvalidOperationException("Chỉ có thể hoàn thành khi đang InProgress.");
                    break;

                case InterviewStatus.Cancelled:
                    if (schedule.Status == InterviewStatus.Completed || schedule.Status == InterviewStatus.Cancelled)
                        throw new InvalidOperationException("Không thể Cancel lịch đã Completed hoặc Cancelled.");
                    if (string.IsNullOrEmpty(dto.CancelReason))
                        throw new ArgumentException("CancelReason là bắt buộc khi Cancel.");
                    schedule.CancelReason = dto.CancelReason;
                    break;

                case InterviewStatus.Rescheduled:
                    if (schedule.Status == InterviewStatus.Completed || schedule.Status == InterviewStatus.Cancelled)
                        throw new InvalidOperationException("Không thể Reschedule lịch đã Completed hoặc Cancelled.");
                    break;

                default:
                    throw new ArgumentException($"Không hỗ trợ chuyển trạng thái sang: {dto.Status}");
            }

            schedule.Status = newStatus;
            schedule.UpdatedAt = DateTime.UtcNow;

            return await _repo.UpdateScheduleAsync(schedule);
        }

        public async Task<bool> DeleteScheduleAsync(int id)
        {
            var schedule = await _repo.GetScheduleByIdAsync(id);
            if (schedule == null) return false;

            if (schedule.Status != InterviewStatus.Scheduled)
                throw new InvalidOperationException("Chỉ có thể xoá lịch ở trạng thái Scheduled.");

            return await _repo.DeleteScheduleAsync(id);
        }

        // ═══════════════════════════════════════════════════════════
        //  Interviewer Assignment
        // ═══════════════════════════════════════════════════════════

        public async Task<List<InterviewAssignmentResponseDto>> AssignInterviewersAsync(int scheduleId, AssignInterviewersDto dto)
        {
            var schedule = await _repo.GetScheduleByIdAsync(scheduleId);
            if (schedule == null)
                throw new KeyNotFoundException("Interview schedule not found.");

            var result = new List<InterviewAssignmentResponseDto>();

            foreach (var item in dto.Interviewers)
            {
                if (!Enum.TryParse<InterviewerRole>(item.Role, true, out var role))
                    role = InterviewerRole.Interviewer;

                var assignment = new InterviewAssignment
                {
                    InterviewScheduleId = scheduleId,
                    InterviewerUserId   = item.InterviewerUserId,
                    Role                = role,
                    AssignedAt          = DateTime.UtcNow
                };

                var created = await _repo.CreateAssignmentAsync(assignment);
                result.Add(MapAssignmentToDto(created));
            }

            return result;
        }

        public async Task<IEnumerable<InterviewAssignmentResponseDto>> GetAssignmentsAsync(int scheduleId)
        {
            var assignments = await _repo.GetAssignmentsByScheduleIdAsync(scheduleId);
            return assignments.Select(MapAssignmentToDto);
        }

        public async Task<bool> RemoveAssignmentAsync(int scheduleId, int assignmentId)
        {
            var assignment = await _repo.GetAssignmentByIdAsync(assignmentId);
            if (assignment == null || assignment.InterviewScheduleId != scheduleId)
                return false;

            return await _repo.DeleteAssignmentAsync(assignmentId);
        }

        public async Task<bool> ConfirmAssignmentAsync(int scheduleId, int assignmentId)
        {
            var assignment = await _repo.GetAssignmentByIdAsync(assignmentId);
            if (assignment == null || assignment.InterviewScheduleId != scheduleId)
                return false;

            assignment.HasConfirmed = true;
            return await _repo.UpdateAssignmentAsync(assignment);
        }

        // ═══════════════════════════════════════════════════════════
        //  Meeting Room
        // ═══════════════════════════════════════════════════════════

        public async Task<MeetingRoomResponseDto?> GetRoomByScheduleIdAsync(int scheduleId)
        {
            var room = await _repo.GetRoomByScheduleIdAsync(scheduleId);
            return room == null ? null : MapRoomToDto(room);
        }

        public async Task<JoinRoomResponseDto> JoinRoomAsync(string roomCode, JoinRoomDto dto)
        {
            var room = await _repo.GetRoomByCodeAsync(roomCode);
            if (room == null)
                throw new KeyNotFoundException("Room not found.");

            if (room.Status == RoomStatus.Closed)
                throw new InvalidOperationException("Room đã đóng.");

            // Kiểm tra max participants
            var activeCount = room.Participants
                .Count(p => p.ConnectionState == ParticipantConnectionState.Joined);
            if (activeCount >= room.MaxParticipants)
                throw new InvalidOperationException("Room đã đầy.");

            // Tạo participant
            var peerId = Guid.NewGuid().ToString("N")[..12];
            var participant = new RoomParticipant
            {
                MeetingRoomId   = room.Id,
                UserId          = dto.UserId,
                DisplayName     = dto.DisplayName,
                Role            = dto.Role,
                PeerId          = peerId,
                ConnectionState = ParticipantConnectionState.Joined,
                JoinedAt        = DateTime.UtcNow
            };
            await _repo.CreateParticipantAsync(participant);

            // Log event
            await _repo.CreateEventAsync(new RoomEvent
            {
                MeetingRoomId = room.Id,
                ActorUserId   = dto.UserId,
                EventType     = "participant.joined",
                OccurredAt    = DateTime.UtcNow
            });

            // Cập nhật trạng thái room nếu cần
            if (room.Status == RoomStatus.Idle)
            {
                room.Status = RoomStatus.Waiting;
                await _repo.UpdateRoomAsync(room);
            }

            // Lấy danh sách active participants
            var allParticipants = await _repo.GetParticipantsByRoomIdAsync(room.Id);
            var activeParticipants = allParticipants
                .Where(p => p.ConnectionState == ParticipantConnectionState.Joined)
                .Select(MapParticipantToDto)
                .ToList();

            return new JoinRoomResponseDto
            {
                RoomCode                = room.RoomCode,
                PeerId                  = peerId,
                StunServerUri           = room.StunServerUri,
                TurnServerUri           = room.TurnServerUri,
                TurnUsername            = room.TurnUsername,
                TurnCredential          = room.TurnCredential,
                TurnCredentialExpiresAt = room.TurnCredentialExpiresAt,
                RoomStatus              = room.Status.ToString(),
                CurrentParticipants     = activeParticipants
            };
        }

        public async Task<bool> LeaveRoomAsync(string roomCode, LeaveRoomDto dto)
        {
            var room = await _repo.GetRoomByCodeAsync(roomCode);
            if (room == null) return false;

            var participant = await _repo.GetActiveParticipantAsync(room.Id, dto.UserId);
            if (participant == null) return false;

            participant.ConnectionState = ParticipantConnectionState.Left;
            participant.LeftAt = DateTime.UtcNow;
            await _repo.UpdateParticipantAsync(participant);

            // Log event
            await _repo.CreateEventAsync(new RoomEvent
            {
                MeetingRoomId = room.Id,
                ActorUserId   = dto.UserId,
                EventType     = "participant.left",
                OccurredAt    = DateTime.UtcNow
            });

            return true;
        }

        public async Task<IEnumerable<RoomParticipantResponseDto>> GetParticipantsAsync(string roomCode)
        {
            var room = await _repo.GetRoomByCodeAsync(roomCode);
            if (room == null)
                throw new KeyNotFoundException("Room not found.");

            var participants = await _repo.GetParticipantsByRoomIdAsync(room.Id);
            return participants.Select(MapParticipantToDto);
        }

        public async Task<IEnumerable<RoomEventResponseDto>> GetEventsAsync(string roomCode)
        {
            var room = await _repo.GetRoomByCodeAsync(roomCode);
            if (room == null)
                throw new KeyNotFoundException("Room not found.");

            var events = await _repo.GetEventsByRoomIdAsync(room.Id);
            return events.Select(MapEventToDto);
        }

        public async Task<bool> CloseRoomAsync(string roomCode)
        {
            var room = await _repo.GetRoomByCodeAsync(roomCode);
            if (room == null) return false;

            if (room.Status == RoomStatus.Closed) return true;

            room.Status = RoomStatus.Closed;
            room.EndedAt = DateTime.UtcNow;

            await _repo.UpdateRoomAsync(room);

            await _repo.CreateEventAsync(new RoomEvent
            {
                MeetingRoomId = room.Id,
                ActorUserId   = Guid.Empty, // System
                EventType     = "room.closed",
                OccurredAt    = DateTime.UtcNow
            });

            return true;
        }

        // ═══════════════════════════════════════════════════════════
        //  Feedback
        // ═══════════════════════════════════════════════════════════

        public async Task<bool> SubmitFeedbackAsync(int scheduleId, int assignmentId, SubmitFeedbackDto dto)
        {
            var assignment = await _repo.GetAssignmentByIdAsync(assignmentId);
            if (assignment == null || assignment.InterviewScheduleId != scheduleId)
                return false;

            if (!Enum.TryParse<InterviewResult>(dto.Result, true, out var result))
                throw new ArgumentException($"Invalid result: {dto.Result}");

            assignment.FeedbackNotes = dto.FeedbackNotes;
            assignment.Result = result;
            assignment.Score = dto.Score;
            assignment.FeedbackSubmittedAt = DateTime.UtcNow;

            return await _repo.UpdateAssignmentAsync(assignment);
        }

        public async Task<FeedbackSummaryResponseDto?> GetFeedbackSummaryAsync(int scheduleId)
        {
            var schedule = await _repo.GetScheduleByIdAsync(scheduleId);
            if (schedule == null) return null;

            return new FeedbackSummaryResponseDto
            {
                InterviewScheduleId = schedule.Id,
                Title               = schedule.Title,
                Feedbacks           = schedule.Assignments.Select(MapAssignmentToDto).ToList()
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════════════════════

        private static string GenerateRoomCode()
        {
            var part1 = Guid.NewGuid().ToString("N")[..4];
            var part2 = Guid.NewGuid().ToString("N")[..4];
            return $"{part1}-{part2}";
        }

        private static InterviewScheduleResponseDto MapScheduleToDto(InterviewSchedule s)
        {
            return new InterviewScheduleResponseDto
            {
                Id              = s.Id,
                ApplicationId   = s.ApplicationId,
                CandidateUserId = s.CandidateUserId,
                CampaignId      = s.CampaignId,
                CreatedByUserId = s.CreatedByUserId,
                Title           = s.Title,
                Description     = s.Description,
                ScheduledAt     = s.ScheduledAt,
                DurationMinutes = s.DurationMinutes,
                Status          = s.Status.ToString(),
                CancelReason    = s.CancelReason,
                CreatedAt       = s.CreatedAt,
                UpdatedAt       = s.UpdatedAt,
                Assignments     = s.Assignments?.Select(MapAssignmentToDto).ToList() ?? new(),
                MeetingRoom     = s.MeetingRoom != null ? MapRoomToDto(s.MeetingRoom) : null
            };
        }

        private static InterviewAssignmentResponseDto MapAssignmentToDto(InterviewAssignment a)
        {
            return new InterviewAssignmentResponseDto
            {
                Id                  = a.Id,
                InterviewScheduleId = a.InterviewScheduleId,
                InterviewerUserId   = a.InterviewerUserId,
                Role                = a.Role.ToString(),
                HasConfirmed        = a.HasConfirmed,
                FeedbackNotes       = a.FeedbackNotes,
                Result              = a.Result?.ToString(),
                Score               = a.Score,
                AssignedAt          = a.AssignedAt,
                FeedbackSubmittedAt = a.FeedbackSubmittedAt
            };
        }

        private static MeetingRoomResponseDto MapRoomToDto(MeetingRoom r)
        {
            return new MeetingRoomResponseDto
            {
                Id                     = r.Id,
                InterviewScheduleId    = r.InterviewScheduleId,
                RoomCode               = r.RoomCode,
                StunServerUri          = r.StunServerUri,
                TurnServerUri          = r.TurnServerUri,
                TurnUsername           = r.TurnUsername,
                TurnCredential         = r.TurnCredential,
                TurnCredentialExpiresAt = r.TurnCredentialExpiresAt,
                IsRecordingEnabled     = r.IsRecordingEnabled,
                IsWaitingRoomEnabled   = r.IsWaitingRoomEnabled,
                MaxParticipants        = r.MaxParticipants,
                Status                 = r.Status.ToString(),
                StartedAt              = r.StartedAt,
                EndedAt                = r.EndedAt,
                CreatedAt              = r.CreatedAt
            };
        }

        private static RoomParticipantResponseDto MapParticipantToDto(RoomParticipant p)
        {
            return new RoomParticipantResponseDto
            {
                Id              = p.Id,
                UserId          = p.UserId,
                DisplayName     = p.DisplayName,
                Role            = p.Role,
                PeerId          = p.PeerId,
                ConnectionState = p.ConnectionState.ToString(),
                JoinedAt        = p.JoinedAt,
                LeftAt          = p.LeftAt
            };
        }

        private static RoomEventResponseDto MapEventToDto(RoomEvent e)
        {
            return new RoomEventResponseDto
            {
                Id            = e.Id,
                MeetingRoomId = e.MeetingRoomId,
                ActorUserId   = e.ActorUserId,
                EventType     = e.EventType,
                Payload       = e.Payload,
                OccurredAt    = e.OccurredAt
            };
        }
    }
}
