using Microsoft.AspNetCore.Mvc;
using Moq;
using UNIC.BusinessLogic.DTOs;
using UNIC.BusinessLogic.Services.Interface;
using UNIC.Presentation.Controllers;

namespace UNIC.ServiceTest.Controllers
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

        private static ApplicationResponseDto BuildDto(int id = 1) => new()
        {
            ApplicationId = id,
            FormId = 10,
            UserId = Guid.NewGuid(),
            SubmissionDate = DateTime.UtcNow,
            Status = "Pending"
        };

        [Fact]
        public async Task GetAllApplications_WhenCalled_Returns200WithList()
        {
            _serviceMock.Setup(s => s.GetAllApplicationsAsync(5))
                        .ReturnsAsync(new List<ApplicationResponseDto> { BuildDto(), BuildDto(2) });

            var result = await _sut.GetAllApplications(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task GetApplicationById_WhenFound_Returns200WithDto()
        {
            _serviceMock.Setup(s => s.GetApplicationByIdAsync(1, 5)).ReturnsAsync(BuildDto(1));

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
        public async Task CreateApplication_WhenCalled_Returns200WithCreatedDto()
        {
            _serviceMock.Setup(s => s.CreateApplicationAsync(It.IsAny<CreateApplicationDto>()))
                        .ReturnsAsync(BuildDto(1));

            var result = await _sut.CreateApplication(
                new CreateApplicationDto { FormId = 1, UserId = Guid.NewGuid() });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, Prop<ApplicationResponseDto>(ok.Value, "data")!.ApplicationId);
        }

        [Fact]
        public async Task UpdateApplicationStatus_WhenValid_Returns200()
        {
            var updated = BuildDto(1);
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

        [Fact]
        public async Task SubmitApplication_WhenValid_Returns200()
        {
            _serviceMock.Setup(s => s.SubmitApplicationWithAnswersAsync(5, It.IsAny<SubmitApplicationWithAnswersDto>()))
                        .ReturnsAsync(BuildDto(1));

            var result = await _sut.SubmitApplication(5,
                new SubmitApplicationWithAnswersDto { UserId = Guid.NewGuid(), FormId = 1 });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
        }

        [Fact]
        public async Task SubmitApplication_WhenExceptionThrown_Returns400WithMessage()
        {
            _serviceMock.Setup(s => s.SubmitApplicationWithAnswersAsync(5, It.IsAny<SubmitApplicationWithAnswersDto>()))
                        .ThrowsAsync(new ArgumentException("Tài khoản không tồn tại."));

            var result = await _sut.SubmitApplication(5,
                new SubmitApplicationWithAnswersDto { UserId = Guid.NewGuid(), FormId = 1 });

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Tài khoản không tồn tại", Prop<string>(bad.Value, "message"));
        }
    }
}
