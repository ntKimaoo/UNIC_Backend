using BusinessLogic.Services.Background;
using BusinessLogic.Services.Interface;
using DataAccess.Enums;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class EventReminderServiceTest
    {
        private readonly Mock<ILogger<EventReminderService>> _mockLogger;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IEventRepository> _mockEventRepo;
        private readonly Mock<IAttendanceRepository> _mockAttendanceRepo;

        public EventReminderServiceTest()
        {
            _mockLogger = new Mock<ILogger<EventReminderService>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockScope = new Mock<IServiceScope>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockEmailService = new Mock<IEmailService>();
            _mockEventRepo = new Mock<IEventRepository>();
            _mockAttendanceRepo = new Mock<IAttendanceRepository>();

            _mockUnitOfWork.Setup(u => u.Events).Returns(_mockEventRepo.Object);
            _mockUnitOfWork.Setup(u => u.Attendances).Returns(_mockAttendanceRepo.Object);

            var scopeServiceProvider = new Mock<IServiceProvider>();
            scopeServiceProvider.Setup(s => s.GetService(typeof(IUnitOfWork))).Returns(_mockUnitOfWork.Object);
            scopeServiceProvider.Setup(s => s.GetService(typeof(IEmailService))).Returns(_mockEmailService.Object);

            _mockScope.Setup(s => s.ServiceProvider).Returns(scopeServiceProvider.Object);
            _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);
            _mockServiceProvider.Setup(s => s.GetService(typeof(IServiceScopeFactory))).Returns(_mockScopeFactory.Object);
        }

        /// <summary>
        /// Helper: creates the service and uses reflection to invoke ProcessRemindersAsync.
        /// We test the private method via reflection since ExecuteAsync runs in a loop.
        /// </summary>
        private async Task InvokeProcessRemindersAsync()
        {
            var service = new EventReminderService(_mockLogger.Object, _mockServiceProvider.Object);
            var method = typeof(EventReminderService)
                .GetMethod("ProcessRemindersAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Should().NotBeNull("ProcessRemindersAsync must exist");
            await (Task)method!.Invoke(service, null)!;
        }

        [Fact]
        public async Task ProcessRemindersAsync_SendsEmails_WhenUpcomingEventsExist()
        {
            // Arrange: event starting in 6 hours with 1 registered user
            var ev = new Event
            {
                EventId = 100,
                EventName = "Tech Talk",
                StartDate = DateTime.Now.AddHours(6),
                Status = "REGISTRATION_OPEN",
                Location = "Room A"
            };
            var user = new User { Email = "user@test.com", FullName = "Test User" };
            var attendance = new Attendance
            {
                EventId = 100,
                UserId = Guid.NewGuid(),
                AttendanceStatus = nameof(AttendanceStatus.REGISTERED),
                User = user
            };

            _mockEventRepo.Setup(r => r.GetUpcomingEventsAsync()).ReturnsAsync(new List<Event> { ev });
            _mockAttendanceRepo.Setup(r => r.GetAttendeesByEventAsync(100)).ReturnsAsync(new List<Attendance> { attendance });

            // Act
            await InvokeProcessRemindersAsync();

            // Assert: email was sent
            //
            // Implementation sends email via fire-and-forget Task.Run, so we need to wait
            // briefly for the invocation to happen before verifying.
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _mockEmailService
                .Setup(e => e.SendEventRegistrationSuccessAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>()))
                .Callback(() => tcs.TrySetResult(true))
                .ReturnsAsync(true);

            await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));

            _mockEmailService.Verify(
                e => e.SendEventRegistrationSuccessAsync(
                    It.Is<string>(s => s == "user@test.com"),
                    It.IsAny<string>(),
                    It.Is<string>(s => s.Contains("Tech Talk")),
                    It.IsAny<DateTime?>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRemindersAsync_SkipsCanceledEvents()
        {
            // Arrange: canceled event within window — should be skipped
            var ev = new Event
            {
                EventId = 200,
                EventName = "Canceled Event",
                StartDate = DateTime.Now.AddHours(3),
                Status = "CANCELED"
            };

            _mockEventRepo.Setup(r => r.GetUpcomingEventsAsync()).ReturnsAsync(new List<Event> { ev });

            // Act
            await InvokeProcessRemindersAsync();

            // Assert: no emails sent
            _mockEmailService.Verify(
                e => e.SendEventRegistrationSuccessAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<string?>()),
                Times.Never);
        }

        [Fact]
        public async Task ProcessRemindersAsync_SkipsEventsOutsideWindow()
        {
            // Arrange: event starting in 20 hours (outside 12.5h window)
            var ev = new Event
            {
                EventId = 300,
                EventName = "Far Event",
                StartDate = DateTime.Now.AddHours(20),
                Status = "REGISTRATION_OPEN"
            };

            _mockEventRepo.Setup(r => r.GetUpcomingEventsAsync()).ReturnsAsync(new List<Event> { ev });

            // Act
            await InvokeProcessRemindersAsync();

            // Assert: no emails sent (outside window)
            _mockEmailService.Verify(
                e => e.SendEventRegistrationSuccessAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<string?>()),
                Times.Never);
        }

        [Fact]
        public async Task ProcessRemindersAsync_SkipsPastEvents()
        {
            // Arrange: event already started (in the past)
            var ev = new Event
            {
                EventId = 400,
                EventName = "Past Event",
                StartDate = DateTime.Now.AddHours(-1),
                Status = "ONGOING"
            };

            _mockEventRepo.Setup(r => r.GetUpcomingEventsAsync()).ReturnsAsync(new List<Event> { ev });

            // Act
            await InvokeProcessRemindersAsync();

            // Assert: no emails sent
            _mockEmailService.Verify(
                e => e.SendEventRegistrationSuccessAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<string?>()),
                Times.Never);
        }

        [Fact]
        public async Task ProcessRemindersAsync_HandlesNoRegisteredUsers()
        {
            // Arrange: event with only WAITLIST users
            var ev = new Event
            {
                EventId = 500,
                EventName = "Empty Event",
                StartDate = DateTime.Now.AddHours(5),
                Status = "REGISTRATION_OPEN"
            };
            var attendance = new Attendance
            {
                EventId = 500,
                AttendanceStatus = nameof(AttendanceStatus.WAITLIST),
                User = new User { Email = "wait@test.com", FullName = "Waiter" }
            };

            _mockEventRepo.Setup(r => r.GetUpcomingEventsAsync()).ReturnsAsync(new List<Event> { ev });
            _mockAttendanceRepo.Setup(r => r.GetAttendeesByEventAsync(500)).ReturnsAsync(new List<Attendance> { attendance });

            // Act
            await InvokeProcessRemindersAsync();

            // Assert: no emails sent (WAITLIST != REGISTERED)
            _mockEmailService.Verify(
                e => e.SendEventRegistrationSuccessAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<string?>()),
                Times.Never);
        }

        [Fact]
        public async Task ProcessRemindersAsync_HandlesNoUpcomingEvents()
        {
            _mockEventRepo.Setup(r => r.GetUpcomingEventsAsync()).ReturnsAsync(new List<Event>());

            // Act — should not throw
            await InvokeProcessRemindersAsync();

            // Assert: no crash, no emails
            _mockEmailService.Verify(
                e => e.SendEventRegistrationSuccessAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<string?>()),
                Times.Never);
        }
    }
}
