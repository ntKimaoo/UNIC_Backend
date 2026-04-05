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

            // Add Proposed Time Slots
            if (dto.ProposedTimeSlots != null && dto.ProposedTimeSlots.Count > 0)
            {
                foreach (var slot in dto.ProposedTimeSlots)
                {
                    if (DateTime.TryParse($"{slot.Date}T{slot.Time}", out var proposedAt))
                    {
                        var timeSlot = new ProposedTimeSlot
                        {
                            InterviewScheduleId = created.Id,
                            ProposedAt          = proposedAt,
                            IsSelected          = false,
                            CreatedAt           = DateTime.UtcNow
                        };
                        await _repo.CreateTimeSlotAsync(timeSlot);
                    }
                }
            }

            // Auto-create MeetingRoom
            var room = new MeetingRoom
            {
                InterviewScheduleId = created.Id,
                RoomType            = RoomType.Interview,
                Title               = dto.Title,
                CreatedByUserId     = dto.CreatedByUserId,
                ScheduledStartAt    = dto.ScheduledAt,
                ScheduledEndAt      = dto.ScheduledAt.AddMinutes(dto.DurationMinutes),
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
        //  Proposed Time Slots
        // ═══════════════════════════════════════════════════════════

        public async Task<List<ProposedTimeSlotResponseDto>> GetTimeSlotsAsync(int scheduleId)
        {
            var slots = await _repo.GetTimeSlotsByScheduleIdAsync(scheduleId);
            return slots.Select(MapProposedTimeSlotToDto).ToList();
        }

        public async Task<InterviewScheduleResponseDto> ConfirmTimeSlotAsync(int scheduleId, ConfirmTimeSlotDto dto)
        {
            var schedule = await _repo.GetScheduleByIdAsync(scheduleId);
            if (schedule == null)
                throw new KeyNotFoundException("Interview schedule not found.");

            if (schedule.Status != InterviewStatus.Scheduled && schedule.Status != InterviewStatus.Rescheduled)
                throw new InvalidOperationException("Chỉ có thể xác nhận giờ cho lịch đang ở trạng thái Scheduled hoặc Rescheduled.");

            var slots = (await _repo.GetTimeSlotsByScheduleIdAsync(scheduleId)).ToList();
            var selectedSlot = slots.FirstOrDefault(s => s.Id == dto.TimeSlotId);

            if (selectedSlot == null)
                throw new ArgumentException("Time slot không hợp lệ hoặc không thuộc về lịch phỏng vấn này.");

            // Đánh dấu slot được chọn, các slot khác bỏ chọn
            foreach (var slot in slots)
            {
                slot.IsSelected = (slot.Id == dto.TimeSlotId);
                await _repo.UpdateTimeSlotAsync(slot);
            }

            // Cập nhật ScheduledAt của InterviewSchedule
            schedule.ScheduledAt = selectedSlot.ProposedAt;
            // Workflow: Confirm slot xong sẽ coi như lịch đã chốt giờ => chuyển status sang Confirmed, 
            // giống như frontend gọi api update status sang Confirmed
            schedule.Status = InterviewStatus.Confirmed;
            schedule.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateScheduleAsync(schedule);

            // Cập nhật lại thời gian MeetingRoom nếu có báo phòng
            if (schedule.MeetingRoom != null)
            {
                schedule.MeetingRoom.ScheduledStartAt = selectedSlot.ProposedAt;
                schedule.MeetingRoom.ScheduledEndAt = selectedSlot.ProposedAt.AddMinutes(schedule.DurationMinutes);
                await _repo.UpdateRoomAsync(schedule.MeetingRoom);
            }

            var full = await _repo.GetScheduleByIdAsync(scheduleId);
            return MapScheduleToDto(full!);
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

        public async Task<MeetingRoomResponseDto?> GetRoomByIdAsync(int roomId)
        {
            var room = await _repo.GetRoomByIdAsync(roomId);
            return room == null ? null : MapRoomToDto(room);
        }

        public async Task<MeetingRoomResponseDto?> GetRoomByCodeAsync(string roomCode)
        {
            var room = await _repo.GetRoomByCodeAsync(roomCode);
            return room == null ? null : MapRoomToDto(room);
        }

        public async Task<MeetingRoomResponseDto> CreateStandaloneRoomAsync(CreateMeetingRoomDto dto)
        {
            if (!Enum.TryParse<RoomType>(dto.RoomType, true, out var roomType))
                roomType = RoomType.General;

            var room = new MeetingRoom
            {
                RoomType            = roomType,
                Title               = dto.Title,
                Description         = dto.Description,
                CreatedByUserId     = dto.CreatedByUserId,
                ScheduledStartAt    = dto.ScheduledStartAt,
                ScheduledEndAt      = dto.ScheduledEndAt,
                InterviewScheduleId = dto.InterviewScheduleId,
                RoomCode            = GenerateRoomCode(),
                MaxParticipants     = dto.MaxParticipants,
                IsWaitingRoomEnabled = dto.IsWaitingRoomEnabled,
                IsRecordingEnabled  = dto.IsRecordingEnabled,
                Status              = RoomStatus.Idle,
                CreatedAt           = DateTime.UtcNow
            };

            var created = await _repo.CreateRoomAsync(room);
            return MapRoomToDto(created);
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
                MeetingRoom     = s.MeetingRoom != null ? MapRoomToDto(s.MeetingRoom) : null,
                ProposedTimeSlots = s.ProposedTimeSlots?.Select(MapProposedTimeSlotToDto).ToList() ?? new()
            };
        }

        private static ProposedTimeSlotResponseDto MapProposedTimeSlotToDto(ProposedTimeSlot t)
        {
            return new ProposedTimeSlotResponseDto
            {
                Id                  = t.Id,
                InterviewScheduleId = t.InterviewScheduleId,
                ProposedAt          = t.ProposedAt,
                IsSelected          = t.IsSelected,
                CreatedAt           = t.CreatedAt
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
                AssignedAt          = a.AssignedAt,
                FeedbackSubmittedAt = a.FeedbackSubmittedAt,
                CriteriaScores      = a.CriteriaScores?.Select(cs => new CriteriaScoreResponseDto
                {
                    Id                    = cs.Id,
                    EvaluationCriterionId = cs.EvaluationCriterionId,
                    Note                  = cs.Note,
                    CreatedAt             = cs.CreatedAt
                }).ToList() ?? new()
            };
        }

        private static MeetingRoomResponseDto MapRoomToDto(MeetingRoom r)
        {
            return new MeetingRoomResponseDto
            {
                Id                     = r.Id,
                RoomType               = r.RoomType.ToString(),
                Title                  = r.Title,
                Description            = r.Description,
                CreatedByUserId        = r.CreatedByUserId,
                ScheduledStartAt       = r.ScheduledStartAt,
                ScheduledEndAt         = r.ScheduledEndAt,
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

        // ═══════════════════════════════════════════════════════════
        //  Evaluation Criteria
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 5 tiêu chí mặc định – tự tạo khi campaign chưa có tiêu chí nào.
        /// </summary>
        private static readonly List<(string Name, string Desc, int Weight)> DefaultCriteria = new()
        {
            ("Kiến thức chuyên môn",       "Đánh giá kiến thức nền tảng, chuyên ngành liên quan", 25),
            ("Kỹ năng giao tiếp",          "Khả năng trình bày, tương tác, diễn đạt ý tưởng",    20),
            ("Tư duy & giải quyết vấn đề", "Khả năng phân tích, tư duy logic, đề xuất giải pháp", 20),
            ("Phù hợp văn hóa CLB",        "Mức độ phù hợp với giá trị, phong cách hoạt động CLB", 20),
            ("Kinh nghiệm & thành tích",   "Kinh nghiệm hoạt động, thành tích cá nhân, kỹ năng mềm", 15),
        };

        public async Task<List<EvaluationCriterionDto>> GetCampaignCriteriaAsync(int campaignId)
        {
            var criteria = await _repo.GetCriteriaByCampaignIdAsync(campaignId);
            var list = criteria.ToList();

            // Nếu chưa có tiêu chí → seed 5 default
            if (list.Count == 0)
            {
                foreach (var (name, desc, weight) in DefaultCriteria)
                {
                    var c = new EvaluationCriterion
                    {
                        CampaignId   = campaignId,
                        Name         = name,
                        Description  = desc,
                        Weight       = weight,
                        IsDefault    = true,
                        CreatedAt    = DateTime.UtcNow
                    };
                    var created = await _repo.CreateCriterionAsync(c);
                    list.Add(created);
                }
            }

            return list.Select(MapCriterionToDto).ToList();
        }

        public async Task<EvaluationCriterionDto> CreateCriterionAsync(int campaignId, CreateEvaluationCriterionDto dto)
        {
            var criterion = new EvaluationCriterion
            {
                CampaignId   = campaignId,
                Name         = dto.Name,
                Description  = dto.Description,
                Weight       = dto.Weight,
                IsDefault    = false,
                CreatedAt    = DateTime.UtcNow
            };

            var created = await _repo.CreateCriterionAsync(criterion);
            return MapCriterionToDto(created);
        }

        public async Task<EvaluationCriterionDto?> UpdateCriterionAsync(int criterionId, UpdateEvaluationCriterionDto dto)
        {
            var criterion = await _repo.GetCriterionByIdAsync(criterionId);
            if (criterion == null) return null;

            if (!string.IsNullOrEmpty(dto.Name))
                criterion.Name = dto.Name;
            if (dto.Description != null)
                criterion.Description = dto.Description;
            if (dto.Weight.HasValue)
                criterion.Weight = dto.Weight.Value;

            await _repo.UpdateCriterionAsync(criterion);
            return MapCriterionToDto(criterion);
        }

        public async Task<bool> DeleteCriterionAsync(int criterionId)
        {
            return await _repo.DeleteCriterionAsync(criterionId);
        }

        public async Task<bool> AssignCriteriaToInterviewerAsync(int scheduleId, int assignmentId, AssignCriteriaDto dto)
        {
            var assignment = await _repo.GetAssignmentByIdAsync(assignmentId);
            if (assignment == null || assignment.InterviewScheduleId != scheduleId)
                return false;

            // Xoá CriteriaScore cũ của assignment này
            await _repo.DeleteCriteriaScoresByAssignmentIdAsync(assignmentId);

            // Tạo CriteriaScore mới cho từng tiêu chí được phân công
            foreach (var criterionId in dto.CriteriaIds)
            {
                var score = new CriteriaScore
                {
                    InterviewAssignmentId = assignmentId,
                    EvaluationCriterionId = criterionId,
                    Note                  = null,
                    CreatedAt             = DateTime.UtcNow
                };
                await _repo.CreateCriteriaScoreAsync(score);
            }

            return true;
        }

        // ═══════════════════════════════════════════════════════════
        //  Criteria-based Feedback
        // ═══════════════════════════════════════════════════════════

        public async Task<bool> SubmitCriteriaFeedbackAsync(int scheduleId, int assignmentId, SubmitCriteriaFeedbackDto dto)
        {
            var assignment = await _repo.GetAssignmentByIdAsync(assignmentId);
            if (assignment == null || assignment.InterviewScheduleId != scheduleId)
                return false;

            // Lưu / cập nhật nhận xét từng tiêu chí (upsert)
            foreach (var item in dto.Notes)
            {
                // Kiểm tra xem đã có CriteriaScore chưa (tạo từ bước Assign Criteria)
                var existing = await _repo.GetCriteriaScoreAsync(assignmentId, item.CriterionId);
                if (existing != null)
                {
                    // Cập nhật note vào record đã tồn tại
                    existing.Note = item.Note;
                    existing.CreatedAt = DateTime.UtcNow;
                    await _repo.UpdateCriteriaScoreAsync(existing);
                }
                else
                {
                    // Tạo mới nếu chưa được phân tiêu chí trước đó
                    var note = new CriteriaScore
                    {
                        InterviewAssignmentId = assignmentId,
                        EvaluationCriterionId = item.CriterionId,
                        Note                  = item.Note,
                        CreatedAt             = DateTime.UtcNow
                    };
                    await _repo.CreateCriteriaScoreAsync(note);
                }
            }

            // Cập nhật feedback tổng hợp vào assignment
            if (!Enum.TryParse<InterviewResult>(dto.Result, true, out var result))
                throw new ArgumentException($"Invalid result: {dto.Result}");

            assignment.FeedbackNotes       = dto.FeedbackNotes;
            assignment.Result              = result;
            assignment.FeedbackSubmittedAt = DateTime.UtcNow;

            return await _repo.UpdateAssignmentAsync(assignment);
        }

        public async Task<EvaluationSummaryDto?> GetEvaluationSummaryAsync(int scheduleId)
        {
            var schedule = await _repo.GetScheduleByIdAsync(scheduleId);
            if (schedule == null) return null;

            var criteria = (await _repo.GetCriteriaByCampaignIdAsync(schedule.CampaignId)).ToList();
            var allNotes = (await _repo.GetCriteriaScoresByScheduleIdAsync(scheduleId)).ToList();

            var criteriaSummaries = criteria.Select(c =>
            {
                var notesForCriterion = allNotes.Where(s => s.EvaluationCriterionId == c.Id).ToList();

                return new CriteriaSummaryItemDto
                {
                    CriterionId   = c.Id,
                    CriterionName = c.Name,
                    Weight        = c.Weight,
                    IndividualNotes = notesForCriterion.Select(s => new CriteriaNoteResultDto
                    {
                        CriterionId       = c.Id,
                        CriterionName     = c.Name,
                        Weight            = c.Weight,
                        Note              = s.Note,
                        InterviewerUserId = s.InterviewAssignment.InterviewerUserId,
                        InterviewerRole   = s.InterviewAssignment.Role.ToString()
                    }).ToList()
                };
            }).ToList();

            return new EvaluationSummaryDto
            {
                InterviewScheduleId = schedule.Id,
                Title               = schedule.Title,
                CandidateUserId     = schedule.CandidateUserId,
                CampaignId          = schedule.CampaignId,
                CriteriaSummaries   = criteriaSummaries,
                Feedbacks           = schedule.Assignments?.Select(MapAssignmentToDto).ToList() ?? new()
            };
        }

        public async Task<List<CandidateComparisonItemDto>> GetCampaignComparisonAsync(int campaignId)
        {
            var schedules = (await _repo.GetSchedulesAsync(campaignId, null, null, null)).ToList();
            var criteria  = (await _repo.GetCriteriaByCampaignIdAsync(campaignId)).ToList();

            var items = new List<CandidateComparisonItemDto>();

            foreach (var schedule in schedules)
            {
                var allNotes = (await _repo.GetCriteriaScoresByScheduleIdAsync(schedule.Id)).ToList();

                var criteriaSummaries = criteria.Select(c =>
                {
                    var notesForCriterion = allNotes.Where(n => n.EvaluationCriterionId == c.Id).ToList();
                    return new CriteriaSummaryItemDto
                    {
                        CriterionId   = c.Id,
                        CriterionName = c.Name,
                        Weight        = c.Weight,
                        IndividualNotes = notesForCriterion.Select(n => new CriteriaNoteResultDto
                        {
                            CriterionId       = c.Id,
                            CriterionName     = c.Name,
                            Weight            = c.Weight,
                            Note              = n.Note,
                            InterviewerUserId = n.InterviewAssignment.InterviewerUserId,
                            InterviewerRole   = n.InterviewAssignment.Role.ToString()
                        }).ToList()
                    };
                }).ToList();

                var feedbackNotes = schedule.Assignments
                    .Where(a => !string.IsNullOrEmpty(a.FeedbackNotes))
                    .Select(a => a.FeedbackNotes!)
                    .ToList();

                items.Add(new CandidateComparisonItemDto
                {
                    InterviewScheduleId = schedule.Id,
                    CandidateUserId     = schedule.CandidateUserId,
                    Title               = schedule.Title,
                    FeedbackNotes       = feedbackNotes,
                    CriteriaSummaries   = criteriaSummaries
                });
            }

            return items;
        }

        // ═══════════════════════════════════════════════════════════
        //  Decisions & Publish
        // ═══════════════════════════════════════════════════════════

        public async Task<List<CampaignDecisionResponseDto>> SubmitDecisionsAsync(int campaignId, SubmitDecisionsDto dto)
        {
            var results = new List<CampaignDecisionResponseDto>();

            foreach (var item in dto.Decisions)
            {
                if (!Enum.TryParse<DecisionResult>(item.Decision, true, out var decision))
                    throw new ArgumentException($"Invalid decision: {item.Decision}");

                // Kiểm tra xem đã có decision chưa → update
                var existing = await _repo.GetDecisionByScheduleIdAsync(item.InterviewScheduleId);
                if (existing != null)
                {
                    existing.Decision = decision;
                    existing.DecidedByUserId = dto.DecidedByUserId;
                    existing.DecidedAt = DateTime.UtcNow;
                    await _repo.UpdateDecisionAsync(existing);
                    results.Add(MapDecisionToDto(existing));
                }
                else
                {
                    var d = new CampaignDecision
                    {
                        CampaignId          = campaignId,
                        InterviewScheduleId = item.InterviewScheduleId,
                        CandidateUserId     = item.CandidateUserId,
                        Decision            = decision,
                        DecidedByUserId     = dto.DecidedByUserId,
                        DecidedAt           = DateTime.UtcNow,
                        PublishStatus       = PublishStatus.Draft
                    };
                    var created = await _repo.CreateDecisionAsync(d);
                    results.Add(MapDecisionToDto(created));
                }
            }

            return results;
        }

        public async Task<PublishStatusResponseDto> PublishResultsAsync(int campaignId, PublishResultDto dto)
        {
            var decisions = (await _repo.GetDecisionsByCampaignIdAsync(campaignId)).ToList();

            if (decisions.Count == 0)
                throw new InvalidOperationException("Chưa có quyết định nào cho campaign này.");

            foreach (var d in decisions)
            {
                if (dto.Mode.Equals("Now", StringComparison.OrdinalIgnoreCase))
                {
                    d.PublishStatus = PublishStatus.Published;
                    d.PublishedAt = DateTime.UtcNow;
                }
                else // Schedule
                {
                    if (!dto.ScheduledAt.HasValue)
                        throw new ArgumentException("ScheduledAt bắt buộc khi Mode = Schedule.");

                    d.PublishStatus = PublishStatus.Scheduled;
                    d.ScheduledPublishAt = dto.ScheduledAt.Value;
                }

                d.NotificationChannels = dto.NotificationChannels;
                await _repo.UpdateDecisionAsync(d);
            }

            return BuildPublishStatus(campaignId, decisions);
        }

        public async Task<PublishStatusResponseDto?> GetPublishStatusAsync(int campaignId)
        {
            var decisions = (await _repo.GetDecisionsByCampaignIdAsync(campaignId)).ToList();
            if (decisions.Count == 0) return null;

            return BuildPublishStatus(campaignId, decisions);
        }

        // ═══════════════════════════════════════════════════════════
        //  New Helpers
        // ═══════════════════════════════════════════════════════════

        private static EvaluationCriterionDto MapCriterionToDto(EvaluationCriterion c)
        {
            return new EvaluationCriterionDto
            {
                Id           = c.Id,
                CampaignId   = c.CampaignId,
                Name         = c.Name,
                Description  = c.Description,
                Weight       = c.Weight,
                IsDefault    = c.IsDefault
            };
        }

        private static CampaignDecisionResponseDto MapDecisionToDto(CampaignDecision d)
        {
            return new CampaignDecisionResponseDto
            {
                Id                  = d.Id,
                CampaignId          = d.CampaignId,
                InterviewScheduleId = d.InterviewScheduleId,
                CandidateUserId     = d.CandidateUserId,
                Decision            = d.Decision.ToString(),
                DecidedByUserId     = d.DecidedByUserId,
                DecidedAt           = d.DecidedAt,
                PublishStatus       = d.PublishStatus.ToString(),
                ScheduledPublishAt  = d.ScheduledPublishAt,
                PublishedAt         = d.PublishedAt
            };
        }

        private static PublishStatusResponseDto BuildPublishStatus(int campaignId, List<CampaignDecision> decisions)
        {
            var overallStatus = "Draft";
            if (decisions.All(d => d.PublishStatus == PublishStatus.Published))
                overallStatus = "Published";
            else if (decisions.Any(d => d.PublishStatus == PublishStatus.Scheduled))
                overallStatus = "Scheduled";

            return new PublishStatusResponseDto
            {
                CampaignId         = campaignId,
                OverallStatus      = overallStatus,
                TotalDecisions     = decisions.Count,
                AcceptCount        = decisions.Count(d => d.Decision == DecisionResult.Accept),
                RejectCount        = decisions.Count(d => d.Decision == DecisionResult.Reject),
                WaitlistCount      = decisions.Count(d => d.Decision == DecisionResult.Waitlist),
                ScheduledPublishAt = decisions.FirstOrDefault(d => d.ScheduledPublishAt.HasValue)?.ScheduledPublishAt,
                PublishedAt        = decisions.FirstOrDefault(d => d.PublishedAt.HasValue)?.PublishedAt,
                Decisions          = decisions.Select(MapDecisionToDto).ToList()
            };
        }
    }
}

