using BusinessLogic.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.BusinessLogic.DTOs;
using UNIC.BusinessLogic.Services.Interface;
using UNIC.Presentation.Controllers;
using Xunit;

namespace UNIC.Presentation.Test.Controllers
{
    public class ApplicationControllerTest
    {
        private readonly Mock<IApplicationService> _mockService;
        private readonly ApplicationController _controller;

        public ApplicationControllerTest()
        {
            _mockService = new Mock<IApplicationService>();
            _controller = new ApplicationController(_mockService.Object);
        }

        #region GetTestUserId

        [Fact]
        public void GetTestUserId_ReturnsOk()
        {
            var result = _controller.GetTestUserId();
            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region GetAllApplications

        [Fact]
        public async Task GetAllApplications_ReturnsOk_WhenApplicationsExist()
        {
            _mockService.Setup(s => s.GetAllApplicationsAsync())
                .ReturnsAsync(new List<ApplicationResponseDto> { new ApplicationResponseDto() });

            var result = await _controller.GetAllApplications();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetAllApplications_ReturnsNotFound_WhenEmpty()
        {
            _mockService.Setup(s => s.GetAllApplicationsAsync())
                .ReturnsAsync(new List<ApplicationResponseDto>());

            var result = await _controller.GetAllApplications();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetApplicationById

        [Fact]
        public async Task GetApplicationById_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetApplicationByIdAsync(1))
                .ReturnsAsync(new ApplicationResponseDto { ApplicationId = 1 });

            var result = await _controller.GetApplicationById(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetApplicationById_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetApplicationByIdAsync(99))
                .ReturnsAsync((ApplicationResponseDto?)null);

            var result = await _controller.GetApplicationById(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetApplicationsByUser

        [Fact]
        public async Task GetApplicationsByUser_ReturnsOk_WhenFound()
        {
            var userId = Guid.NewGuid();
            _mockService.Setup(s => s.GetApplicationsByUserAsync(userId))
                .ReturnsAsync(new List<ApplicationResponseDto> { new ApplicationResponseDto() });

            var result = await _controller.GetApplicationsByUser(userId);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetApplicationsByUser_ReturnsNotFound_WhenEmpty()
        {
            var userId = Guid.NewGuid();
            _mockService.Setup(s => s.GetApplicationsByUserAsync(userId))
                .ReturnsAsync(new List<ApplicationResponseDto>());

            var result = await _controller.GetApplicationsByUser(userId);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetApplicationsByForm

        [Fact]
        public async Task GetApplicationsByForm_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetApplicationsByFormAsync(1))
                .ReturnsAsync(new List<ApplicationResponseDto> { new ApplicationResponseDto() });

            var result = await _controller.GetApplicationsByForm(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetApplicationsByForm_ReturnsNotFound_WhenEmpty()
        {
            _mockService.Setup(s => s.GetApplicationsByFormAsync(1))
                .ReturnsAsync(new List<ApplicationResponseDto>());

            var result = await _controller.GetApplicationsByForm(1);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetApplicationsByStatus

        [Fact]
        public async Task GetApplicationsByStatus_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetApplicationsByStatusAsync("Pending"))
                .ReturnsAsync(new List<ApplicationResponseDto> { new ApplicationResponseDto() });

            var result = await _controller.GetApplicationsByStatus("Pending");

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetApplicationsByStatus_ReturnsNotFound_WhenEmpty()
        {
            _mockService.Setup(s => s.GetApplicationsByStatusAsync("Unknown"))
                .ReturnsAsync(new List<ApplicationResponseDto>());

            var result = await _controller.GetApplicationsByStatus("Unknown");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetApplicationByUserAndForm

        [Fact]
        public async Task GetApplicationByUserAndForm_ReturnsOk_WhenFound()
        {
            var userId = Guid.NewGuid();
            _mockService.Setup(s => s.GetApplicationByUserAndFormAsync(userId, 1))
                .ReturnsAsync(new ApplicationResponseDto());

            var result = await _controller.GetApplicationByUserAndForm(userId, 1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetApplicationByUserAndForm_ReturnsNotFound_WhenNull()
        {
            var userId = Guid.NewGuid();
            _mockService.Setup(s => s.GetApplicationByUserAndFormAsync(userId, 1))
                .ReturnsAsync((ApplicationResponseDto?)null);

            var result = await _controller.GetApplicationByUserAndForm(userId, 1);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetApplicationsByCampaign / GetApplicationsByClub

        [Fact]
        public async Task GetApplicationsByCampaign_ReturnsOk()
        {
            _mockService.Setup(s => s.GetApplicationsByCampaignAsync(1, null))
                .ReturnsAsync(new List<ApplicationResponseDto>());

            var result = await _controller.GetApplicationsByCampaign(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetApplicationsByClub_ReturnsOk()
        {
            _mockService.Setup(s => s.GetApplicationsByClubAsync(1, null))
                .ReturnsAsync(new List<ApplicationResponseDto>());

            var result = await _controller.GetApplicationsByClub(1);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region CreateApplication

        [Fact]
        public async Task CreateApplication_ReturnsCreated()
        {
            var request = new CreateApplicationDto { FormId = 1, UserId = Guid.NewGuid() };
            var created = new ApplicationResponseDto { ApplicationId = 5 };
            _mockService.Setup(s => s.CreateApplicationAsync(request)).ReturnsAsync(created);

            var result = await _controller.CreateApplication(request);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        #endregion

        #region UpdateApplication

        [Fact]
        public async Task UpdateApplication_ReturnsOk_WhenFound()
        {
            var dto = new ApplicationResponseDto { ApplicationId = 1 };
            _mockService.Setup(s => s.UpdateApplicationAsync(1, dto)).ReturnsAsync(true);

            var result = await _controller.UpdateApplication(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateApplication_ReturnsNotFound_WhenMissing()
        {
            var dto = new ApplicationResponseDto();
            _mockService.Setup(s => s.UpdateApplicationAsync(1, dto)).ReturnsAsync(false);

            var result = await _controller.UpdateApplication(1, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region UpdateApplicationStatus

        [Fact]
        public async Task UpdateApplicationStatus_ReturnsOk_WhenValid()
        {
            var dto = new UpdateApplicationStatusDto { Status = "Approved" };
            _mockService.Setup(s => s.UpdateApplicationStatusAsync(1, "Approved"))
                .ReturnsAsync(new ApplicationResponseDto { ApplicationId = 1 });

            var result = await _controller.UpdateApplicationStatus(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateApplicationStatus_ReturnsNotFound_WhenNull()
        {
            var dto = new UpdateApplicationStatusDto { Status = "Approved" };
            _mockService.Setup(s => s.UpdateApplicationStatusAsync(1, "Approved"))
                .ReturnsAsync((ApplicationResponseDto?)null);

            var result = await _controller.UpdateApplicationStatus(1, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateApplicationStatus_ReturnsBadRequest_WhenInvalidStatus()
        {
            var dto = new UpdateApplicationStatusDto { Status = "BadStatus" };
            _mockService.Setup(s => s.UpdateApplicationStatusAsync(1, "BadStatus"))
                .ThrowsAsync(new ArgumentException("Invalid status"));

            var result = await _controller.UpdateApplicationStatus(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region DeleteApplication

        [Fact]
        public async Task DeleteApplication_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.DeleteApplicationAsync(1)).ReturnsAsync(true);

            var result = await _controller.DeleteApplication(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteApplication_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.DeleteApplicationAsync(99)).ReturnsAsync(false);

            var result = await _controller.DeleteApplication(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region Forms

        [Fact]
        public async Task GetAllForms_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetAllFormsAsync())
                .ReturnsAsync(new List<ApplicationFormResponseDto> { new() });

            var result = await _controller.GetAllForms();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetAllForms_ReturnsNotFound_WhenEmpty()
        {
            _mockService.Setup(s => s.GetAllFormsAsync())
                .ReturnsAsync(new List<ApplicationFormResponseDto>());

            var result = await _controller.GetAllForms();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetFormsByCampaign_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetFormsByCampaignAsync(1))
                .ReturnsAsync(new List<ApplicationFormResponseDto> { new() });

            var result = await _controller.GetFormsByCampaign(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetFormsByCampaign_ReturnsNotFound_WhenEmpty()
        {
            _mockService.Setup(s => s.GetFormsByCampaignAsync(1))
                .ReturnsAsync(new List<ApplicationFormResponseDto>());

            var result = await _controller.GetFormsByCampaign(1);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetFormById_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetFormByIdAsync(1))
                .ReturnsAsync(new ApplicationFormResponseDto { FormId = 1 });

            var result = await _controller.GetFormById(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetFormById_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetFormByIdAsync(99))
                .ReturnsAsync((ApplicationFormResponseDto?)null);

            var result = await _controller.GetFormById(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateForm_ReturnsCreated()
        {
            var request = new CreateApplicationFormDto { CampaignId = 1, FormName = "Test" };
            _mockService.Setup(s => s.CreateFormAsync(request))
                .ReturnsAsync(new ApplicationFormResponseDto { FormId = 1 });

            var result = await _controller.CreateForm(request);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task UpdateForm_ReturnsOk_WhenFound()
        {
            var dto = new ApplicationFormResponseDto();
            _mockService.Setup(s => s.UpdateFormAsync(1, dto)).ReturnsAsync(true);

            var result = await _controller.UpdateForm(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateForm_ReturnsNotFound_WhenMissing()
        {
            var dto = new ApplicationFormResponseDto();
            _mockService.Setup(s => s.UpdateFormAsync(99, dto)).ReturnsAsync(false);

            var result = await _controller.UpdateForm(99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteForm_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.DeleteFormAsync(1)).ReturnsAsync(true);

            var result = await _controller.DeleteForm(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteForm_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.DeleteFormAsync(99)).ReturnsAsync(false);

            var result = await _controller.DeleteForm(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region Questions

        [Fact]
        public async Task GetQuestionsByForm_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetQuestionsByFormAsync(1))
                .ReturnsAsync(new List<ApplicationQuestionResponseDto> { new() });

            var result = await _controller.GetQuestionsByForm(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetQuestionsByForm_ReturnsNotFound_WhenEmpty()
        {
            _mockService.Setup(s => s.GetQuestionsByFormAsync(1))
                .ReturnsAsync(new List<ApplicationQuestionResponseDto>());

            var result = await _controller.GetQuestionsByForm(1);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetQuestionById_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetQuestionByIdAsync(1))
                .ReturnsAsync(new ApplicationQuestionResponseDto { QuestionId = 1 });

            var result = await _controller.GetQuestionById(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetQuestionById_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetQuestionByIdAsync(99))
                .ReturnsAsync((ApplicationQuestionResponseDto?)null);

            var result = await _controller.GetQuestionById(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateQuestion_ReturnsCreated()
        {
            var request = new CreateApplicationQuestionDto { FormId = 1, QuestionText = "Q?" };
            _mockService.Setup(s => s.CreateQuestionAsync(request))
                .ReturnsAsync(new ApplicationQuestionResponseDto { QuestionId = 1 });

            var result = await _controller.CreateQuestion(request);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task UpdateQuestion_ReturnsOk_WhenFound()
        {
            var dto = new ApplicationQuestionResponseDto();
            _mockService.Setup(s => s.UpdateQuestionAsync(1, dto)).ReturnsAsync(true);

            var result = await _controller.UpdateQuestion(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateQuestion_ReturnsNotFound_WhenMissing()
        {
            var dto = new ApplicationQuestionResponseDto();
            _mockService.Setup(s => s.UpdateQuestionAsync(99, dto)).ReturnsAsync(false);

            var result = await _controller.UpdateQuestion(99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteQuestion_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.DeleteQuestionAsync(1)).ReturnsAsync(true);

            var result = await _controller.DeleteQuestion(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteQuestion_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.DeleteQuestionAsync(99)).ReturnsAsync(false);

            var result = await _controller.DeleteQuestion(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region Answers

        [Fact]
        public async Task GetAnswersByApplication_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetAnswersByApplicationAsync(1))
                .ReturnsAsync(new List<ApplicationAnswerResponseDto> { new() });

            var result = await _controller.GetAnswersByApplication(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetAnswersByApplication_ReturnsNotFound_WhenEmpty()
        {
            _mockService.Setup(s => s.GetAnswersByApplicationAsync(1))
                .ReturnsAsync(new List<ApplicationAnswerResponseDto>());

            var result = await _controller.GetAnswersByApplication(1);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetAnswerById_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetAnswerByIdAsync(1))
                .ReturnsAsync(new ApplicationAnswerResponseDto { AnswerId = 1 });

            var result = await _controller.GetAnswerById(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetAnswerById_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetAnswerByIdAsync(99))
                .ReturnsAsync((ApplicationAnswerResponseDto?)null);

            var result = await _controller.GetAnswerById(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateAnswer_ReturnsCreated_WhenValid()
        {
            var request = new CreateApplicationAnswerDto { ApplicationId = 1, QuestionId = 1, AnswerText = "Yes" };
            _mockService.Setup(s => s.CreateAnswerAsync(request))
                .ReturnsAsync(new ApplicationAnswerResponseDto { AnswerId = 1 });

            var result = await _controller.CreateAnswer(1, request);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task CreateAnswer_ReturnsBadRequest_WhenServiceThrows()
        {
            var request = new CreateApplicationAnswerDto { ApplicationId = 1, QuestionId = 99 };
            _mockService.Setup(s => s.CreateAnswerAsync(request))
                .ThrowsAsync(new ArgumentException("Invalid question"));

            var result = await _controller.CreateAnswer(1, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAnswer_ReturnsOk_WhenFound()
        {
            var dto = new ApplicationAnswerResponseDto();
            _mockService.Setup(s => s.UpdateAnswerAsync(1, dto)).ReturnsAsync(true);

            var result = await _controller.UpdateAnswer(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAnswer_ReturnsNotFound_WhenMissing()
        {
            var dto = new ApplicationAnswerResponseDto();
            _mockService.Setup(s => s.UpdateAnswerAsync(99, dto)).ReturnsAsync(false);

            var result = await _controller.UpdateAnswer(99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteAnswer_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.DeleteAnswerAsync(1)).ReturnsAsync(true);

            var result = await _controller.DeleteAnswer(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteAnswer_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.DeleteAnswerAsync(99)).ReturnsAsync(false);

            var result = await _controller.DeleteAnswer(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region SubmitApplicationWithAnswers

        [Fact]
        public async Task SubmitApplicationWithAnswers_ReturnsCreated_WhenValid()
        {
            var request = new SubmitApplicationWithAnswersDto { UserId = Guid.NewGuid(), FormId = 1 };
            _mockService.Setup(s => s.SubmitApplicationWithAnswersAsync(request))
                .ReturnsAsync(new ApplicationResponseDto { ApplicationId = 10 });

            var result = await _controller.SubmitApplicationWithAnswers(request);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswers_ReturnsBadRequest_WhenInvalidOperation()
        {
            var request = new SubmitApplicationWithAnswersDto { UserId = Guid.NewGuid(), FormId = 1 };
            _mockService.Setup(s => s.SubmitApplicationWithAnswersAsync(request))
                .ThrowsAsync(new InvalidOperationException("Already submitted"));

            var result = await _controller.SubmitApplicationWithAnswers(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswers_ReturnsBadRequest_WhenArgumentException()
        {
            var request = new SubmitApplicationWithAnswersDto { UserId = Guid.NewGuid(), FormId = 1 };
            _mockService.Setup(s => s.SubmitApplicationWithAnswersAsync(request))
                .ThrowsAsync(new ArgumentException("Bad arg"));

            var result = await _controller.SubmitApplicationWithAnswers(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswers_Returns500_WhenUnexpectedException()
        {
            var request = new SubmitApplicationWithAnswersDto { UserId = Guid.NewGuid(), FormId = 1 };
            _mockService.Setup(s => s.SubmitApplicationWithAnswersAsync(request))
                .ThrowsAsync(new Exception("Unexpected"));

            var result = await _controller.SubmitApplicationWithAnswers(request);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        #endregion
    }
}
