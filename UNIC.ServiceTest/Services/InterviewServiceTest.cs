using BusinessLogic.DTOs;
using BusinessLogic.Services.Implementation;
using BusinessLogic.Services.Interface;
using DataAccess.Models.Meeting;
using DataAccess.Models.Meeting.Enums;
using DataAccess.Repositories.Interface;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class InterviewServiceTest
    {
        private readonly Mock<IInterviewRepository> _mockRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IRecruitmentCampaignRepository> _mockCampaignRepo;
        private readonly InterviewService _interviewService;

        public InterviewServiceTest()
        {
            _mockRepo = new Mock<IInterviewRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockEmailService = new Mock<IEmailService>();
            _mockCampaignRepo = new Mock<IRecruitmentCampaignRepository>();
            _interviewService = new InterviewService(
                _mockRepo.Object, 
                _mockUserRepo.Object, 
                _mockEmailService.Object, 
                _mockCampaignRepo.Object);
        }

        #region CreateScheduleAsync

        [Fact]
        public async Task CreateScheduleAsync_ShouldCreateScheduleRoomAndAssignments()
        {
            var dto = new CreateInterviewScheduleDto
            {
                ApplicationId = 1, CandidateUserId = Guid.NewGuid(), Title = "Test",
                Interviewers = new List<AssignInterviewerItemDto> 
                { 
                    new AssignInterviewerItemDto { InterviewerUserId = Guid.NewGuid(), Role = "Interviewer" } 
                }
            };

            var createdSchedule = new InterviewSchedule { Id = 10, Title = "Test" };
            
            _mockRepo.Setup(r => r.CreateScheduleAsync(It.IsAny<InterviewSchedule>())).ReturnsAsync(createdSchedule);
            _mockRepo.Setup(r => r.CreateRoomAsync(It.IsAny<MeetingRoom>())).ReturnsAsync(new MeetingRoom());
            _mockRepo.Setup(r => r.CreateAssignmentAsync(It.IsAny<InterviewAssignment>())).ReturnsAsync(new InterviewAssignment());
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(10)).ReturnsAsync(createdSchedule);

            var result = await _interviewService.CreateScheduleAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("Test", result.Title);
            _mockRepo.Verify(r => r.CreateScheduleAsync(It.IsAny<InterviewSchedule>()), Times.Once);
            _mockRepo.Verify(r => r.CreateRoomAsync(It.IsAny<MeetingRoom>()), Times.Once);
            _mockRepo.Verify(r => r.CreateAssignmentAsync(It.IsAny<InterviewAssignment>()), Times.Once);
        }

        #endregion

        #region UpdateScheduleStatusAsync

        [Fact]
        public async Task UpdateScheduleStatusAsync_ShouldThrowException_WhenStatusInvalid()
        {
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(new InterviewSchedule());

            var dto = new UpdateInterviewStatusDto { Status = "InvalidStatus" };
            await Assert.ThrowsAsync<ArgumentException>(() => _interviewService.UpdateScheduleStatusAsync(1, dto));
        }

        [Fact]
        public async Task UpdateScheduleStatusAsync_ShouldThrowException_WhenConfirmingWrongState()
        {
            var schedule = new InterviewSchedule { Status = InterviewStatus.InProgress };
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(schedule);

            var dto = new UpdateInterviewStatusDto { Status = "Confirmed" };
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _interviewService.UpdateScheduleStatusAsync(1, dto));
            Assert.Contains("Chỉ có thể Confirm", ex.Message);
        }

        [Fact]
        public async Task UpdateScheduleStatusAsync_ShouldUpdateAndSave_WhenValid()
        {
            var schedule = new InterviewSchedule { Status = InterviewStatus.Scheduled };
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(schedule);
            _mockRepo.Setup(r => r.UpdateScheduleAsync(schedule)).ReturnsAsync(true);

            var dto = new UpdateInterviewStatusDto { Status = "Confirmed" };
            var result = await _interviewService.UpdateScheduleStatusAsync(1, dto);

            Assert.True(result);
            Assert.Equal(InterviewStatus.Confirmed, schedule.Status);
            _mockRepo.Verify(r => r.UpdateScheduleAsync(schedule), Times.Once);
        }

        #endregion

        #region JoinRoomAsync

        [Fact]
        public async Task JoinRoomAsync_ShouldThrowException_WhenRoomNotFound()
        {
            _mockRepo.Setup(r => r.GetRoomByCodeAsync("code")).ReturnsAsync((MeetingRoom?)null);
            
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _interviewService.JoinRoomAsync("code", new JoinRoomDto()));
        }

        [Fact]
        public async Task JoinRoomAsync_ShouldThrowException_WhenRoomClosed()
        {
            var room = new MeetingRoom { Status = RoomStatus.Closed };
            _mockRepo.Setup(r => r.GetRoomByCodeAsync("code")).ReturnsAsync(room);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _interviewService.JoinRoomAsync("code", new JoinRoomDto()));
            Assert.Contains("Room đã đóng", ex.Message);
        }

        [Fact]
        public async Task JoinRoomAsync_ShouldThrowException_WhenRoomFull()
        {
            var room = new MeetingRoom 
            { 
                Status = RoomStatus.Idle, MaxParticipants = 1,
                Participants = new List<RoomParticipant> { new RoomParticipant { ConnectionState = ParticipantConnectionState.Joined } }
            };
            _mockRepo.Setup(r => r.GetRoomByCodeAsync("code")).ReturnsAsync(room);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _interviewService.JoinRoomAsync("code", new JoinRoomDto()));
            Assert.Contains("Room đã đầy", ex.Message);
        }

        [Fact]
        public async Task JoinRoomAsync_ShouldAddParticipantAndEvent_WhenValid()
        {
            var room = new MeetingRoom { Id = 1, RoomCode = "code", Status = RoomStatus.Idle, MaxParticipants = 10, Participants = new List<RoomParticipant>() };
            _mockRepo.Setup(r => r.GetRoomByCodeAsync("code")).ReturnsAsync(room);
            
            _mockRepo.Setup(r => r.CreateParticipantAsync(It.IsAny<RoomParticipant>())).ReturnsAsync(new RoomParticipant());
            _mockRepo.Setup(r => r.CreateEventAsync(It.IsAny<RoomEvent>())).ReturnsAsync(new RoomEvent());
            _mockRepo.Setup(r => r.GetParticipantsByRoomIdAsync(1)).ReturnsAsync(new List<RoomParticipant>());
            _mockRepo.Setup(r => r.UpdateRoomAsync(room)).ReturnsAsync(true);

            var result = await _interviewService.JoinRoomAsync("code", new JoinRoomDto { UserId = Guid.NewGuid() });

            Assert.NotNull(result);
            Assert.Equal("Waiting", result.RoomStatus);
            _mockRepo.Verify(r => r.CreateParticipantAsync(It.IsAny<RoomParticipant>()), Times.Once);
            _mockRepo.Verify(r => r.CreateEventAsync(It.IsAny<RoomEvent>()), Times.Once);
            _mockRepo.Verify(r => r.UpdateRoomAsync(room), Times.Once);
        }

        #endregion

        #region LeaveRoomAsync

        [Fact]
        public async Task LeaveRoomAsync_ShouldReturnFalse_WhenParticipantNotFound()
        {
            var room = new MeetingRoom { Id = 1 };
            _mockRepo.Setup(r => r.GetRoomByCodeAsync("code")).ReturnsAsync(room);
            _mockRepo.Setup(r => r.GetActiveParticipantAsync(1, It.IsAny<Guid>())).ReturnsAsync((RoomParticipant?)null);

            var result = await _interviewService.LeaveRoomAsync("code", new LeaveRoomDto());
            Assert.False(result);
        }

        [Fact]
        public async Task LeaveRoomAsync_ShouldUpdateParticipantAndLogEvent_WhenValid()
        {
            var room = new MeetingRoom { Id = 1 };
            var participant = new RoomParticipant { ConnectionState = ParticipantConnectionState.Joined };
            var userId = Guid.NewGuid();

            _mockRepo.Setup(r => r.GetRoomByCodeAsync("code")).ReturnsAsync(room);
            _mockRepo.Setup(r => r.GetActiveParticipantAsync(1, userId)).ReturnsAsync(participant);
            _mockRepo.Setup(r => r.UpdateParticipantAsync(participant)).ReturnsAsync(true);

            var result = await _interviewService.LeaveRoomAsync("code", new LeaveRoomDto { UserId = userId });

            Assert.True(result);
            Assert.Equal(ParticipantConnectionState.Left, participant.ConnectionState);
            _mockRepo.Verify(r => r.UpdateParticipantAsync(participant), Times.Once);
            _mockRepo.Verify(r => r.CreateEventAsync(It.IsAny<RoomEvent>()), Times.Once);
        }

        #endregion

        #region Feedback

        [Fact]
        public async Task SubmitFeedbackAsync_ShouldReturnFalse_WhenAssignmentMismatch()
        {
            var assignment = new InterviewAssignment { InterviewScheduleId = 2 };
            _mockRepo.Setup(r => r.GetAssignmentByIdAsync(1)).ReturnsAsync(assignment);

            var result = await _interviewService.SubmitFeedbackAsync(1, 1, new SubmitFeedbackDto());
            Assert.False(result);
        }

        [Fact]
        public async Task SubmitFeedbackAsync_ShouldUpdateAndReturnTrue_WhenValid()
        {
            var assignment = new InterviewAssignment { InterviewScheduleId = 1 };
            _mockRepo.Setup(r => r.GetAssignmentByIdAsync(1)).ReturnsAsync(assignment);
            _mockRepo.Setup(r => r.UpdateAssignmentAsync(assignment)).ReturnsAsync(true);

            var dto = new SubmitFeedbackDto { Result = "Pass", FeedbackNotes = "Good" };
            var result = await _interviewService.SubmitFeedbackAsync(1, 1, dto);

            Assert.True(result);
            Assert.Equal(InterviewResult.Pass, assignment.Result);
            _mockRepo.Verify(r => r.UpdateAssignmentAsync(assignment), Times.Once);
        }

        #endregion

        #region GetSchedulesAsync

        [Fact]
        public async Task GetSchedulesAsync_ReturnsMappedDtos()
        {
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule { Id = 1, Title = "S1", Status = InterviewStatus.Scheduled }
            };
            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);

            var result = await _interviewService.GetSchedulesAsync(1, null, null, null);
            Assert.Single(result);
        }

        #endregion

        #region GetScheduleByIdAsync

        [Fact]
        public async Task GetScheduleByIdAsync_ReturnsDto_WhenFound()
        {
            var schedule = new InterviewSchedule { Id = 1, Title = "Test" };
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(schedule);

            var result = await _interviewService.GetScheduleByIdAsync(1);
            Assert.NotNull(result);
            Assert.Equal("Test", result!.Title);
        }

        [Fact]
        public async Task GetScheduleByIdAsync_ReturnsNull_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(99)).ReturnsAsync((InterviewSchedule?)null);
            var result = await _interviewService.GetScheduleByIdAsync(99);
            Assert.Null(result);
        }

        #endregion

        #region UpdateScheduleAsync

        [Fact]
        public async Task UpdateScheduleAsync_ReturnsNull_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(99)).ReturnsAsync((InterviewSchedule?)null);
            var result = await _interviewService.UpdateScheduleAsync(99, new UpdateInterviewScheduleDto());
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateScheduleAsync_ReturnsNull_WhenUpdateFails()
        {
            var schedule = new InterviewSchedule { Id = 1, Title = "Old" };
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(schedule);
            _mockRepo.Setup(r => r.UpdateScheduleAsync(schedule)).ReturnsAsync(false);

            var result = await _interviewService.UpdateScheduleAsync(1, new UpdateInterviewScheduleDto { Title = "New" });
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateScheduleAsync_ReturnsUpdated_WhenSuccess()
        {
            var schedule = new InterviewSchedule { Id = 1, Title = "Old" };
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(schedule);
            _mockRepo.Setup(r => r.UpdateScheduleAsync(schedule)).ReturnsAsync(true);

            var dto = new UpdateInterviewScheduleDto { Title = "New", DurationMinutes = 90 };
            var result = await _interviewService.UpdateScheduleAsync(1, dto);

            Assert.NotNull(result);
            Assert.Equal("New", result!.Title);
        }

        #endregion

        #region DeleteScheduleAsync

        [Fact]
        public async Task DeleteScheduleAsync_ReturnsFalse_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(99)).ReturnsAsync((InterviewSchedule?)null);
            var result = await _interviewService.DeleteScheduleAsync(99);
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteScheduleAsync_Throws_WhenNotScheduledStatus()
        {
            var schedule = new InterviewSchedule { Id = 1, Status = InterviewStatus.Confirmed };
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(schedule);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _interviewService.DeleteScheduleAsync(1));
        }

        [Fact]
        public async Task DeleteScheduleAsync_ReturnsTrue_WhenScheduled()
        {
            var schedule = new InterviewSchedule { Id = 1, Status = InterviewStatus.Scheduled };
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(schedule);
            _mockRepo.Setup(r => r.DeleteScheduleAsync(1)).ReturnsAsync(true);

            var result = await _interviewService.DeleteScheduleAsync(1);
            Assert.True(result);
        }

        #endregion

        #region UpdateScheduleStatusAsync_Additional

        [Fact]
        public async Task UpdateScheduleStatusAsync_ReturnsFalse_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(99)).ReturnsAsync((InterviewSchedule?)null);
            var result = await _interviewService.UpdateScheduleStatusAsync(99, new UpdateInterviewStatusDto { Status = "Confirmed" });
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateScheduleStatusAsync_Cancelled_RequiresCancelReason()
        {
            var schedule = new InterviewSchedule { Status = InterviewStatus.Scheduled };
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(schedule);

            var dto = new UpdateInterviewStatusDto { Status = "Cancelled" };
            await Assert.ThrowsAsync<ArgumentException>(() => _interviewService.UpdateScheduleStatusAsync(1, dto));
        }

        [Fact]
        public async Task UpdateScheduleStatusAsync_Cancelled_WithReason_Succeeds()
        {
            var schedule = new InterviewSchedule { Status = InterviewStatus.Scheduled };
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(schedule);
            _mockRepo.Setup(r => r.UpdateScheduleAsync(schedule)).ReturnsAsync(true);

            var dto = new UpdateInterviewStatusDto { Status = "Cancelled", CancelReason = "No show" };
            var result = await _interviewService.UpdateScheduleStatusAsync(1, dto);
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateScheduleStatusAsync_InProgress_FromConfirmed()
        {
            var schedule = new InterviewSchedule { Status = InterviewStatus.Confirmed };
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(schedule);
            _mockRepo.Setup(r => r.UpdateScheduleAsync(schedule)).ReturnsAsync(true);

            var result = await _interviewService.UpdateScheduleStatusAsync(1, new UpdateInterviewStatusDto { Status = "InProgress" });
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateScheduleStatusAsync_Completed_FromInProgress()
        {
            var schedule = new InterviewSchedule { Status = InterviewStatus.InProgress };
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(schedule);
            _mockRepo.Setup(r => r.UpdateScheduleAsync(schedule)).ReturnsAsync(true);

            var result = await _interviewService.UpdateScheduleStatusAsync(1, new UpdateInterviewStatusDto { Status = "Completed" });
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateScheduleStatusAsync_Completed_ThrowsWhenNotInProgress()
        {
            var schedule = new InterviewSchedule { Status = InterviewStatus.Scheduled };
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(schedule);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _interviewService.UpdateScheduleStatusAsync(1, new UpdateInterviewStatusDto { Status = "Completed" }));
        }

        [Fact]
        public async Task UpdateScheduleStatusAsync_Rescheduled_ThrowsFromCompleted()
        {
            var schedule = new InterviewSchedule { Status = InterviewStatus.Completed };
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(schedule);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _interviewService.UpdateScheduleStatusAsync(1, new UpdateInterviewStatusDto { Status = "Rescheduled" }));
        }

        #endregion

        #region AssignInterviewersAsync

        [Fact]
        public async Task AssignInterviewersAsync_Throws_WhenScheduleNotFound()
        {
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(99)).ReturnsAsync((InterviewSchedule?)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _interviewService.AssignInterviewersAsync(99, new AssignInterviewersDto()));
        }

        [Fact]
        public async Task AssignInterviewersAsync_CreatesAssignments()
        {
            var schedule = new InterviewSchedule { Id = 1 };
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(schedule);
            _mockRepo.Setup(r => r.CreateAssignmentAsync(It.IsAny<InterviewAssignment>()))
                     .ReturnsAsync(new InterviewAssignment { Id = 1 });

            var dto = new AssignInterviewersDto
            {
                Interviewers = new List<AssignInterviewerItemDto>
                {
                    new AssignInterviewerItemDto { InterviewerUserId = Guid.NewGuid(), Role = "Lead" }
                }
            };

            var result = await _interviewService.AssignInterviewersAsync(1, dto);
            Assert.Single(result);
        }

        #endregion

        #region GetAssignmentsAsync

        [Fact]
        public async Task GetAssignmentsAsync_ReturnsMappedDtos()
        {
            var assignments = new List<InterviewAssignment>
            {
                new InterviewAssignment { Id = 1, InterviewScheduleId = 1 }
            };
            _mockRepo.Setup(r => r.GetAssignmentsByScheduleIdAsync(1)).ReturnsAsync(assignments);

            var result = await _interviewService.GetAssignmentsAsync(1);
            Assert.Single(result);
        }

        #endregion

        #region RemoveAssignmentAsync

        [Fact]
        public async Task RemoveAssignmentAsync_ReturnsFalse_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetAssignmentByIdAsync(99)).ReturnsAsync((InterviewAssignment?)null);
            Assert.False(await _interviewService.RemoveAssignmentAsync(1, 99));
        }

        [Fact]
        public async Task RemoveAssignmentAsync_ReturnsFalse_WhenScheduleMismatch()
        {
            _mockRepo.Setup(r => r.GetAssignmentByIdAsync(1))
                     .ReturnsAsync(new InterviewAssignment { InterviewScheduleId = 2 });
            Assert.False(await _interviewService.RemoveAssignmentAsync(1, 1));
        }

        [Fact]
        public async Task RemoveAssignmentAsync_ReturnsTrue_WhenValid()
        {
            _mockRepo.Setup(r => r.GetAssignmentByIdAsync(1))
                     .ReturnsAsync(new InterviewAssignment { InterviewScheduleId = 1 });
            _mockRepo.Setup(r => r.DeleteAssignmentAsync(1)).ReturnsAsync(true);

            Assert.True(await _interviewService.RemoveAssignmentAsync(1, 1));
        }

        #endregion

        #region ConfirmAssignmentAsync

        [Fact]
        public async Task ConfirmAssignmentAsync_ReturnsFalse_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetAssignmentByIdAsync(99)).ReturnsAsync((InterviewAssignment?)null);
            Assert.False(await _interviewService.ConfirmAssignmentAsync(1, 99));
        }

        [Fact]
        public async Task ConfirmAssignmentAsync_ReturnsTrue_WhenValid()
        {
            var assignment = new InterviewAssignment { Id = 1, InterviewScheduleId = 1 };
            _mockRepo.Setup(r => r.GetAssignmentByIdAsync(1)).ReturnsAsync(assignment);
            _mockRepo.Setup(r => r.UpdateAssignmentAsync(assignment)).ReturnsAsync(true);

            Assert.True(await _interviewService.ConfirmAssignmentAsync(1, 1));
            Assert.True(assignment.HasConfirmed);
        }

        #endregion

        #region Room Methods

        [Fact]
        public async Task GetRoomByIdAsync_ReturnsNull_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetRoomByIdAsync(99)).ReturnsAsync((MeetingRoom?)null);
            Assert.Null(await _interviewService.GetRoomByIdAsync(99));
        }

        [Fact]
        public async Task GetRoomByIdAsync_ReturnsDto_WhenFound()
        {
            _mockRepo.Setup(r => r.GetRoomByIdAsync(1)).ReturnsAsync(new MeetingRoom { Id = 1, RoomCode = "abc" });
            var result = await _interviewService.GetRoomByIdAsync(1);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetRoomByCodeAsync_ReturnsNull_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetRoomByCodeAsync("nope")).ReturnsAsync((MeetingRoom?)null);
            Assert.Null(await _interviewService.GetRoomByCodeAsync("nope"));
        }

        [Fact]
        public async Task GetRoomByScheduleIdAsync_ReturnsNull_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetRoomByScheduleIdAsync(99)).ReturnsAsync((MeetingRoom?)null);
            Assert.Null(await _interviewService.GetRoomByScheduleIdAsync(99));
        }

        [Fact]
        public async Task CreateStandaloneRoomAsync_ReturnsDto()
        {
            var created = new MeetingRoom { Id = 1, RoomCode = "test-code" };
            _mockRepo.Setup(r => r.CreateRoomAsync(It.IsAny<MeetingRoom>())).ReturnsAsync(created);

            var dto = new CreateMeetingRoomDto
            {
                Title = "Standalone Room",
                CreatedByUserId = Guid.NewGuid(),
                RoomType = "General"
            };

            var result = await _interviewService.CreateStandaloneRoomAsync(dto);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CloseRoomAsync_ReturnsFalse_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetRoomByCodeAsync("nope")).ReturnsAsync((MeetingRoom?)null);
            Assert.False(await _interviewService.CloseRoomAsync("nope"));
        }

        [Fact]
        public async Task CloseRoomAsync_ReturnsTrue_WhenAlreadyClosed()
        {
            _mockRepo.Setup(r => r.GetRoomByCodeAsync("c")).ReturnsAsync(new MeetingRoom { Status = RoomStatus.Closed });
            Assert.True(await _interviewService.CloseRoomAsync("c"));
        }

        [Fact]
        public async Task CloseRoomAsync_ClosesAndLogsEvent()
        {
            var room = new MeetingRoom { Id = 1, Status = RoomStatus.Waiting };
            _mockRepo.Setup(r => r.GetRoomByCodeAsync("code")).ReturnsAsync(room);
            _mockRepo.Setup(r => r.UpdateRoomAsync(room)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.CreateEventAsync(It.IsAny<RoomEvent>())).ReturnsAsync(new RoomEvent());

            Assert.True(await _interviewService.CloseRoomAsync("code"));
            Assert.Equal(RoomStatus.Closed, room.Status);
        }

        [Fact]
        public async Task GetParticipantsAsync_Throws_WhenRoomNotFound()
        {
            _mockRepo.Setup(r => r.GetRoomByCodeAsync("nope")).ReturnsAsync((MeetingRoom?)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _interviewService.GetParticipantsAsync("nope"));
        }

        [Fact]
        public async Task GetEventsAsync_Throws_WhenRoomNotFound()
        {
            _mockRepo.Setup(r => r.GetRoomByCodeAsync("nope")).ReturnsAsync((MeetingRoom?)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _interviewService.GetEventsAsync("nope"));
        }

        [Fact]
        public async Task GetParticipantsAsync_ReturnsList()
        {
            var room = new MeetingRoom { Id = 1 };
            _mockRepo.Setup(r => r.GetRoomByCodeAsync("code")).ReturnsAsync(room);
            _mockRepo.Setup(r => r.GetParticipantsByRoomIdAsync(1)).ReturnsAsync(new List<RoomParticipant>());

            var result = await _interviewService.GetParticipantsAsync("code");
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetEventsAsync_ReturnsList()
        {
            var room = new MeetingRoom { Id = 1 };
            _mockRepo.Setup(r => r.GetRoomByCodeAsync("code")).ReturnsAsync(room);
            _mockRepo.Setup(r => r.GetEventsByRoomIdAsync(1)).ReturnsAsync(new List<RoomEvent>());

            var result = await _interviewService.GetEventsAsync("code");
            Assert.Empty(result);
        }

        #endregion

        #region LeaveRoomAsync_Additional

        [Fact]
        public async Task LeaveRoomAsync_ReturnsFalse_WhenRoomNotFound()
        {
            _mockRepo.Setup(r => r.GetRoomByCodeAsync("nope")).ReturnsAsync((MeetingRoom?)null);
            Assert.False(await _interviewService.LeaveRoomAsync("nope", new LeaveRoomDto()));
        }

        #endregion

        #region GetFeedbackSummaryAsync

        [Fact]
        public async Task GetFeedbackSummaryAsync_ReturnsNull_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(99)).ReturnsAsync((InterviewSchedule?)null);
            Assert.Null(await _interviewService.GetFeedbackSummaryAsync(99));
        }

        [Fact]
        public async Task GetFeedbackSummaryAsync_ReturnsDto_WhenFound()
        {
            var schedule = new InterviewSchedule
            {
                Id = 1,
                Title = "Test",
                Assignments = new List<InterviewAssignment>
                {
                    new InterviewAssignment { Id = 1, InterviewScheduleId = 1 }
                }
            };
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(1)).ReturnsAsync(schedule);

            var result = await _interviewService.GetFeedbackSummaryAsync(1);
            Assert.NotNull(result);
            Assert.Single(result!.Feedbacks);
        }

        #endregion

        #region SubmitFeedbackAsync_Additional

        [Fact]
        public async Task SubmitFeedbackAsync_ReturnsFalse_WhenAssignmentNotFound()
        {
            _mockRepo.Setup(r => r.GetAssignmentByIdAsync(99)).ReturnsAsync((InterviewAssignment?)null);
            Assert.False(await _interviewService.SubmitFeedbackAsync(1, 99, new SubmitFeedbackDto { Result = "Pass" }));
        }

        [Fact]
        public async Task SubmitFeedbackAsync_ThrowsOnInvalidResult()
        {
            var assignment = new InterviewAssignment { InterviewScheduleId = 1 };
            _mockRepo.Setup(r => r.GetAssignmentByIdAsync(1)).ReturnsAsync(assignment);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _interviewService.SubmitFeedbackAsync(1, 1, new SubmitFeedbackDto { Result = "INVALID" }));
        }

        #endregion

        #region EvaluationCriteria

        [Fact]
        public async Task GetCampaignCriteriaAsync_SeedsDefaults_WhenEmpty()
        {
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1))
                     .ReturnsAsync(new List<EvaluationCriterion>());
            _mockRepo.Setup(r => r.CreateCriterionAsync(It.IsAny<EvaluationCriterion>()))
                     .ReturnsAsync((EvaluationCriterion c) => c);

            var result = await _interviewService.GetCampaignCriteriaAsync(1);
            Assert.Equal(5, result.Count); // 5 default criteria
        }

        [Fact]
        public async Task GetCampaignCriteriaAsync_ReturnsExisting_WhenNotEmpty()
        {
            var criteria = new List<EvaluationCriterion>
            {
                new EvaluationCriterion { Id = 1, CampaignId = 1, Name = "Custom" }
            };
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(criteria);

            var result = await _interviewService.GetCampaignCriteriaAsync(1);
            Assert.Single(result);
        }

        [Fact]
        public async Task CreateCriterionAsync_ReturnsDto()
        {
            _mockRepo.Setup(r => r.CreateCriterionAsync(It.IsAny<EvaluationCriterion>()))
                     .ReturnsAsync((EvaluationCriterion c) => { c.Id = 10; return c; });

            var result = await _interviewService.CreateCriterionAsync(1, new CreateEvaluationCriterionDto
            {
                Name = "Test"
            });

            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public async Task UpdateCriterionAsync_ReturnsNull_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetCriterionByIdAsync(99)).ReturnsAsync((EvaluationCriterion?)null);
            Assert.Null(await _interviewService.UpdateCriterionAsync(99, new UpdateEvaluationCriterionDto()));
        }

        [Fact]
        public async Task UpdateCriterionAsync_UpdatesAndReturnsDto()
        {
            var criterion = new EvaluationCriterion { Id = 1, Name = "Old" };
            _mockRepo.Setup(r => r.GetCriterionByIdAsync(1)).ReturnsAsync(criterion);
            _mockRepo.Setup(r => r.UpdateCriterionAsync(criterion)).ReturnsAsync(true);

            var result = await _interviewService.UpdateCriterionAsync(1, new UpdateEvaluationCriterionDto { Name = "New" });
            Assert.Equal("New", result!.Name);
        }

        [Fact]
        public async Task DeleteCriterionAsync_DelegatesToRepo()
        {
            _mockRepo.Setup(r => r.DeleteCriterionAsync(1)).ReturnsAsync(true);
            Assert.True(await _interviewService.DeleteCriterionAsync(1));
        }

        #endregion

        #region AssignCriteriaToInterviewerAsync

        [Fact]
        public async Task AssignCriteriaToInterviewerAsync_ReturnsFalse_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetAssignmentByIdAsync(99)).ReturnsAsync((InterviewAssignment?)null);
            Assert.False(await _interviewService.AssignCriteriaToInterviewerAsync(1, 99, new AssignCriteriaDto()));
        }

        [Fact]
        public async Task AssignCriteriaToInterviewerAsync_CreatesScores()
        {
            var assignment = new InterviewAssignment { InterviewScheduleId = 1 };
            _mockRepo.Setup(r => r.GetAssignmentByIdAsync(1)).ReturnsAsync(assignment);
            _mockRepo.Setup(r => r.DeleteCriteriaScoresByAssignmentIdAsync(1)).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.CreateCriteriaScoreAsync(It.IsAny<CriteriaScore>()))
                     .ReturnsAsync(new CriteriaScore());

            var dto = new AssignCriteriaDto { CriteriaIds = new List<int> { 1, 2, 3 } };
            Assert.True(await _interviewService.AssignCriteriaToInterviewerAsync(1, 1, dto));
            _mockRepo.Verify(r => r.CreateCriteriaScoreAsync(It.IsAny<CriteriaScore>()), Times.Exactly(3));
        }

        #endregion

        #region CreateScheduleAsync_Additional

        [Fact]
        public async Task CreateScheduleAsync_WithoutInterviewers()
        {
            var dto = new CreateInterviewScheduleDto
            {
                ApplicationId = 1, CandidateUserId = Guid.NewGuid(), Title = "No Interviewers",
                Interviewers = null
            };

            var created = new InterviewSchedule { Id = 5, Title = "No Interviewers" };
            _mockRepo.Setup(r => r.CreateScheduleAsync(It.IsAny<InterviewSchedule>())).ReturnsAsync(created);
            _mockRepo.Setup(r => r.CreateRoomAsync(It.IsAny<MeetingRoom>())).ReturnsAsync(new MeetingRoom());
            _mockRepo.Setup(r => r.GetScheduleByIdAsync(5)).ReturnsAsync(created);

            var result = await _interviewService.CreateScheduleAsync(dto);
            Assert.NotNull(result);
            _mockRepo.Verify(r => r.CreateAssignmentAsync(It.IsAny<InterviewAssignment>()), Times.Never);
        }

        #endregion
    }
}
