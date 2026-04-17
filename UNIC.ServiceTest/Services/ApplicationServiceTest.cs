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

        // ==================== BUILDERS ====================

        private static Application BuildApp(int id = 1, Guid? userId = null, int formId = 10) => new()
        {
            ApplicationId = id,
            FormId = formId,
            UserId = userId ?? Guid.NewGuid(),
            SubmissionDate = DateTime.UtcNow,
            Status = "Pending"
        };

        private static ApplicationForm BuildForm(int id = 1, int campaignId = 1, int clubId = 5) => new()
        {
            FormId = id,
            CampaignId = campaignId,
            FormName = "Form A",
            FormTitle = "Title A",
            Description = "Desc",
            CreatedAt = DateTime.UtcNow,
            RecruitmentCampaign = new RecruitmentCampaign { CampaignId = campaignId, ClubId = clubId }
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

        private static ApplicationAnswer BuildAnswer(int id = 1, int appId = 1, int questionId = 1) => new()
        {
            AnswerId = id,
            ApplicationId = appId,
            QuestionId = questionId,
            AnswerText = "My answer"
        };

        // ==================== APPLICATION ====================

        [Fact]
        public async Task GetAllApplicationsAsync_WhenCalled_ReturnsMappedList()
        {
            _appRepoMock.Setup(r => r.GetAllAsync(5))
                        .ReturnsAsync(new List<Application> { BuildApp(1), BuildApp(2) });

            var result = (await _sut.GetAllApplicationsAsync(5)).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].ApplicationId);
        }

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

        [Fact]
        public async Task CreateApplicationAsync_WhenCalled_ReturnsMappedDto()
        {
            var app = BuildApp(1);
            _appRepoMock.Setup(r => r.CreateAsync(It.IsAny<Application>())).ReturnsAsync(app);

            var result = await _sut.CreateApplicationAsync(
                new CreateApplicationDto { FormId = 10, UserId = app.UserId });

            Assert.Equal(1, result.ApplicationId);
            Assert.Equal(10, result.FormId);
        }

        [Fact]
        public async Task DeleteApplicationAsync_WhenFound_ReturnsTrue()
        {
            _appRepoMock.Setup(r => r.DeleteAsync(1, 5)).ReturnsAsync(true);

            var result = await _sut.DeleteApplicationAsync(1, 5);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteApplicationAsync_WhenNotFound_ReturnsFalse()
        {
            _appRepoMock.Setup(r => r.DeleteAsync(999, 5)).ReturnsAsync(false);

            var result = await _sut.DeleteApplicationAsync(999, 5);

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateApplicationAsync_WhenFound_ReturnsTrue()
        {
            var app = BuildApp(1);
            _appRepoMock.Setup(r => r.GetByIdAsync(1, 5)).ReturnsAsync(app);
            _appRepoMock.Setup(r => r.UpdateAsync(app)).ReturnsAsync(true);

            var result = await _sut.UpdateApplicationAsync(1, 5, new ApplicationResponseDto
            {
                ApplicationId = 1, FormId = 10, UserId = app.UserId, Status = "Approved"
            });

            Assert.True(result);
        }

        [Fact]
        public async Task UpdateApplicationAsync_WhenNotFound_ReturnsFalse()
        {
            _appRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync((Application?)null);

            var result = await _sut.UpdateApplicationAsync(999, 5, new ApplicationResponseDto
            {
                ApplicationId = 999, FormId = 10, UserId = Guid.NewGuid(), Status = "Pending"
            });

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateApplicationAsync_WhenIdMismatch_ReturnsFalse()
        {
            var app = BuildApp(1);
            _appRepoMock.Setup(r => r.GetByIdAsync(1, 5)).ReturnsAsync(app);

            var result = await _sut.UpdateApplicationAsync(1, 5, new ApplicationResponseDto
            {
                ApplicationId = 99, FormId = 10, UserId = Guid.NewGuid(), Status = "Pending"
            });

            Assert.False(result);
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Approved")]
        [InlineData("Rejected")]
        [InlineData("Success")]
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
        public async Task GetApplicationsByUserAsync_WhenCalled_ReturnsMappedList()
        {
            var userId = Guid.NewGuid();
            _appRepoMock.Setup(r => r.GetByUserIdAsync(userId))
                        .ReturnsAsync(new List<Application> { BuildApp(1, userId), BuildApp(2, userId) });

            var result = (await _sut.GetApplicationsByUserAsync(userId)).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(userId, r.UserId));
        }

        [Fact]
        public async Task GetApplicationsByFormAsync_WhenCalled_ReturnsMappedList()
        {
            _appRepoMock.Setup(r => r.GetByFormIdAsync(3, 5))
                        .ReturnsAsync(new List<Application> { BuildApp(1), BuildApp(2) });

            var result = (await _sut.GetApplicationsByFormAsync(3, 5)).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetApplicationsByStatusAsync_WhenCalled_ReturnsMappedList()
        {
            _appRepoMock.Setup(r => r.GetByStatusAsync("Pending", 5))
                        .ReturnsAsync(new List<Application> { BuildApp(1) });

            var result = (await _sut.GetApplicationsByStatusAsync("Pending", 5)).ToList();

            Assert.Single(result);
            Assert.Equal("Pending", result[0].Status);
        }

        [Fact]
        public async Task GetApplicationByUserAndFormAsync_WhenFound_ReturnsMappedDto()
        {
            var userId = Guid.NewGuid();
            _appRepoMock.Setup(r => r.GetByUserIdAndFormIdAsync(userId, 3))
                        .ReturnsAsync(BuildApp(1, userId));

            var result = await _sut.GetApplicationByUserAndFormAsync(userId, 3);

            Assert.NotNull(result);
            Assert.Equal(1, result!.ApplicationId);
            Assert.Equal(userId, result.UserId);
        }

        [Fact]
        public async Task GetApplicationByUserAndFormAsync_WhenNotFound_ReturnsNull()
        {
            _appRepoMock.Setup(r => r.GetByUserIdAndFormIdAsync(It.IsAny<Guid>(), It.IsAny<int>()))
                        .ReturnsAsync((Application?)null);

            var result = await _sut.GetApplicationByUserAndFormAsync(Guid.NewGuid(), 3);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetApplicationsByCampaignAsync_WhenCalled_ReturnsMappedList()
        {
            _appRepoMock.Setup(r => r.GetByCampaignIdAsync(2, 5, null))
                        .ReturnsAsync(new List<Application> { BuildApp(1), BuildApp(2) });

            var result = (await _sut.GetApplicationsByCampaignAsync(2, 5)).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetApplicationsByClubAsync_WhenCalled_ReturnsMappedList()
        {
            _appRepoMock.Setup(r => r.GetByClubIdAsync(5, null))
                        .ReturnsAsync(new List<Application> { BuildApp(1) });

            var result = (await _sut.GetApplicationsByClubAsync(5)).ToList();

            Assert.Single(result);
        }

        // ==================== FORM ====================

        [Fact]
        public async Task GetAllFormsAsync_WhenCalled_ReturnsMappedList()
        {
            _appRepoMock.Setup(r => r.GetAllFormsAsync(5))
                        .ReturnsAsync(new List<ApplicationForm> { BuildForm(1), BuildForm(2) });

            var result = (await _sut.GetAllFormsAsync(5)).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].FormId);
        }

        [Fact]
        public async Task GetFormsByCampaignAsync_WhenCalled_ReturnsMappedList()
        {
            _appRepoMock.Setup(r => r.GetFormsByCampaignIdAsync(2, 5))
                        .ReturnsAsync(new List<ApplicationForm> { BuildForm(1) });

            var result = (await _sut.GetFormsByCampaignAsync(2, 5)).ToList();

            Assert.Single(result);
        }

        [Fact]
        public async Task GetFormByIdAsync_WhenFound_ReturnsMappedDto()
        {
            _appRepoMock.Setup(r => r.GetFormByIdAsync(1)).ReturnsAsync(BuildForm(1));

            var result = await _sut.GetFormByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result!.FormId);
            Assert.Equal("Form A", result.FormName);
        }

        [Fact]
        public async Task GetFormByIdAsync_WhenNotFound_ReturnsNull()
        {
            _appRepoMock.Setup(r => r.GetFormByIdAsync(It.IsAny<int>()))
                        .ReturnsAsync((ApplicationForm?)null);

            var result = await _sut.GetFormByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateFormAsync_WhenCalled_ReturnsMappedDto()
        {
            var form = BuildForm(1);
            _appRepoMock.Setup(r => r.CreateFormAsync(It.IsAny<ApplicationForm>())).ReturnsAsync(form);

            var result = await _sut.CreateFormAsync(
                new CreateApplicationFormDto { CampaignId = 1, FormName = "Form A" });

            Assert.Equal(1, result.FormId);
            Assert.Equal("Form A", result.FormName);
        }

        [Fact]
        public async Task UpdateFormAsync_WhenFound_ReturnsTrue()
        {
            var form = BuildForm(1, clubId: 5);
            _appRepoMock.Setup(r => r.GetFormByIdAsync(1)).ReturnsAsync(form);
            _appRepoMock.Setup(r => r.UpdateFormAsync(form)).ReturnsAsync(true);

            var result = await _sut.UpdateFormAsync(1, 5, new ApplicationFormResponseDto
            {
                FormId = 1, CampaignId = 1, FormName = "Updated"
            });

            Assert.True(result);
        }

        [Fact]
        public async Task UpdateFormAsync_WhenNotFound_ReturnsFalse()
        {
            _appRepoMock.Setup(r => r.GetFormByIdAsync(It.IsAny<int>()))
                        .ReturnsAsync((ApplicationForm?)null);

            var result = await _sut.UpdateFormAsync(999, 5, new ApplicationFormResponseDto
            {
                FormId = 999, CampaignId = 1, FormName = "F"
            });

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateFormAsync_WhenClubMismatch_ReturnsFalse()
        {
            var form = BuildForm(1, clubId: 99);
            _appRepoMock.Setup(r => r.GetFormByIdAsync(1)).ReturnsAsync(form);

            var result = await _sut.UpdateFormAsync(1, 5, new ApplicationFormResponseDto
            {
                FormId = 1, CampaignId = 1, FormName = "F"
            });

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateFormAsync_WhenIdMismatch_ReturnsFalse()
        {
            var form = BuildForm(1, clubId: 5);
            _appRepoMock.Setup(r => r.GetFormByIdAsync(1)).ReturnsAsync(form);

            var result = await _sut.UpdateFormAsync(1, 5, new ApplicationFormResponseDto
            {
                FormId = 99, CampaignId = 1, FormName = "F"
            });

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteFormAsync_WhenFound_ReturnsTrue()
        {
            _appRepoMock.Setup(r => r.DeleteFormAsync(1, 5)).ReturnsAsync(true);

            var result = await _sut.DeleteFormAsync(1, 5);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteFormAsync_WhenNotFound_ReturnsFalse()
        {
            _appRepoMock.Setup(r => r.DeleteFormAsync(999, 5)).ReturnsAsync(false);

            var result = await _sut.DeleteFormAsync(999, 5);

            Assert.False(result);
        }

        // ==================== QUESTION ====================

        [Fact]
        public async Task GetQuestionsByFormAsync_WhenCalled_ReturnsMappedList()
        {
            _appRepoMock.Setup(r => r.GetQuestionsByFormIdAsync(10))
                        .ReturnsAsync(new List<ApplicationQuestion> { BuildQuestion(1, 10), BuildQuestion(2, 10) });

            var result = (await _sut.GetQuestionsByFormAsync(10)).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].QuestionId);
        }

        [Fact]
        public async Task GetQuestionByIdAsync_WhenFound_ReturnsMappedDto()
        {
            _appRepoMock.Setup(r => r.GetQuestionByIdAsync(1, 5)).ReturnsAsync(BuildQuestion(1, 10));

            var result = await _sut.GetQuestionByIdAsync(1, 5);

            Assert.NotNull(result);
            Assert.Equal(1, result!.QuestionId);
            Assert.Equal(10, result.FormId);
        }

        [Fact]
        public async Task GetQuestionByIdAsync_WhenNotFound_ReturnsNull()
        {
            _appRepoMock.Setup(r => r.GetQuestionByIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync((ApplicationQuestion?)null);

            var result = await _sut.GetQuestionByIdAsync(999, 5);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateQuestionAsync_WhenCalled_ReturnsMappedDto()
        {
            var question = BuildQuestion(1, 10);
            _appRepoMock.Setup(r => r.CreateQuestionAsync(It.IsAny<ApplicationQuestion>())).ReturnsAsync(question);

            var result = await _sut.CreateQuestionAsync(
                new CreateApplicationQuestionDto { FormId = 10, QuestionText = "Q?" });

            Assert.Equal(1, result.QuestionId);
            Assert.Equal("Q?", result.QuestionText);
        }

        [Fact]
        public async Task UpdateQuestionAsync_WhenFound_ReturnsTrue()
        {
            var question = BuildQuestion(1, 10);
            _appRepoMock.Setup(r => r.GetQuestionByIdAsync(1, 5)).ReturnsAsync(question);
            _appRepoMock.Setup(r => r.UpdateQuestionAsync(question)).ReturnsAsync(true);

            var result = await _sut.UpdateQuestionAsync(1, 5, new ApplicationQuestionResponseDto
            {
                QuestionId = 1, FormId = 10, QuestionText = "Updated Q?"
            });

            Assert.True(result);
        }

        [Fact]
        public async Task UpdateQuestionAsync_WhenNotFound_ReturnsFalse()
        {
            _appRepoMock.Setup(r => r.GetQuestionByIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync((ApplicationQuestion?)null);

            var result = await _sut.UpdateQuestionAsync(999, 5, new ApplicationQuestionResponseDto
            {
                QuestionId = 999, FormId = 10, QuestionText = "Q?"
            });

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateQuestionAsync_WhenIdMismatch_ReturnsFalse()
        {
            var question = BuildQuestion(1, 10);
            _appRepoMock.Setup(r => r.GetQuestionByIdAsync(1, 5)).ReturnsAsync(question);

            var result = await _sut.UpdateQuestionAsync(1, 5, new ApplicationQuestionResponseDto
            {
                QuestionId = 99, FormId = 10, QuestionText = "Q?"
            });

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteQuestionAsync_WhenFound_ReturnsTrue()
        {
            _appRepoMock.Setup(r => r.DeleteQuestionAsync(1, 5)).ReturnsAsync(true);

            var result = await _sut.DeleteQuestionAsync(1, 5);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteQuestionAsync_WhenNotFound_ReturnsFalse()
        {
            _appRepoMock.Setup(r => r.DeleteQuestionAsync(999, 5)).ReturnsAsync(false);

            var result = await _sut.DeleteQuestionAsync(999, 5);

            Assert.False(result);
        }

        // ==================== ANSWER ====================

        [Fact]
        public async Task GetAnswersByApplicationAsync_WhenCalled_ReturnsMappedList()
        {
            _appRepoMock.Setup(r => r.GetAnswersByApplicationIdAsync(1, 5))
                        .ReturnsAsync(new List<ApplicationAnswer> { BuildAnswer(1), BuildAnswer(2) });

            var result = (await _sut.GetAnswersByApplicationAsync(1, 5)).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].AnswerId);
        }

        [Fact]
        public async Task GetAnswerByIdAsync_WhenFound_ReturnsMappedDto()
        {
            _appRepoMock.Setup(r => r.GetAnswerByIdAsync(1, 5)).ReturnsAsync(BuildAnswer(1));

            var result = await _sut.GetAnswerByIdAsync(1, 5);

            Assert.NotNull(result);
            Assert.Equal(1, result!.AnswerId);
            Assert.Equal("My answer", result.AnswerText);
        }

        [Fact]
        public async Task GetAnswerByIdAsync_WhenNotFound_ReturnsNull()
        {
            _appRepoMock.Setup(r => r.GetAnswerByIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync((ApplicationAnswer?)null);

            var result = await _sut.GetAnswerByIdAsync(999, 5);

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
        public async Task CreateAnswerAsync_WhenQuestionNotFound_ThrowsArgumentException()
        {
            var app = BuildApp(1);
            _appRepoMock.Setup(r => r.GetByIdAsync(1, 5)).ReturnsAsync(app);
            _appRepoMock.Setup(r => r.GetQuestionByIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync((ApplicationQuestion?)null);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CreateAnswerAsync(
                    new CreateApplicationAnswerDto { ApplicationId = 1, QuestionId = 99, AnswerText = "A" }, 5));

            Assert.Contains("Câu hỏi không tồn tại", ex.Message);
        }

        [Fact]
        public async Task CreateAnswerAsync_WhenQuestionNotBelongToForm_ThrowsArgumentException()
        {
            var app = BuildApp(1, formId: 10);
            var question = BuildQuestion(1, formId: 99);
            _appRepoMock.Setup(r => r.GetByIdAsync(1, 5)).ReturnsAsync(app);
            _appRepoMock.Setup(r => r.GetQuestionByIdAsync(1, 5)).ReturnsAsync(question);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.CreateAnswerAsync(
                    new CreateApplicationAnswerDto { ApplicationId = 1, QuestionId = 1, AnswerText = "A" }, 5));

            Assert.Contains("không thuộc form", ex.Message);
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
            var answer = BuildAnswer(1);
            _appRepoMock.Setup(r => r.GetByIdAsync(1, 5)).ReturnsAsync(app);
            _appRepoMock.Setup(r => r.GetQuestionByIdAsync(1, 5)).ReturnsAsync(question);
            _appRepoMock.Setup(r => r.CreateAnswerAsync(It.IsAny<ApplicationAnswer>())).ReturnsAsync(answer);

            var result = await _sut.CreateAnswerAsync(
                new CreateApplicationAnswerDto { ApplicationId = 1, QuestionId = 1, AnswerText = "My answer" }, 5);

            Assert.Equal(1, result.AnswerId);
            Assert.Equal("My answer", result.AnswerText);
        }

        [Fact]
        public async Task UpdateAnswerAsync_WhenFound_ReturnsTrue()
        {
            var answer = BuildAnswer(1);
            _appRepoMock.Setup(r => r.GetAnswerByIdAsync(1, 5)).ReturnsAsync(answer);
            _appRepoMock.Setup(r => r.UpdateAnswerAsync(answer)).ReturnsAsync(true);

            var result = await _sut.UpdateAnswerAsync(1, 5, new ApplicationAnswerResponseDto
            {
                AnswerId = 1, ApplicationId = 1, QuestionId = 1, AnswerText = "Updated"
            });

            Assert.True(result);
        }

        [Fact]
        public async Task UpdateAnswerAsync_WhenNotFound_ReturnsFalse()
        {
            _appRepoMock.Setup(r => r.GetAnswerByIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync((ApplicationAnswer?)null);

            var result = await _sut.UpdateAnswerAsync(999, 5, new ApplicationAnswerResponseDto
            {
                AnswerId = 999, ApplicationId = 1, QuestionId = 1, AnswerText = "A"
            });

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAnswerAsync_WhenIdMismatch_ReturnsFalse()
        {
            var answer = BuildAnswer(1);
            _appRepoMock.Setup(r => r.GetAnswerByIdAsync(1, 5)).ReturnsAsync(answer);

            var result = await _sut.UpdateAnswerAsync(1, 5, new ApplicationAnswerResponseDto
            {
                AnswerId = 99, ApplicationId = 1, QuestionId = 1, AnswerText = "A"
            });

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAnswerAsync_WhenFound_ReturnsTrue()
        {
            _appRepoMock.Setup(r => r.DeleteAnswerAsync(1, 5)).ReturnsAsync(true);

            var result = await _sut.DeleteAnswerAsync(1, 5);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAnswerAsync_WhenNotFound_ReturnsFalse()
        {
            _appRepoMock.Setup(r => r.DeleteAnswerAsync(999, 5)).ReturnsAsync(false);

            var result = await _sut.DeleteAnswerAsync(999, 5);

            Assert.False(result);
        }

        // ==================== SUBMIT ====================

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_WhenUserNotFound_ThrowsArgumentException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.SubmitApplicationWithAnswersAsync(
                    new SubmitApplicationWithAnswersDto { UserId = Guid.NewGuid(), FormId = 1 }));

            Assert.Contains("Tài khoản không tồn tại", ex.Message);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_WhenFormNotFound_ThrowsArgumentException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new User());
            _appRepoMock.Setup(r => r.GetFormByIdAsync(It.IsAny<int>()))
                        .ReturnsAsync((ApplicationForm?)null);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.SubmitApplicationWithAnswersAsync(
                    new SubmitApplicationWithAnswersDto { UserId = Guid.NewGuid(), FormId = 99 }));

            Assert.Contains("Form không tồn tại", ex.Message);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_WhenAnswerQuestionNotInForm_ThrowsArgumentException()
        {
            var userId = Guid.NewGuid();
            var form = BuildForm(1);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User());
            _appRepoMock.Setup(r => r.GetFormByIdAsync(1)).ReturnsAsync(form);
            _appRepoMock.Setup(r => r.GetQuestionsByFormIdAsync(1))
                        .ReturnsAsync(new List<ApplicationQuestion> { BuildQuestion(1, 1) });

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.SubmitApplicationWithAnswersAsync(new SubmitApplicationWithAnswersDto
                {
                    UserId = userId,
                    FormId = 1,
                    Answers = new List<ApplicationAnswerItemDto>
                    {
                        new() { QuestionId = 99, AnswerText = "A" }
                    }
                }));

            Assert.Contains("không thuộc form", ex.Message);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_WhenMissingRequiredAnswers_ThrowsArgumentException()
        {
            var userId = Guid.NewGuid();
            var form = BuildForm(1);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User());
            _appRepoMock.Setup(r => r.GetFormByIdAsync(1)).ReturnsAsync(form);
            _appRepoMock.Setup(r => r.GetQuestionsByFormIdAsync(1))
                        .ReturnsAsync(new List<ApplicationQuestion> { BuildQuestion(1, 1, required: true) });

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.SubmitApplicationWithAnswersAsync(new SubmitApplicationWithAnswersDto
                {
                    UserId = userId,
                    FormId = 1,
                    Answers = new List<ApplicationAnswerItemDto>()
                }));

            Assert.Contains("bắt buộc", ex.Message);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_WhenNullAnswersAndRequiredExists_ThrowsArgumentException()
        {
            var userId = Guid.NewGuid();
            var form = BuildForm(1);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User());
            _appRepoMock.Setup(r => r.GetFormByIdAsync(1)).ReturnsAsync(form);
            _appRepoMock.Setup(r => r.GetQuestionsByFormIdAsync(1))
                        .ReturnsAsync(new List<ApplicationQuestion> { BuildQuestion(1, 1, required: true) });

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.SubmitApplicationWithAnswersAsync(new SubmitApplicationWithAnswersDto
                {
                    UserId = userId,
                    FormId = 1,
                    Answers = null!
                }));

            Assert.Contains("bắt buộc", ex.Message);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_WhenValid_ReturnsCreatedApplication()
        {
            var userId = Guid.NewGuid();
            var form = BuildForm(1);
            var createdApp = BuildApp(99, userId);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User());
            _appRepoMock.Setup(r => r.GetFormByIdAsync(1)).ReturnsAsync(form);
            _appRepoMock.Setup(r => r.GetQuestionsByFormIdAsync(1)).ReturnsAsync(new List<ApplicationQuestion>());
            _appRepoMock.Setup(r => r.CreateAsync(It.IsAny<Application>())).ReturnsAsync(createdApp);

            var result = await _sut.SubmitApplicationWithAnswersAsync(
                new SubmitApplicationWithAnswersDto { UserId = userId, FormId = 1, Answers = null });

            Assert.Equal(99, result.ApplicationId);
            Assert.Equal("Pending", result.Status);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_WithValidAnswers_CallsCreateAnswers()
        {
            var userId = Guid.NewGuid();
            var form = BuildForm(1);
            var createdApp = BuildApp(99, userId);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User());
            _appRepoMock.Setup(r => r.GetFormByIdAsync(1)).ReturnsAsync(form);
            _appRepoMock.Setup(r => r.GetQuestionsByFormIdAsync(1))
                        .ReturnsAsync(new List<ApplicationQuestion> { BuildQuestion(1, 1, required: true) });
            _appRepoMock.Setup(r => r.CreateAsync(It.IsAny<Application>())).ReturnsAsync(createdApp);
            _appRepoMock.Setup(r => r.CreateAnswersAsync(It.IsAny<IEnumerable<ApplicationAnswer>>()))
                        .Returns(Task.CompletedTask);

            var result = await _sut.SubmitApplicationWithAnswersAsync(new SubmitApplicationWithAnswersDto
            {
                UserId = userId,
                FormId = 1,
                Answers = new List<ApplicationAnswerItemDto>
                {
                    new() { QuestionId = 1, AnswerText = "My answer" }
                }
            });

            Assert.Equal(99, result.ApplicationId);
            _appRepoMock.Verify(r => r.CreateAnswersAsync(It.IsAny<IEnumerable<ApplicationAnswer>>()), Times.Once);
        }
    }
}
