using BusinessLogic.DTOs;
using BusinessLogic.Services.Implementation;
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
        private readonly InterviewService _interviewService;

        public InterviewServiceTest()
        {
            _mockRepo = new Mock<IInterviewRepository>();
            _interviewService = new InterviewService(_mockRepo.Object);
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
    }
}
