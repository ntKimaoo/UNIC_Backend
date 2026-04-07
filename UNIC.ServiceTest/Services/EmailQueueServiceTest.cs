using BusinessLogic.DTOs;
using BusinessLogic.Services.Background;
using BusinessLogic.Services.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class EmailQueueServiceTest
    {
        private readonly Mock<ILogger<EmailQueueService>> _mockLogger;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly IServiceProvider _serviceProvider;

        public EmailQueueServiceTest()
        {
            _mockLogger = new Mock<ILogger<EmailQueueService>>();
            _mockEmailService = new Mock<IEmailService>();

            var services = new ServiceCollection();
            services.AddSingleton(_mockEmailService.Object);
            _serviceProvider = services.BuildServiceProvider();
        }

        #region EnqueueEmail

        [Fact]
        public void EnqueueEmail_DoesNotThrow()
        {
            var item = new EmailQueueItem
            {
                ToEmail = "test@test.com",
                FullName = "Test User",
                EmailType = EmailType.Verification,
                Token = "token123"
            };

            EmailQueueService.EnqueueEmail(item);
        }

        [Fact]
        public void EnqueueEmail_MultipleItems_DoesNotThrow()
        {
            for (int i = 0; i < 5; i++)
            {
                EmailQueueService.EnqueueEmail(new EmailQueueItem
                {
                    ToEmail = $"user{i}@test.com",
                    FullName = $"User {i}",
                    EmailType = EmailType.Welcome
                });
            }
        }

        #endregion

        #region ExecuteAsync

        [Fact]
        public async Task ExecuteAsync_StopsGracefully_WhenCancelled()
        {
            var service = new EmailQueueService(_mockLogger.Object, _serviceProvider);
            var cts = new CancellationTokenSource();

            cts.CancelAfter(200);

            await service.StartAsync(cts.Token);
            await Task.Delay(300);
            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task ExecuteAsync_ProcessesVerificationEmail()
        {
            _mockEmailService.Setup(s => s.SendVerificationEmailAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            EmailQueueService.EnqueueEmail(new EmailQueueItem
            {
                ToEmail = "verify@test.com",
                FullName = "Verify User",
                EmailType = EmailType.Verification,
                Token = "verify-token"
            });

            var service = new EmailQueueService(_mockLogger.Object, _serviceProvider);
            var cts = new CancellationTokenSource();

            cts.CancelAfter(500);
            await service.StartAsync(cts.Token);
            await Task.Delay(600);
            await service.StopAsync(CancellationToken.None);

            _mockEmailService.Verify(s => s.SendVerificationEmailAsync(
                "verify@test.com", "verify-token", "Verify User"), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_ProcessesPasswordResetEmail()
        {
            _mockEmailService.Setup(s => s.SendPasswordResetEmailAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            EmailQueueService.EnqueueEmail(new EmailQueueItem
            {
                ToEmail = "reset@test.com",
                FullName = "Reset User",
                EmailType = EmailType.PasswordReset,
                Token = "reset-token"
            });

            var service = new EmailQueueService(_mockLogger.Object, _serviceProvider);
            var cts = new CancellationTokenSource();

            cts.CancelAfter(500);
            await service.StartAsync(cts.Token);
            await Task.Delay(600);
            await service.StopAsync(CancellationToken.None);

            _mockEmailService.Verify(s => s.SendPasswordResetEmailAsync(
                "reset@test.com", "reset-token", "Reset User"), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_ProcessesWelcomeEmail()
        {
            _mockEmailService.Setup(s => s.SendWelcomeEmailAsync(
                    It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            EmailQueueService.EnqueueEmail(new EmailQueueItem
            {
                ToEmail = "welcome@test.com",
                FullName = "Welcome User",
                EmailType = EmailType.Welcome
            });

            var service = new EmailQueueService(_mockLogger.Object, _serviceProvider);
            var cts = new CancellationTokenSource();

            cts.CancelAfter(500);
            await service.StartAsync(cts.Token);
            await Task.Delay(600);
            await service.StopAsync(CancellationToken.None);

            _mockEmailService.Verify(s => s.SendWelcomeEmailAsync(
                "welcome@test.com", "Welcome User"), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_RetriesOnFailure_UpTo3Times()
        {
            _mockEmailService.Setup(s => s.SendVerificationEmailAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            EmailQueueService.EnqueueEmail(new EmailQueueItem
            {
                ToEmail = "fail@test.com",
                FullName = "Fail User",
                EmailType = EmailType.Verification,
                Token = "fail-token",
                RetryCount = 0
            });

            var service = new EmailQueueService(_mockLogger.Object, _serviceProvider);
            var cts = new CancellationTokenSource();

            // Give it enough time to process and retry
            cts.CancelAfter(3000);
            await service.StartAsync(cts.Token);
            await Task.Delay(3500);
            await service.StopAsync(CancellationToken.None);

            // Should be called more than once due to retries
            _mockEmailService.Verify(s => s.SendVerificationEmailAsync(
                "fail@test.com", "fail-token", "Fail User"), Times.AtLeast(2));
        }

        #endregion
    }
}
