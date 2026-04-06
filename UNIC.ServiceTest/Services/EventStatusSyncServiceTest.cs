using BusinessLogic.Services.Background;
using DataAccess.Repositories.Interface;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class EventStatusSyncServiceTest
    {
        private readonly Mock<ILogger<EventStatusSyncService>> _mockLogger;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IEventRepository> _mockEventRepo;

        public EventStatusSyncServiceTest()
        {
            _mockLogger = new Mock<ILogger<EventStatusSyncService>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockScope = new Mock<IServiceScope>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockEventRepo = new Mock<IEventRepository>();

            _mockUnitOfWork.Setup(u => u.Events).Returns(_mockEventRepo.Object);

            var scopeServiceProvider = new Mock<IServiceProvider>();
            scopeServiceProvider.Setup(s => s.GetService(typeof(IUnitOfWork))).Returns(_mockUnitOfWork.Object);

            _mockScope.Setup(s => s.ServiceProvider).Returns(scopeServiceProvider.Object);
            _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);
            _mockServiceProvider.Setup(s => s.GetService(typeof(IServiceScopeFactory))).Returns(_mockScopeFactory.Object);
        }

        /// <summary>
        /// Helper: starts ExecuteAsync with a CancellationToken that cancels after a delay.
        /// </summary>
        private async Task RunServiceOnceAsync(int delayMs = 100)
        {
            var cts = new CancellationTokenSource();
            var service = new EventStatusSyncService(_mockLogger.Object, _mockServiceProvider.Object);

            // Start the service, then cancel after a short delay
            cts.CancelAfter(delayMs);
            try
            {
                await service.StartAsync(cts.Token);
                await Task.Delay(delayMs + 200); // allow one iteration
                await service.StopAsync(CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        [Fact]
        public async Task ExecuteAsync_CallsBulkSyncStatus()
        {
            _mockEventRepo.Setup(r => r.BulkSyncStatusAsync()).ReturnsAsync(0);

            await RunServiceOnceAsync();

            _mockEventRepo.Verify(r => r.BulkSyncStatusAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_LogsUpdatedCount_WhenChangesExist()
        {
            _mockEventRepo.Setup(r => r.BulkSyncStatusAsync()).ReturnsAsync(3);

            await RunServiceOnceAsync();

            _mockEventRepo.Verify(r => r.BulkSyncStatusAsync(), Times.AtLeastOnce);
            // Logger should have been called with the count
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("3")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_NoLog_WhenNoChanges()
        {
            _mockEventRepo.Setup(r => r.BulkSyncStatusAsync()).ReturnsAsync(0);

            await RunServiceOnceAsync();

            // Should NOT log "updated X event(s)" when count = 0
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("updated")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_HandlesException_Gracefully()
        {
            _mockEventRepo.Setup(r => r.BulkSyncStatusAsync()).ThrowsAsync(new Exception("DB error"));

            // Act — should not throw
            await RunServiceOnceAsync();

            // Assert: error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_StopsOnCancellation()
        {
            int callCount = 0;
            _mockEventRepo.Setup(r => r.BulkSyncStatusAsync())
                .ReturnsAsync(() => { Interlocked.Increment(ref callCount); return 0; });

            var cts = new CancellationTokenSource();
            var service = new EventStatusSyncService(_mockLogger.Object, _mockServiceProvider.Object);

            // Start the service, let it run briefly, then cancel
            await service.StartAsync(cts.Token);
            await Task.Delay(200);
            cts.Cancel();
            await Task.Delay(300);
            await service.StopAsync(CancellationToken.None);

            // Capture the count after stop, wait more, verify no new calls
            int countAfterStop = callCount;
            await Task.Delay(500);
            callCount.Should().Be(countAfterStop, "no more iterations should run after cancellation");
        }
    }
}
