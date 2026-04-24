using Microsoft.AspNetCore.Mvc;
using Moq;
using UNIC.BusinessLogic.DTOs;
using UNIC.BusinessLogic.Services.Interface;
using UNIC.Presentation.Controllers;

namespace UNIC.ControllerTest.Controllers
{
    public class ApplicationControllerTest
    {
        private readonly Mock<IApplicationService> _serviceMock = new();
        private readonly ApplicationController _sut;

        private static T? Prop<T>(object? obj, string name) =>
            (T?)obj?.GetType().GetProperty(name)?.GetValue(obj);

        public ApplicationControllerTest()
        {
            _sut = new ApplicationController(_serviceMock.Object);
        }

        private static ApplicationResponseDto BuildAppDto(int id = 1) => new()
        {
            ApplicationId = id,
            FormId = 10,
            UserId = Guid.NewGuid(),
            SubmissionDate = DateTime.UtcNow,
            Status = "Pending"
        };

        private static ApplicationFormResponseDto BuildFormDto(int id = 1) => new()
        {
            FormId = id,
            CampaignId = 1,
            FormName = "Form A",
            FormTitle = "Title A",
            CreatedAt = DateTime.UtcNow
        };

        private static ApplicationQuestionResponseDto BuildQuestionDto(int id = 1) => new()
        {
            QuestionId = id,
            FormId = 10,
            QuestionText = "Q?",
            QuestionType = "text",
            IsRequired = false,
            DisplayOrder = 1
        };

        private static ApplicationAnswerResponseDto BuildAnswerDto(int id = 1) => new()
        {
            AnswerId = id,
            ApplicationId = 1,
            QuestionId = 1,
            AnswerText = "Answer"
        };

        // ==================== APPLICATION ====================

        [Fact]
        public async Task GetAllApplications_WhenCalled_Returns200WithList()
        {
            _serviceMock.Setup(s => s.GetAllApplicationsAsync(5))
                        .ReturnsAsync(new List<ApplicationResponseDto> { BuildAppDto(), BuildAppDto(2) });

            var result = await _sut.GetAllApplications(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task GetApplicationById_WhenFound_Returns200WithDto()
        {
            _serviceMock.Setup(s => s.GetApplicationByIdAsync(1, 5)).ReturnsAsync(BuildAppDto(1));

            var result = await _sut.GetApplicationById(1, 5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
            Assert.Equal(1, Prop<ApplicationResponseDto>(ok.Value, "data")!.ApplicationId);
        }

        [Fact]
        public async Task GetApplicationById_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.GetApplicationByIdAsync(999, 5))
                        .ReturnsAsync((ApplicationResponseDto?)null);

            var result = await _sut.GetApplicationById(999, 5);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetApplicationsByUser_WhenCalled_Returns200WithList()
        {
            var userId = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetApplicationsByUserAsync(userId))
                        .ReturnsAsync(new List<ApplicationResponseDto> { BuildAppDto() });

            var result = await _sut.GetApplicationsByUser(userId);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task GetApplicationsByForm_WhenCalled_Returns200WithList()
        {
            _serviceMock.Setup(s => s.GetApplicationsByFormAsync(3, 5))
                        .ReturnsAsync(new List<ApplicationResponseDto> { BuildAppDto() });

            var result = await _sut.GetApplicationsByForm(3, 5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task GetApplicationsByStatus_WhenCalled_Returns200WithList()
        {
            _serviceMock.Setup(s => s.GetApplicationsByStatusAsync("Pending", 5))
                        .ReturnsAsync(new List<ApplicationResponseDto> { BuildAppDto() });

            var result = await _sut.GetApplicationsByStatus("Pending", 5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task GetApplicationByUserAndForm_WhenFound_Returns200WithDto()
        {
            var userId = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetApplicationByUserAndFormAsync(userId, 3))
                        .ReturnsAsync(BuildAppDto(1));

            var result = await _sut.GetApplicationByUserAndForm(userId, 3);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task GetApplicationByUserAndForm_WhenNotFound_Returns404()
        {
            var userId = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetApplicationByUserAndFormAsync(userId, 3))
                        .ReturnsAsync((ApplicationResponseDto?)null);

            var result = await _sut.GetApplicationByUserAndForm(userId, 3);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetApplicationsByCampaign_WhenCalled_Returns200WithList()
        {
            _serviceMock.Setup(s => s.GetApplicationsByCampaignAsync(2, 5, null))
                        .ReturnsAsync(new List<ApplicationResponseDto> { BuildAppDto() });

            var result = await _sut.GetApplicationsByCampaign(2, 5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task GetApplicationsByClub_WhenCalled_Returns200WithList()
        {
            _serviceMock.Setup(s => s.GetApplicationsByClubAsync(5, null))
                        .ReturnsAsync(new List<ApplicationResponseDto> { BuildAppDto() });

            var result = await _sut.GetApplicationsByClub(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task CreateApplication_WhenCalled_Returns200WithCreatedDto()
        {
            _serviceMock.Setup(s => s.CreateApplicationAsync(It.IsAny<CreateApplicationDto>()))
                        .ReturnsAsync(BuildAppDto(1));

            var result = await _sut.CreateApplication(
                new CreateApplicationDto { FormId = 1, UserId = Guid.NewGuid() });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, Prop<ApplicationResponseDto>(ok.Value, "data")!.ApplicationId);
        }

        [Fact]
        public async Task UpdateApplication_WhenFound_Returns200()
        {
            _serviceMock.Setup(s => s.UpdateApplicationAsync(1, 5, It.IsAny<ApplicationResponseDto>()))
                        .ReturnsAsync(true);

            var result = await _sut.UpdateApplication(1, 5, BuildAppDto(1));

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task UpdateApplication_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.UpdateApplicationAsync(999, 5, It.IsAny<ApplicationResponseDto>()))
                        .ReturnsAsync(false);

            var result = await _sut.UpdateApplication(999, 5, BuildAppDto(999));

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateApplicationStatus_WhenValid_Returns200()
        {
            var updated = BuildAppDto(1);
            updated.Status = "Approved";
            _serviceMock.Setup(s => s.UpdateApplicationStatusAsync(1, 5, "Approved"))
                        .ReturnsAsync(updated);

            var result = await _sut.UpdateApplicationStatus(1, 5,
                new UpdateApplicationStatusDto { Status = "Approved" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task UpdateApplicationStatus_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.UpdateApplicationStatusAsync(999, 5, It.IsAny<string>()))
                        .ReturnsAsync((ApplicationResponseDto?)null);

            var result = await _sut.UpdateApplicationStatus(999, 5,
                new UpdateApplicationStatusDto { Status = "Approved" });

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateApplicationStatus_WhenInvalidStatus_Returns400WithMessage()
        {
            _serviceMock.Setup(s => s.UpdateApplicationStatusAsync(1, 5, "WRONG"))
                        .ThrowsAsync(new ArgumentException("Invalid status."));

            var result = await _sut.UpdateApplicationStatus(1, 5,
                new UpdateApplicationStatusDto { Status = "WRONG" });

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.False(Prop<bool>(bad.Value, "success"));
            Assert.Contains("Invalid status", Prop<string>(bad.Value, "message"));
        }

        [Fact]
        public async Task DeleteApplication_WhenFound_Returns200()
        {
            _serviceMock.Setup(s => s.DeleteApplicationAsync(1, 5)).ReturnsAsync(true);

            var result = await _sut.DeleteApplication(1, 5);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteApplication_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.DeleteApplicationAsync(999, 5)).ReturnsAsync(false);

            var result = await _sut.DeleteApplication(999, 5);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ==================== FORM ====================

        [Fact]
        public async Task GetAllForms_WhenCalled_Returns200WithList()
        {
            _serviceMock.Setup(s => s.GetAllFormsAsync(5))
                        .ReturnsAsync(new List<ApplicationFormResponseDto> { BuildFormDto(), BuildFormDto(2) });

            var result = await _sut.GetAllForms(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task GetFormsByCampaign_WhenCalled_Returns200WithList()
        {
            _serviceMock.Setup(s => s.GetFormsByCampaignAsync(2, 5))
                        .ReturnsAsync(new List<ApplicationFormResponseDto> { BuildFormDto() });

            var result = await _sut.GetFormsByCampaign(2, 5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task GetFormById_WhenFound_Returns200WithDto()
        {
            _serviceMock.Setup(s => s.GetFormByIdAsync(1)).ReturnsAsync(BuildFormDto(1));

            var result = await _sut.GetFormById(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
            Assert.Equal(1, Prop<ApplicationFormResponseDto>(ok.Value, "data")!.FormId);
        }

        [Fact]
        public async Task GetFormById_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.GetFormByIdAsync(999))
                        .ReturnsAsync((ApplicationFormResponseDto?)null);

            var result = await _sut.GetFormById(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateForm_WhenCalled_Returns200WithCreatedDto()
        {
            _serviceMock.Setup(s => s.CreateFormAsync(It.IsAny<CreateApplicationFormDto>()))
                        .ReturnsAsync(BuildFormDto(1));

            var result = await _sut.CreateForm(
                new CreateApplicationFormDto { CampaignId = 1, FormName = "Form A" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, Prop<ApplicationFormResponseDto>(ok.Value, "data")!.FormId);
        }

        [Fact]
        public async Task UpdateForm_WhenFound_Returns200()
        {
            _serviceMock.Setup(s => s.UpdateFormAsync(1, 5, It.IsAny<ApplicationFormResponseDto>()))
                        .ReturnsAsync(true);

            var result = await _sut.UpdateForm(1, 5, BuildFormDto(1));

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task UpdateForm_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.UpdateFormAsync(999, 5, It.IsAny<ApplicationFormResponseDto>()))
                        .ReturnsAsync(false);

            var result = await _sut.UpdateForm(999, 5, BuildFormDto(999));

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteForm_WhenFound_Returns200()
        {
            _serviceMock.Setup(s => s.DeleteFormAsync(1, 5)).ReturnsAsync(true);

            var result = await _sut.DeleteForm(1, 5);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteForm_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.DeleteFormAsync(999, 5)).ReturnsAsync(false);

            var result = await _sut.DeleteForm(999, 5);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ==================== QUESTION ====================

        [Fact]
        public async Task GetQuestionsByForm_WhenCalled_Returns200WithList()
        {
            _serviceMock.Setup(s => s.GetQuestionsByFormAsync(10))
                        .ReturnsAsync(new List<ApplicationQuestionResponseDto> { BuildQuestionDto(), BuildQuestionDto(2) });

            var result = await _sut.GetQuestionsByForm(10);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task GetQuestionById_WhenFound_Returns200WithDto()
        {
            _serviceMock.Setup(s => s.GetQuestionByIdAsync(1, 5)).ReturnsAsync(BuildQuestionDto(1));

            var result = await _sut.GetQuestionById(1, 5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
            Assert.Equal(1, Prop<ApplicationQuestionResponseDto>(ok.Value, "data")!.QuestionId);
        }

        [Fact]
        public async Task GetQuestionById_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.GetQuestionByIdAsync(999, 5))
                        .ReturnsAsync((ApplicationQuestionResponseDto?)null);

            var result = await _sut.GetQuestionById(999, 5);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateQuestion_WhenCalled_Returns200WithCreatedDto()
        {
            _serviceMock.Setup(s => s.CreateQuestionAsync(It.IsAny<CreateApplicationQuestionDto>()))
                        .ReturnsAsync(BuildQuestionDto(1));

            var result = await _sut.CreateQuestion(
                new CreateApplicationQuestionDto { FormId = 10, QuestionText = "Q?" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, Prop<ApplicationQuestionResponseDto>(ok.Value, "data")!.QuestionId);
        }

        [Fact]
        public async Task UpdateQuestion_WhenFound_Returns200()
        {
            _serviceMock.Setup(s => s.UpdateQuestionAsync(1, 5, It.IsAny<ApplicationQuestionResponseDto>()))
                        .ReturnsAsync(true);

            var result = await _sut.UpdateQuestion(1, 5, BuildQuestionDto(1));

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task UpdateQuestion_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.UpdateQuestionAsync(999, 5, It.IsAny<ApplicationQuestionResponseDto>()))
                        .ReturnsAsync(false);

            var result = await _sut.UpdateQuestion(999, 5, BuildQuestionDto(999));

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteQuestion_WhenFound_Returns200()
        {
            _serviceMock.Setup(s => s.DeleteQuestionAsync(1, 5)).ReturnsAsync(true);

            var result = await _sut.DeleteQuestion(1, 5);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteQuestion_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.DeleteQuestionAsync(999, 5)).ReturnsAsync(false);

            var result = await _sut.DeleteQuestion(999, 5);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ==================== ANSWER ====================

        [Fact]
        public async Task GetAnswersByApplication_WhenCalled_Returns200WithList()
        {
            _serviceMock.Setup(s => s.GetAnswersByApplicationAsync(1, 5))
                        .ReturnsAsync(new List<ApplicationAnswerResponseDto> { BuildAnswerDto(), BuildAnswerDto(2) });

            var result = await _sut.GetAnswersByApplication(1, 5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task GetAnswerById_WhenFound_Returns200WithDto()
        {
            _serviceMock.Setup(s => s.GetAnswerByIdAsync(1, 5)).ReturnsAsync(BuildAnswerDto(1));

            var result = await _sut.GetAnswerById(1, 5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
            Assert.Equal(1, Prop<ApplicationAnswerResponseDto>(ok.Value, "data")!.AnswerId);
        }

        [Fact]
        public async Task GetAnswerById_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.GetAnswerByIdAsync(999, 5))
                        .ReturnsAsync((ApplicationAnswerResponseDto?)null);

            var result = await _sut.GetAnswerById(999, 5);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateAnswer_WhenValid_Returns200WithCreatedDto()
        {
            _serviceMock.Setup(s => s.CreateAnswerAsync(It.IsAny<CreateApplicationAnswerDto>(), 5))
                        .ReturnsAsync(BuildAnswerDto(1));

            var result = await _sut.CreateAnswer(1,
                new CreateApplicationAnswerDto { QuestionId = 1, AnswerText = "A" }, 5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
            Assert.Equal(1, Prop<ApplicationAnswerResponseDto>(ok.Value, "data")!.AnswerId);
        }

        [Fact]
        public async Task CreateAnswer_WhenArgumentException_Returns400WithMessage()
        {
            _serviceMock.Setup(s => s.CreateAnswerAsync(It.IsAny<CreateApplicationAnswerDto>(), 5))
                        .ThrowsAsync(new ArgumentException("Application không tồn tại."));

            var result = await _sut.CreateAnswer(1,
                new CreateApplicationAnswerDto { QuestionId = 1, AnswerText = "A" }, 5);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.False(Prop<bool>(bad.Value, "success"));
            Assert.Contains("Application không tồn tại", Prop<string>(bad.Value, "message"));
        }

        [Fact]
        public async Task UpdateAnswer_WhenFound_Returns200()
        {
            _serviceMock.Setup(s => s.UpdateAnswerAsync(1, 5, It.IsAny<ApplicationAnswerResponseDto>()))
                        .ReturnsAsync(true);

            var result = await _sut.UpdateAnswer(1, 5, BuildAnswerDto(1));

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task UpdateAnswer_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.UpdateAnswerAsync(999, 5, It.IsAny<ApplicationAnswerResponseDto>()))
                        .ReturnsAsync(false);

            var result = await _sut.UpdateAnswer(999, 5, BuildAnswerDto(999));

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteAnswer_WhenFound_Returns200()
        {
            _serviceMock.Setup(s => s.DeleteAnswerAsync(1, 5)).ReturnsAsync(true);

            var result = await _sut.DeleteAnswer(1, 5);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteAnswer_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.DeleteAnswerAsync(999, 5)).ReturnsAsync(false);

            var result = await _sut.DeleteAnswer(999, 5);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ==================== SUBMIT ====================

        [Fact]
        public async Task SubmitApplication_WhenValid_Returns200()
        {
            _serviceMock.Setup(s => s.SubmitApplicationWithAnswersAsync(It.IsAny<SubmitApplicationWithAnswersDto>()))
                        .ReturnsAsync(BuildAppDto(1));

            var result = await _sut.SubmitApplication(
                new SubmitApplicationWithAnswersDto { UserId = Guid.NewGuid(), FormId = 1 });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task SubmitApplication_WhenInvalidOperation_Returns409WithMessage()
        {
            _serviceMock.Setup(s => s.SubmitApplicationWithAnswersAsync(It.IsAny<SubmitApplicationWithAnswersDto>()))
                        .ThrowsAsync(new InvalidOperationException("Đã nộp đơn rồi."));

            var result = await _sut.SubmitApplication(
                new SubmitApplicationWithAnswersDto { UserId = Guid.NewGuid(), FormId = 1 });

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.False(Prop<bool>(conflict.Value, "success"));
            Assert.Contains("Đã nộp đơn rồi", Prop<string>(conflict.Value, "message"));
        }

        [Fact]
        public async Task SubmitApplication_WhenExceptionThrown_Returns400WithMessage()
        {
            _serviceMock.Setup(s => s.SubmitApplicationWithAnswersAsync(It.IsAny<SubmitApplicationWithAnswersDto>()))
                        .ThrowsAsync(new ArgumentException("Tài khoản không tồn tại."));

            var result = await _sut.SubmitApplication(
                new SubmitApplicationWithAnswersDto { UserId = Guid.NewGuid(), FormId = 1 });

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Tài khoản không tồn tại", Prop<string>(bad.Value, "message"));
        }
    }
}
