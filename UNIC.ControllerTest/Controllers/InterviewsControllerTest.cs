using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Controllers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ControllerTest.Controllers
{
    public class InterviewsControllerTest
    {
        private readonly Mock<IInterviewService> _mockService;
        private readonly Mock<IAiAnalysisService> _mockAiService;
        private readonly InterviewsController _controller;

        public InterviewsControllerTest()
        {
            _mockService = new Mock<IInterviewService>();
            _mockAiService = new Mock<IAiAnalysisService>();
            _controller = new InterviewsController(_mockService.Object, _mockAiService.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region Create

        [Fact]
        public async Task Create_ReturnsCreated_WhenSuccess()
        {
            var dto = new CreateInterviewScheduleDto { Title = "Test Interview" };
            var response = new InterviewScheduleResponseDto { Id = 1, Title = "Test Interview" };

            _mockService.Setup(s => s.CreateScheduleAsync(dto))
                        .ReturnsAsync(response);

            var result = await _controller.Create(dto);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new CreateInterviewScheduleDto { Title = "Bad" };

            _mockService.Setup(s => s.CreateScheduleAsync(dto))
                        .ThrowsAsync(new Exception("Invalid data"));

            var result = await _controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region GetAll

        [Fact]
        public async Task GetAll_ReturnsOk()
        {
            var schedules = new List<InterviewScheduleResponseDto>
            {
                new InterviewScheduleResponseDto { Id = 1, Title = "Schedule 1" }
            };

            _mockService.Setup(s => s.GetSchedulesAsync(null, null, null, null))
                        .ReturnsAsync(schedules);

            var result = await _controller.GetAll(null, null, null, null);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region GetById

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var schedule = new InterviewScheduleResponseDto { Id = 1, Title = "Found" };

            _mockService.Setup(s => s.GetScheduleByIdAsync(1))
                        .ReturnsAsync(schedule);

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
            var dto = new UpdateInterviewScheduleDto { Title = "Updated" };
            var response = new InterviewScheduleResponseDto { Id = 1, Title = "Updated" };

            _mockService.Setup(s => s.UpdateScheduleAsync(1, dto))
                        .ReturnsAsync(response);

            var result = await _controller.Update(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenNull()
        {
            var dto = new UpdateInterviewScheduleDto { Title = "Missing" };

            _mockService.Setup(s => s.UpdateScheduleAsync(99, dto))
                        .ReturnsAsync((InterviewScheduleResponseDto?)null);

            var result = await _controller.Update(99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new UpdateInterviewScheduleDto { Title = "Bad" };

            _mockService.Setup(s => s.UpdateScheduleAsync(1, dto))
                        .ThrowsAsync(new Exception("Error"));

            var result = await _controller.Update(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region UpdateStatus

        [Fact]
        public async Task UpdateStatus_ReturnsOk_WhenSuccess()
        {
            var dto = new UpdateInterviewStatusDto { Status = "Confirmed" };

            _mockService.Setup(s => s.UpdateScheduleStatusAsync(1, dto))
                        .ReturnsAsync(true);

            var result = await _controller.UpdateStatus(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateStatus_ReturnsNotFound_WhenMissing()
        {
            var dto = new UpdateInterviewStatusDto { Status = "Confirmed" };

            _mockService.Setup(s => s.UpdateScheduleStatusAsync(99, dto))
                        .ReturnsAsync(false);

            var result = await _controller.UpdateStatus(99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateStatus_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new UpdateInterviewStatusDto { Status = "Invalid" };

            _mockService.Setup(s => s.UpdateScheduleStatusAsync(1, dto))
                        .ThrowsAsync(new ArgumentException("Invalid status"));

            var result = await _controller.UpdateStatus(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region Delete

        [Fact]
        public async Task Delete_ReturnsOk_WhenSuccess()
        {
            _mockService.Setup(s => s.DeleteScheduleAsync(1))
                        .ReturnsAsync(true);

            var result = await _controller.Delete(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.DeleteScheduleAsync(99))
                        .ReturnsAsync(false);

            var result = await _controller.Delete(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsBadRequest_WhenServiceThrows()
        {
            _mockService.Setup(s => s.DeleteScheduleAsync(1))
                        .ThrowsAsync(new InvalidOperationException("Cannot delete"));

            var result = await _controller.Delete(1);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region AssignInterviewers

        [Fact]
        public async Task AssignInterviewers_ReturnsOk_WhenSuccess()
        {
            var dto = new AssignInterviewersDto
            {
                Interviewers = new List<AssignInterviewerItemDto>
                {
                    new AssignInterviewerItemDto { InterviewerUserId = Guid.NewGuid(), Role = "Interviewer" }
                }
            };
            var assignments = new List<InterviewAssignmentResponseDto>
            {
                new InterviewAssignmentResponseDto { Id = 1 }
            };

            _mockService.Setup(s => s.AssignInterviewersAsync(1, dto))
                        .ReturnsAsync(assignments);

            var result = await _controller.AssignInterviewers(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AssignInterviewers_ReturnsNotFound_WhenKeyNotFound()
        {
            var dto = new AssignInterviewersDto { Interviewers = new List<AssignInterviewerItemDto>() };

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
            var assignments = new List<InterviewAssignmentResponseDto> { new InterviewAssignmentResponseDto() };

            _mockService.Setup(s => s.GetAssignmentsAsync(1))
                        .ReturnsAsync(assignments);

            var result = await _controller.GetAssignments(1);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region RemoveAssignment

        [Fact]
        public async Task RemoveAssignment_ReturnsOk_WhenSuccess()
        {
            _mockService.Setup(s => s.RemoveAssignmentAsync(1, 2))
                        .ReturnsAsync(true);

            var result = await _controller.RemoveAssignment(1, 2);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task RemoveAssignment_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.RemoveAssignmentAsync(1, 99))
                        .ReturnsAsync(false);

            var result = await _controller.RemoveAssignment(1, 99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region ConfirmAssignment

        [Fact]
        public async Task ConfirmAssignment_ReturnsOk_WhenSuccess()
        {
            _mockService.Setup(s => s.ConfirmAssignmentAsync(1, 2))
                        .ReturnsAsync(true);

            var result = await _controller.ConfirmAssignment(1, 2);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ConfirmAssignment_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.ConfirmAssignmentAsync(1, 99))
                        .ReturnsAsync(false);

            var result = await _controller.ConfirmAssignment(1, 99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetRoom

        [Fact]
        public async Task GetRoom_ReturnsOk_WhenFound()
        {
            var room = new MeetingRoomResponseDto { Id = 1, RoomCode = "abc-1234" };

            _mockService.Setup(s => s.GetRoomByScheduleIdAsync(1))
                        .ReturnsAsync(room);

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

        #region SubmitFeedback

        [Fact]
        public async Task SubmitFeedback_ReturnsOk_WhenSuccess()
        {
            var dto = new SubmitFeedbackDto { Result = "Pass", FeedbackNotes = "Good" };

            _mockService.Setup(s => s.SubmitFeedbackAsync(1, 2, dto))
                        .ReturnsAsync(true);

            var result = await _controller.SubmitFeedback(1, 2, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task SubmitFeedback_ReturnsNotFound_WhenMissing()
        {
            var dto = new SubmitFeedbackDto { Result = "Pass" };

            _mockService.Setup(s => s.SubmitFeedbackAsync(1, 99, dto))
                        .ReturnsAsync(false);

            var result = await _controller.SubmitFeedback(1, 99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetFeedbackSummary

        [Fact]
        public async Task GetFeedbackSummary_ReturnsOk_WhenFound()
        {
            var summary = new FeedbackSummaryResponseDto { InterviewScheduleId = 1 };

            _mockService.Setup(s => s.GetFeedbackSummaryAsync(1))
                        .ReturnsAsync(summary);

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

        #region GetCampaignCriteria

        [Fact]
        public async Task GetCampaignCriteria_ReturnsOk()
        {
            var criteria = new List<EvaluationCriterionDto>
            {
                new EvaluationCriterionDto { Id = 1, Name = "Communication" }
            };

            _mockService.Setup(s => s.GetCampaignCriteriaAsync(1))
                        .ReturnsAsync(criteria);

            var result = await _controller.GetCampaignCriteria(1);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region CreateCriterion

        [Fact]
        public async Task CreateCriterion_ReturnsOk_WhenSuccess()
        {
            var dto = new CreateEvaluationCriterionDto { Name = "Teamwork" };
            var response = new EvaluationCriterionDto { Id = 1, Name = "Teamwork" };

            _mockService.Setup(s => s.CreateCriterionAsync(1, dto))
                        .ReturnsAsync(response);

            var result = await _controller.CreateCriterion(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task CreateCriterion_ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("Name", "Required");

            var result = await _controller.CreateCriterion(1, new CreateEvaluationCriterionDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region UpdateCriterion

        [Fact]
        public async Task UpdateCriterion_ReturnsOk_WhenSuccess()
        {
            var dto = new UpdateEvaluationCriterionDto { Name = "Updated" };
            var response = new EvaluationCriterionDto { Id = 1, Name = "Updated" };

            _mockService.Setup(s => s.UpdateCriterionAsync(1, dto))
                        .ReturnsAsync(response);

            var result = await _controller.UpdateCriterion(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCriterion_ReturnsNotFound_WhenNull()
        {
            var dto = new UpdateEvaluationCriterionDto { Name = "Missing" };

            _mockService.Setup(s => s.UpdateCriterionAsync(99, dto))
                        .ReturnsAsync((EvaluationCriterionDto?)null);

            var result = await _controller.UpdateCriterion(99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region DeleteCriterion

        [Fact]
        public async Task DeleteCriterion_ReturnsOk_WhenSuccess()
        {
            _mockService.Setup(s => s.DeleteCriterionAsync(1))
                        .ReturnsAsync(true);

            var result = await _controller.DeleteCriterion(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteCriterion_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.DeleteCriterionAsync(99))
                        .ReturnsAsync(false);

            var result = await _controller.DeleteCriterion(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region AssignCriteria

        [Fact]
        public async Task AssignCriteria_ReturnsOk_WhenSuccess()
        {
            var dto = new AssignCriteriaDto { CriteriaIds = new List<int> { 1, 2 } };

            _mockService.Setup(s => s.AssignCriteriaToInterviewerAsync(1, 2, dto))
                        .ReturnsAsync(true);

            var result = await _controller.AssignCriteria(1, 2, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AssignCriteria_ReturnsNotFound_WhenMissing()
        {
            var dto = new AssignCriteriaDto { CriteriaIds = new List<int> { 1 } };

            _mockService.Setup(s => s.AssignCriteriaToInterviewerAsync(1, 99, dto))
                        .ReturnsAsync(false);

            var result = await _controller.AssignCriteria(1, 99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task AssignCriteria_ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("CriterionIds", "Required");

            var result = await _controller.AssignCriteria(1, 2, new AssignCriteriaDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region SubmitCriteriaFeedback

        [Fact]
        public async Task SubmitCriteriaFeedback_ReturnsOk_WhenSuccess()
        {
            var dto = new SubmitCriteriaFeedbackDto();

            _mockService.Setup(s => s.SubmitCriteriaFeedbackAsync(1, 2, dto))
                        .ReturnsAsync(true);

            var result = await _controller.SubmitCriteriaFeedback(1, 2, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task SubmitCriteriaFeedback_ReturnsNotFound_WhenMissing()
        {
            var dto = new SubmitCriteriaFeedbackDto();

            _mockService.Setup(s => s.SubmitCriteriaFeedbackAsync(1, 99, dto))
                        .ReturnsAsync(false);

            var result = await _controller.SubmitCriteriaFeedback(1, 99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task SubmitCriteriaFeedback_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new SubmitCriteriaFeedbackDto();

            _mockService.Setup(s => s.SubmitCriteriaFeedbackAsync(1, 2, dto))
                        .ThrowsAsync(new Exception("Error"));

            var result = await _controller.SubmitCriteriaFeedback(1, 2, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SubmitCriteriaFeedback_ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("Error", "Invalid");

            var result = await _controller.SubmitCriteriaFeedback(1, 2, new SubmitCriteriaFeedbackDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region GetEvaluationSummary

        [Fact]
        public async Task GetEvaluationSummary_ReturnsOk_WhenFound()
        {
            var summary = new EvaluationSummaryDto { InterviewScheduleId = 1 };

            _mockService.Setup(s => s.GetEvaluationSummaryAsync(1))
                        .ReturnsAsync(summary);

            var result = await _controller.GetEvaluationSummary(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetEvaluationSummary_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetEvaluationSummaryAsync(99))
                        .ReturnsAsync((EvaluationSummaryDto?)null);

            var result = await _controller.GetEvaluationSummary(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetCampaignComparison

        [Fact]
        public async Task GetCampaignComparison_ReturnsOk()
        {
            var comparison = new List<CandidateComparisonItemDto>();

            _mockService.Setup(s => s.GetCampaignComparisonAsync(1))
                        .ReturnsAsync(comparison);

            var result = await _controller.GetCampaignComparison(1);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region SubmitDecisions

        [Fact]
        public async Task SubmitDecisions_ReturnsOk_WhenSuccess()
        {
            var dto = new SubmitDecisionsDto
            {
                DecidedByUserId = Guid.NewGuid(),
                Decisions = new List<CampaignDecisionItemDto>
                {
                    new CampaignDecisionItemDto { InterviewScheduleId = 1, CandidateUserId = Guid.NewGuid(), Decision = "Accept" }
                }
            };
            var result_data = new List<CampaignDecisionResponseDto> { new CampaignDecisionResponseDto() };

            _mockService.Setup(s => s.SubmitDecisionsAsync(1, dto))
                        .ReturnsAsync(result_data);

            var result = await _controller.SubmitDecisions(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task SubmitDecisions_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new SubmitDecisionsDto { Decisions = new List<CampaignDecisionItemDto>() };

            _mockService.Setup(s => s.SubmitDecisionsAsync(1, dto))
                        .ThrowsAsync(new ArgumentException("Invalid decision"));

            var result = await _controller.SubmitDecisions(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SubmitDecisions_ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("Decisions", "Required");

            var result = await _controller.SubmitDecisions(1, new SubmitDecisionsDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region PublishResults

        [Fact]
        public async Task PublishResults_ReturnsOk_WhenSuccess()
        {
            var dto = new PublishResultDto { Mode = "Now" };
            var response = new PublishStatusResponseDto { CampaignId = 1 };

            _mockService.Setup(s => s.PublishResultsAsync(1, dto))
                        .ReturnsAsync(response);

            var result = await _controller.PublishResults(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task PublishResults_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new PublishResultDto { Mode = "Schedule" };

            _mockService.Setup(s => s.PublishResultsAsync(1, dto))
                        .ThrowsAsync(new InvalidOperationException("No decisions"));

            var result = await _controller.PublishResults(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PublishResults_ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("Mode", "Required");

            var result = await _controller.PublishResults(1, new PublishResultDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region GetPublishStatus

        [Fact]
        public async Task GetPublishStatus_ReturnsOk_WhenFound()
        {
            var status = new PublishStatusResponseDto { CampaignId = 1 };

            _mockService.Setup(s => s.GetPublishStatusAsync(1))
                        .ReturnsAsync(status);

            var result = await _controller.GetPublishStatus(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetPublishStatus_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetPublishStatusAsync(99))
                        .ReturnsAsync((PublishStatusResponseDto?)null);

            var result = await _controller.GetPublishStatus(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetAiAnalysis

        [Fact]
        public async Task GetAiAnalysis_ReturnsOk_WhenSuccess()
        {
            var response = new AiCampaignAnalysisResponseDto { CampaignId = 1 };

            _mockAiService.Setup(s => s.AnalyzeCampaignCandidatesAsync(1))
                          .ReturnsAsync(response);

            var result = await _controller.GetAiAnalysis(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetAiAnalysis_ReturnsBadRequest_WhenServiceThrows()
        {
            _mockAiService.Setup(s => s.AnalyzeCampaignCandidatesAsync(1))
                          .ThrowsAsync(new Exception("AI Error"));

            var result = await _controller.GetAiAnalysis(1);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region AiSearchCandidates

        [Fact]
        public async Task AiSearchCandidates_ReturnsOk_WhenSuccess()
        {
            var dto = new AiSearchRequestDto { Query = "best candidate" };
            var response = new AiSearchResponseDto { TotalFound = 1 };

            _mockAiService.Setup(s => s.SearchCandidatesAsync(1, dto))
                          .ReturnsAsync(response);

            var result = await _controller.AiSearchCandidates(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AiSearchCandidates_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new AiSearchRequestDto { Query = "test" };

            _mockAiService.Setup(s => s.SearchCandidatesAsync(1, dto))
                          .ThrowsAsync(new Exception("AI Error"));

            var result = await _controller.AiSearchCandidates(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AiSearchCandidates_ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("Query", "Required");

            var result = await _controller.AiSearchCandidates(1, new AiSearchRequestDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region SubmitFeedback_BadRequest

        [Fact]
        public async Task SubmitFeedback_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new SubmitFeedbackDto { Result = "Invalid" };

            _mockService.Setup(s => s.SubmitFeedbackAsync(1, 2, dto))
                        .ThrowsAsync(new Exception("Invalid result"));

            var result = await _controller.SubmitFeedback(1, 2, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SubmitFeedback_ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("Result", "Required");

            var result = await _controller.SubmitFeedback(1, 2, new SubmitFeedbackDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region Create_BadRequest

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("Title", "Required");

            var result = await _controller.Create(new CreateInterviewScheduleDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region Update_BadRequest

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("Title", "Required");

            var result = await _controller.Update(1, new UpdateInterviewScheduleDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region UpdateStatus_BadRequest

        [Fact]
        public async Task UpdateStatus_ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("Status", "Required");

            var result = await _controller.UpdateStatus(1, new UpdateInterviewStatusDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region AssignInterviewers_BadRequest

        [Fact]
        public async Task AssignInterviewers_ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("Interviewers", "Required");

            var result = await _controller.AssignInterviewers(1, new AssignInterviewersDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AssignInterviewers_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new AssignInterviewersDto { Interviewers = new List<AssignInterviewerItemDto>() };

            _mockService.Setup(s => s.AssignInterviewersAsync(1, dto))
                        .ThrowsAsync(new Exception("General error"));

            var result = await _controller.AssignInterviewers(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion
    }
}
