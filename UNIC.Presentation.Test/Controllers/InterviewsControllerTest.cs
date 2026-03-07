using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Presentation.Controllers;
using Xunit;

namespace UNIC.Presentation.Test.Controllers
{
    public class InterviewsControllerTest
    {
        private readonly Mock<IInterviewService> _mockService;
        private readonly InterviewsController _controller;

        public InterviewsControllerTest()
        {
            _mockService = new Mock<IInterviewService>();
            _controller = new InterviewsController(_mockService.Object);
        }

        #region Create

        [Fact]
        public async Task Create_ReturnsCreated_WhenSuccess()
        {
            var dto = new CreateInterviewScheduleDto { ApplicationId = 1, Title = "T", ScheduledAt = DateTime.Now, CandidateUserId = Guid.NewGuid(), CampaignId = 1, CreatedByUserId = Guid.NewGuid() };
            _mockService.Setup(s => s.CreateScheduleAsync(dto))
                .ReturnsAsync(new InterviewScheduleResponseDto { Id = 1 });

            var result = await _controller.Create(dto);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new CreateInterviewScheduleDto();
            _mockService.Setup(s => s.CreateScheduleAsync(dto))
                .ThrowsAsync(new Exception("Application not found"));

            var result = await _controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region GetAll

        [Fact]
        public async Task GetAll_ReturnsOk()
        {
            _mockService.Setup(s => s.GetSchedulesAsync(null, null, null, null))
                .ReturnsAsync(new List<InterviewScheduleResponseDto> { new() });

            var result = await _controller.GetAll(null, null, null, null);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region GetById

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetScheduleByIdAsync(1))
                .ReturnsAsync(new InterviewScheduleResponseDto { Id = 1 });

            var result = await _controller.GetById(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetScheduleByIdAsync(99))
                .ReturnsAsync((InterviewScheduleResponseDto?)null);

            var result = await _controller.GetById(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region Update

        [Fact]
        public async Task Update_ReturnsOk_WhenSuccess()
        {
            var dto = new UpdateInterviewScheduleDto();
            _mockService.Setup(s => s.UpdateScheduleAsync(1, dto))
                .ReturnsAsync(new InterviewScheduleResponseDto { Id = 1 });

            var result = await _controller.Update(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenNull()
        {
            var dto = new UpdateInterviewScheduleDto();
            _mockService.Setup(s => s.UpdateScheduleAsync(99, dto))
                .ReturnsAsync((InterviewScheduleResponseDto?)null);

            var result = await _controller.Update(99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new UpdateInterviewScheduleDto();
            _mockService.Setup(s => s.UpdateScheduleAsync(1, dto))
                .ThrowsAsync(new Exception("Service error"));

            var result = await _controller.Update(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region UpdateStatus

        [Fact]
        public async Task UpdateStatus_ReturnsOk_WhenSuccess()
        {
            var dto = new UpdateInterviewStatusDto { Status = "Confirmed" };
            _mockService.Setup(s => s.UpdateScheduleStatusAsync(1, dto)).ReturnsAsync(true);

            var result = await _controller.UpdateStatus(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateStatus_ReturnsNotFound_WhenMissing()
        {
            var dto = new UpdateInterviewStatusDto { Status = "Confirmed" };
            _mockService.Setup(s => s.UpdateScheduleStatusAsync(99, dto)).ReturnsAsync(false);

            var result = await _controller.UpdateStatus(99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateStatus_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new UpdateInterviewStatusDto { Status = "Bad" };
            _mockService.Setup(s => s.UpdateScheduleStatusAsync(1, dto))
                .ThrowsAsync(new Exception("Invalid transition"));

            var result = await _controller.UpdateStatus(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region Delete

        [Fact]
        public async Task Delete_ReturnsOk_WhenSuccess()
        {
            _mockService.Setup(s => s.DeleteScheduleAsync(1)).ReturnsAsync(true);

            var result = await _controller.Delete(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.DeleteScheduleAsync(99)).ReturnsAsync(false);

            var result = await _controller.Delete(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsBadRequest_WhenServiceThrows()
        {
            _mockService.Setup(s => s.DeleteScheduleAsync(1))
                .ThrowsAsync(new Exception("Cannot delete"));

            var result = await _controller.Delete(1);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region AssignInterviewers

        [Fact]
        public async Task AssignInterviewers_ReturnsOk_WhenSuccess()
        {
            var dto = new AssignInterviewersDto();
            _mockService.Setup(s => s.AssignInterviewersAsync(1, dto))
                .ReturnsAsync(new List<InterviewAssignmentResponseDto> { new() });

            var result = await _controller.AssignInterviewers(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AssignInterviewers_ReturnsNotFound_WhenKeyNotFound()
        {
            var dto = new AssignInterviewersDto();
            _mockService.Setup(s => s.AssignInterviewersAsync(99, dto))
                .ThrowsAsync(new KeyNotFoundException("Schedule not found"));

            var result = await _controller.AssignInterviewers(99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetAssignments

        [Fact]
        public async Task GetAssignments_ReturnsOk()
        {
            _mockService.Setup(s => s.GetAssignmentsAsync(1))
                .ReturnsAsync(new List<InterviewAssignmentResponseDto> { new() });

            var result = await _controller.GetAssignments(1);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region RemoveAssignment

        [Fact]
        public async Task RemoveAssignment_ReturnsOk_WhenSuccess()
        {
            _mockService.Setup(s => s.RemoveAssignmentAsync(1, 1)).ReturnsAsync(true);

            var result = await _controller.RemoveAssignment(1, 1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task RemoveAssignment_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.RemoveAssignmentAsync(1, 99)).ReturnsAsync(false);

            var result = await _controller.RemoveAssignment(1, 99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region ConfirmAssignment

        [Fact]
        public async Task ConfirmAssignment_ReturnsOk_WhenSuccess()
        {
            _mockService.Setup(s => s.ConfirmAssignmentAsync(1, 1)).ReturnsAsync(true);

            var result = await _controller.ConfirmAssignment(1, 1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ConfirmAssignment_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.ConfirmAssignmentAsync(1, 99)).ReturnsAsync(false);

            var result = await _controller.ConfirmAssignment(1, 99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetRoom

        [Fact]
        public async Task GetRoom_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetRoomByScheduleIdAsync(1))
                .ReturnsAsync(new MeetingRoomResponseDto { RoomCode = "ABC" });

            var result = await _controller.GetRoom(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetRoom_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetRoomByScheduleIdAsync(99))
                .ReturnsAsync((MeetingRoomResponseDto?)null);

            var result = await _controller.GetRoom(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region SubmitFeedback / GetFeedbackSummary

        [Fact]
        public async Task SubmitFeedback_ReturnsOk_WhenSuccess()
        {
            var dto = new SubmitFeedbackDto { Result = "Pass" };
            _mockService.Setup(s => s.SubmitFeedbackAsync(1, 1, dto)).ReturnsAsync(true);

            var result = await _controller.SubmitFeedback(1, 1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task SubmitFeedback_ReturnsNotFound_WhenMissing()
        {
            var dto = new SubmitFeedbackDto { Result = "Fail" };
            _mockService.Setup(s => s.SubmitFeedbackAsync(1, 99, dto)).ReturnsAsync(false);

            var result = await _controller.SubmitFeedback(1, 99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetFeedbackSummary_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetFeedbackSummaryAsync(1))
                .ReturnsAsync(new FeedbackSummaryResponseDto { InterviewScheduleId = 1 });

            var result = await _controller.GetFeedbackSummary(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetFeedbackSummary_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetFeedbackSummaryAsync(99))
                .ReturnsAsync((FeedbackSummaryResponseDto?)null);

            var result = await _controller.GetFeedbackSummary(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion
    }
}
