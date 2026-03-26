using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Moq;
using UNIC.BusinessLogic.DTOs;
using UNIC.BusinessLogic.Services.Implementation;
using UNIC.DataAccess.Repositories.Interface;

namespace UNIC.ServiceTest.Services
{

    public class ApplicationServiceTest
    {
        private readonly Mock<IApplicationRepository> _appRepoMock = new();
        private readonly Mock<IUserRepository> _userRepoMock = new();
        private readonly ApplicationService _sut;

        public ApplicationServiceTest()
        {
            _sut = new ApplicationService(_appRepoMock.Object, _userRepoMock.Object);
        }

        private static Application BuildApp(int id = 1, Guid? userId = null) => new()
        {
            ApplicationId = id,
            FormId = 10,
            UserId = userId ?? Guid.NewGuid(),
            SubmissionDate = DateTime.UtcNow,
            Status = "Pending"
        };

        private static ApplicationQuestion BuildQuestion(int id, int formId, bool required = false) => new()
        {
            QuestionId = id,
            FormId = formId,
            QuestionText = "Q?",
            QuestionType = "text",
            IsRequired = required,
            DisplayOrder = 1
        };

        [Fact]
        public async Task GetApplicationByIdAsync_WhenFound_ReturnsMappedDto()
        {
            _appRepoMock.Setup(r => r.GetByIdAsync(1, 5)).ReturnsAsync(BuildApp(1));

            var result = await _sut.GetApplicationByIdAsync(1, 5);

            Assert.NotNull(result);
            Assert.Equal(1, result!.ApplicationId);
            Assert.Equal("Pending", result.Status);
        }

        [Fact]
        public async Task GetApplicationByIdAsync_WhenNotFound_ReturnsNull()
        {
            _appRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync((Application?)null);

            var result = await _sut.GetApplicationByIdAsync(999, 5);

            Assert.Null(result);
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Approved")]
        [InlineData("Rejected")]
        public async Task UpdateApplicationStatusAsync_WithValidStatus_ReturnsUpdatedDto(string status)
        {
            var app = BuildApp(1);
            _appRepoMock.Setup(r => r.GetByIdAsync(1, 5)).ReturnsAsync(app);
            _appRepoMock.Setup(r => r.UpdateAsync(app)).ReturnsAsync(true);

            var result = await _sut.UpdateApplicationStatusAsync(1, 5, status);

            Assert.NotNull(result);
            Assert.Equal(status, result!.Status);
        }

        [Fact]
        public async Task UpdateApplicationStatusAsync_WithInvalidStatus_ThrowsArgumentException()
        {
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.UpdateApplicationStatusAsync(1, 5, "InvalidStatus"));

            Assert.Contains("Invalid status", ex.Message);
        }

        [Fact]
        public async Task UpdateApplicationStatusAsync_WhenNotFound_ReturnsNull()
        {
            _appRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync((Application?)null);

            var result = await _sut.UpdateApplicationStatusAsync(999, 5, "Approved");

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAnswerAsync_WhenApplicationNotFound_ThrowsArgumentException()
        {
            _appRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync((Application?)null);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CreateAnswerAsync(
                    new CreateApplicationAnswerDto { ApplicationId = 1, QuestionId = 1, AnswerText = "A" }, 5));

            Assert.Contains("Application không tồn tại", ex.Message);
        }

        [Fact]
        public async Task CreateAnswerAsync_WhenRequiredQuestionHasNoAnswer_ThrowsArgumentException()
        {
            var app = BuildApp(1);
            var question = BuildQuestion(1, formId: 10, required: true);
            _appRepoMock.Setup(r => r.GetByIdAsync(1, 5)).ReturnsAsync(app);
            _appRepoMock.Setup(r => r.GetQuestionByIdAsync(1, 5)).ReturnsAsync(question);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CreateAnswerAsync(
                    new CreateApplicationAnswerDto { ApplicationId = 1, QuestionId = 1, AnswerText = " " }, 5));

            Assert.Contains("bắt buộc", ex.Message);
        }

        [Fact]
        public async Task CreateAnswerAsync_WhenValid_ReturnsCreatedDto()
        {
            var app = BuildApp(1);
            var question = BuildQuestion(1, formId: 10, required: false);
            var answer = new ApplicationAnswer { AnswerId = 1, ApplicationId = 1, QuestionId = 1, AnswerText = "My answer" };
            _appRepoMock.Setup(r => r.GetByIdAsync(1, 5)).ReturnsAsync(app);
            _appRepoMock.Setup(r => r.GetQuestionByIdAsync(1, 5)).ReturnsAsync(question);
            _appRepoMock.Setup(r => r.CreateAnswerAsync(It.IsAny<ApplicationAnswer>())).ReturnsAsync(answer);

            var result = await _sut.CreateAnswerAsync(
                new CreateApplicationAnswerDto { ApplicationId = 1, QuestionId = 1, AnswerText = "My answer" }, 5);

            Assert.Equal(1, result.AnswerId);
            Assert.Equal("My answer", result.AnswerText);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_WhenUserNotFound_ThrowsArgumentException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.SubmitApplicationWithAnswersAsync(5,
                    new SubmitApplicationWithAnswersDto { UserId = Guid.NewGuid(), FormId = 1 }));

            Assert.Contains("Tài khoản không tồn tại", ex.Message);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_WhenFormNotFound_ThrowsArgumentException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new User());
            _appRepoMock.Setup(r => r.GetFormByIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync((ApplicationForm?)null);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.SubmitApplicationWithAnswersAsync(5,
                    new SubmitApplicationWithAnswersDto { UserId = Guid.NewGuid(), FormId = 99 }));

            Assert.Contains("Form không tồn tại", ex.Message);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_WhenMissingRequiredAnswers_ThrowsArgumentException()
        {
            var userId = Guid.NewGuid();
            var form = new ApplicationForm { FormId = 1, CampaignId = 1, FormName = "F", FormTitle = "T", CreatedAt = DateTime.UtcNow };
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User());
            _appRepoMock.Setup(r => r.GetFormByIdAsync(1, 5)).ReturnsAsync(form);
            _appRepoMock.Setup(r => r.GetQuestionsByFormIdAsync(1, 5))
                        .ReturnsAsync(new List<ApplicationQuestion> { BuildQuestion(1, 1, required: true) });

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.SubmitApplicationWithAnswersAsync(5, new SubmitApplicationWithAnswersDto
                {
                    UserId = userId,
                    FormId = 1,
                    Answers = new List<ApplicationAnswerItemDto>()
                }));

            Assert.Contains("bắt buộc", ex.Message);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_WhenValid_ReturnsCreatedApplication()
        {
            var userId = Guid.NewGuid();
            var form = new ApplicationForm { FormId = 1, CampaignId = 1, FormName = "F", FormTitle = "T", CreatedAt = DateTime.UtcNow };
            var createdApp = BuildApp(99, userId);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User());
            _appRepoMock.Setup(r => r.GetFormByIdAsync(1, 5)).ReturnsAsync(form);
            _appRepoMock.Setup(r => r.GetQuestionsByFormIdAsync(1, 5)).ReturnsAsync(new List<ApplicationQuestion>());
            _appRepoMock.Setup(r => r.CreateAsync(It.IsAny<Application>())).ReturnsAsync(createdApp);

            var result = await _sut.SubmitApplicationWithAnswersAsync(5,
                new SubmitApplicationWithAnswersDto { UserId = userId, FormId = 1, Answers = null });

            Assert.Equal(99, result.ApplicationId);
            Assert.Equal("Pending", result.Status);
        }
    }
}