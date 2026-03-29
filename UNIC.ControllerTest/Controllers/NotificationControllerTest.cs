using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Controllers;

namespace UNIC.ServiceTest.Controllers
{

    public class NotificationControllerTest
    {
        private readonly Mock<INotificationService> _serviceMock = new();
        private readonly NotificationController _sut;

        private static T? Prop<T>(object? obj, string name) =>
            (T?)obj?.GetType().GetProperty(name)?.GetValue(obj);

        public NotificationControllerTest()
        {
            _sut = new NotificationController(_serviceMock.Object);
        }

        [Fact]
        public async Task GetByUserId_WhenCalled_Returns200WithData()
        {
            var userId = Guid.NewGuid();
            var dtos = new List<NotificationDto>
        {
            new() { NotificationId = 1, UserId = userId, Title = "T", Message = "M", Type = "X", IsRead = false, CreatedAt = DateTime.Now }
        };
            _serviceMock.Setup(s => s.GetByUserIdAsync(userId)).ReturnsAsync(dtos);

            var result = await _sut.GetByUserId(userId);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(Prop<bool>(ok.Value, "success"));
            Assert.Single(Prop<IEnumerable<NotificationDto>>(ok.Value, "data")!);
        }

        [Fact]
        public async Task MarkAsRead_WhenFound_Returns200()
        {
            _serviceMock.Setup(s => s.MarkAsReadAsync(1)).ReturnsAsync(true);

            var result = await _sut.MarkAsRead(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task MarkAsRead_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.MarkAsReadAsync(999)).ReturnsAsync(false);

            var result = await _sut.MarkAsRead(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task MarkAllAsRead_WhenCalled_Returns200()
        {
            var userId = Guid.NewGuid();
            _serviceMock.Setup(s => s.MarkAllAsReadAsync(userId)).ReturnsAsync(true);

            var result = await _sut.MarkAllAsRead(userId);

            Assert.IsType<OkObjectResult>(result);
            _serviceMock.Verify(s => s.MarkAllAsReadAsync(userId), Times.Once);
        }
    }
}
