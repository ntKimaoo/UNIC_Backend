using BusinessLogic.DTOs;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.BusinessLogic.Constants;
using UNIC.BusinessLogic.DTOs;
using UNIC.BusinessLogic.Services.Implementation;
using UNIC.DataAccess.Repositories.Interface;
using Xunit;

namespace UNIC.BusinessLogic.Test.Services
{
    public class ApplicationServiceTest
    {
        private readonly Mock<IApplicationRepository> _mockApplicationRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly ApplicationService _applicationService;

        public ApplicationServiceTest()
        {
            _mockApplicationRepo = new Mock<IApplicationRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _applicationService = new ApplicationService(_mockApplicationRepo.Object, _mockUserRepo.Object);
        }

        #region Application Tests

        [Fact]
        public async Task CreateApplicationAsync_ShouldReturnDto()
        {
            // Arrange
            var request = new CreateApplicationDto { FormId = 1, UserId = Guid.NewGuid() };
            var createdApp = new Application { ApplicationId = 1, FormId = 1, UserId = request.UserId, Status = ApplicationStatus.Pending };
            
            _mockApplicationRepo.Setup(r => r.CreateAsync(It.IsAny<Application>())).ReturnsAsync(createdApp);

            // Act
            var result = await _applicationService.CreateApplicationAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ApplicationId);
            Assert.Equal(ApplicationStatus.Pending, result.Status);
        }

        [Fact]
        public async Task UpdateApplicationStatusAsync_ShouldThrowException_WhenInvalidStatus()
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _applicationService.UpdateApplicationStatusAsync(1, "InvalidStatus"));
            Assert.Contains("Invalid status", ex.Message);
        }

        [Fact]
        public async Task UpdateApplicationStatusAsync_ShouldUpdateAndReturnDto_WhenValid()
        {
            // Arrange
            var app = new Application { ApplicationId = 1, Status = ApplicationStatus.Pending };
            _mockApplicationRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(app);
            _mockApplicationRepo.Setup(r => r.UpdateAsync(app)).ReturnsAsync(true);

            // Act
            var result = await _applicationService.UpdateApplicationStatusAsync(1, ApplicationStatus.Approved);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ApplicationStatus.Approved, result.Status);
        }

        #endregion

        #region SubmitApplicationWithAnswersAsync Tests

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_ShouldThrowException_WhenUserNotFound()
        {
            // Arrange
            var request = new SubmitApplicationWithAnswersDto { UserId = Guid.NewGuid() };
            _mockUserRepo.Setup(r => r.GetByIdAsync(request.UserId)).ReturnsAsync((User?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _applicationService.SubmitApplicationWithAnswersAsync(request));
            Assert.Contains("Tài khoản không tồn tại", ex.Message);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_ShouldThrowException_WhenFormNotFound()
        {
            // Arrange
            var request = new SubmitApplicationWithAnswersDto { UserId = Guid.NewGuid(), FormId = 1 };
            _mockUserRepo.Setup(r => r.GetByIdAsync(request.UserId)).ReturnsAsync(new User());
            _mockApplicationRepo.Setup(r => r.GetFormByIdAsync(request.FormId)).ReturnsAsync((ApplicationForm?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _applicationService.SubmitApplicationWithAnswersAsync(request));
            Assert.Contains("Form không tồn tại", ex.Message);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_ShouldThrowException_WhenQuestionNotBelongToForm()
        {
            // Arrange
            var request = new SubmitApplicationWithAnswersDto 
            { 
                UserId = Guid.NewGuid(), FormId = 1, 
                Answers = new List<ApplicationAnswerItemDto> { new ApplicationAnswerItemDto { QuestionId = 99 } } 
            };
            
            _mockUserRepo.Setup(r => r.GetByIdAsync(request.UserId)).ReturnsAsync(new User());
            _mockApplicationRepo.Setup(r => r.GetFormByIdAsync(request.FormId)).ReturnsAsync(new ApplicationForm());
            _mockApplicationRepo.Setup(r => r.GetQuestionsByFormIdAsync(request.FormId)).ReturnsAsync(new List<ApplicationQuestion>());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _applicationService.SubmitApplicationWithAnswersAsync(request));
            Assert.Contains("không thuộc form này", ex.Message);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_ShouldThrowException_WhenRequiredAnswerIsMissing()
        {
            // Arrange
            var request = new SubmitApplicationWithAnswersDto 
            { 
                UserId = Guid.NewGuid(), FormId = 1, 
                Answers = new List<ApplicationAnswerItemDto>() 
            };
            var questions = new List<ApplicationQuestion> { new ApplicationQuestion { QuestionId = 1, IsRequired = true } };
            
            _mockUserRepo.Setup(r => r.GetByIdAsync(request.UserId)).ReturnsAsync(new User());
            _mockApplicationRepo.Setup(r => r.GetFormByIdAsync(request.FormId)).ReturnsAsync(new ApplicationForm());
            _mockApplicationRepo.Setup(r => r.GetQuestionsByFormIdAsync(request.FormId)).ReturnsAsync(questions);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _applicationService.SubmitApplicationWithAnswersAsync(request));
            Assert.Contains("Form có câu hỏi bắt buộc nhưng không có câu trả lời nào", ex.Message);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_ShouldThrowException_WhenAnswerIsEmptyForRequiredQuestion()
        {
            // Arrange
            var request = new SubmitApplicationWithAnswersDto 
            { 
                UserId = Guid.NewGuid(), FormId = 1, 
                Answers = new List<ApplicationAnswerItemDto> { new ApplicationAnswerItemDto { QuestionId = 1, AnswerText = "" } } 
            };
            var questions = new List<ApplicationQuestion> { new ApplicationQuestion { QuestionId = 1, IsRequired = true } };
            
            _mockUserRepo.Setup(r => r.GetByIdAsync(request.UserId)).ReturnsAsync(new User());
            _mockApplicationRepo.Setup(r => r.GetFormByIdAsync(request.FormId)).ReturnsAsync(new ApplicationForm());
            _mockApplicationRepo.Setup(r => r.GetQuestionsByFormIdAsync(request.FormId)).ReturnsAsync(questions);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _applicationService.SubmitApplicationWithAnswersAsync(request));
            Assert.Contains("chưa có câu trả lời", ex.Message);
        }

        [Fact]
        public async Task SubmitApplicationWithAnswersAsync_ShouldCreateApplicationAndAnswers_WhenValid()
        {
            // Arrange
            var request = new SubmitApplicationWithAnswersDto 
            { 
                UserId = Guid.NewGuid(), FormId = 1, 
                Answers = new List<ApplicationAnswerItemDto> { new ApplicationAnswerItemDto { QuestionId = 1, AnswerText = "Yes" } } 
            };
            var questions = new List<ApplicationQuestion> { new ApplicationQuestion { QuestionId = 1, IsRequired = true } };
            var createdApp = new Application { ApplicationId = 10, FormId = 1, UserId = request.UserId };
            
            _mockUserRepo.Setup(r => r.GetByIdAsync(request.UserId)).ReturnsAsync(new User());
            _mockApplicationRepo.Setup(r => r.GetFormByIdAsync(request.FormId)).ReturnsAsync(new ApplicationForm());
            _mockApplicationRepo.Setup(r => r.GetQuestionsByFormIdAsync(request.FormId)).ReturnsAsync(questions);
            _mockApplicationRepo.Setup(r => r.CreateAsync(It.IsAny<Application>())).ReturnsAsync(createdApp);

            // Act
            var result = await _applicationService.SubmitApplicationWithAnswersAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.ApplicationId);
            _mockApplicationRepo.Verify(r => r.CreateAsync(It.IsAny<Application>()), Times.Once);
            _mockApplicationRepo.Verify(r => r.CreateAnswersAsync(It.IsNotNull<List<ApplicationAnswer>>()), Times.Once);
        }

        #endregion
        
        #region Form Tests
        
        [Fact]
        public async Task CreateFormAsync_ShouldReturnDto()
        {
            // Arrange
            var request = new CreateApplicationFormDto { CampaignId = 1, FormName = "A Form" };
            var createdForm = new ApplicationForm { FormId = 1, CampaignId = 1, FormName = "A Form" };
            
            _mockApplicationRepo.Setup(r => r.CreateFormAsync(It.IsAny<ApplicationForm>())).ReturnsAsync(createdForm);

            // Act
            var result = await _applicationService.CreateFormAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.FormId);
        }
        
        #endregion

        #region Other methods (simplified mapping tests coverage)
        
        [Fact]
        public async Task GetAllApplicationsAsync_ShouldReturnList()
        {
            _mockApplicationRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Application> { new Application { ApplicationId = 1 } });
            var result = await _applicationService.GetAllApplicationsAsync();
            Assert.Single(result);
        }

        [Fact]
        public async Task GetApplicationByIdAsync_ShouldReturnNullOrDto()
        {
            _mockApplicationRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Application { ApplicationId = 1 });
            _mockApplicationRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync((Application?)null);
            
            Assert.NotNull(await _applicationService.GetApplicationByIdAsync(1));
            Assert.Null(await _applicationService.GetApplicationByIdAsync(2));
        }

        #endregion
    }
}
