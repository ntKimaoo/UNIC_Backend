using BusinessLogic.DTOs;
using BusinessLogic.Hubs;
using BusinessLogic.Services.Implementation;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Moq;

namespace UNIC.ServiceTest.Services
{

    public class NotificationServiceTest
    {
        private readonly Mock<INotificationRepository> _repoMock = new();
        private readonly Mock<INotificationHubContext> _hubMock = new();
        private readonly NotificationService _sut;

        public NotificationServiceTest()
        {
            _sut = new NotificationService(_repoMock.Object, _hubMock.Object);
        }

        [Fact]
        public async Task GetByUserIdAsync_WhenNotificationsExist_ReturnsMappedDtos()
        {
            var userId = Guid.NewGuid();
            var notifications = new List<Notification>
        {
            new() { NotificationId = 1, UserId = userId, Title = "T1", Message = "M1", Type = "INFO", IsRead = false, CreatedAt = DateTime.Now },
            new() { NotificationId = 2, UserId = userId, Title = "T2", Message = "M2", Type = "WARN", IsRead = true,  CreatedAt = DateTime.Now }
        };
            _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(notifications);

            var result = (await _sut.GetByUserIdAsync(userId)).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].NotificationId);
            Assert.Equal("T1", result[0].Title);
            Assert.False(result[0].IsRead);
            Assert.True(result[1].IsRead);
        }

        [Fact]
        public async Task GetByUserIdAsync_WhenNoNotifications_ReturnsEmpty()
        {
            _repoMock.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>()))
                     .ReturnsAsync(new List<Notification>());

            var result = await _sut.GetByUserIdAsync(Guid.NewGuid());

            Assert.Empty(result);
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenNotificationExists_ReturnsTrue()
        {
            _repoMock.Setup(r => r.MarkAsReadAsync(1)).ReturnsAsync(true);

            var result = await _sut.MarkAsReadAsync(1);

            Assert.True(result);
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenNotificationNotFound_ReturnsFalse()
        {
            _repoMock.Setup(r => r.MarkAsReadAsync(999)).ReturnsAsync(false);

            var result = await _sut.MarkAsReadAsync(999);

            Assert.False(result);
        }

        [Fact]
        public async Task MarkAllAsReadAsync_WhenCalled_DelegatesToRepositoryAndReturns()
        {
            var userId = Guid.NewGuid();
            _repoMock.Setup(r => r.MarkAllAsReadAsync(userId)).ReturnsAsync(true);

            var result = await _sut.MarkAllAsReadAsync(userId);

            Assert.True(result);
            _repoMock.Verify(r => r.MarkAllAsReadAsync(userId), Times.Once);
        }

        [Fact]
        public async Task SendNotificationAsync_WhenCalled_PersistsNotificationAndPushesViaHub()
        {
            var userId = Guid.NewGuid();
            var saved = new Notification
            {
                NotificationId = 42,
                UserId = userId,
                Title = "Title",
                Message = "Msg",
                Type = "TEST",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Notification>())).ReturnsAsync(saved);
            _hubMock.Setup(h => h.SendToUserAsync(userId, It.IsAny<object>())).Returns(Task.CompletedTask);

            await _sut.SendNotificationAsync(userId, "Title", "Msg", "TEST");

            _repoMock.Verify(r => r.CreateAsync(It.Is<Notification>(n =>
                n.UserId == userId && n.Title == "Title" && n.Message == "Msg" && !n.IsRead)), Times.Once);
            _hubMock.Verify(h => h.SendToUserAsync(
                userId, It.Is<NotificationDto>(d => d.NotificationId == 42 && d.Title == "Title")), Times.Once);
        }

        [Fact]
        public async Task SendNotificationAsync_WhenRepositoryThrows_PropagatesException()
        {
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                     .ThrowsAsync(new Exception("DB error"));

            var ex = await Assert.ThrowsAsync<Exception>(
                () => _sut.SendNotificationAsync(Guid.NewGuid(), "T", "M", "X"));

            Assert.Equal("DB error", ex.Message);
            _hubMock.Verify(h => h.SendToUserAsync(It.IsAny<Guid>(), It.IsAny<object>()), Times.Never);
        }
    }
}