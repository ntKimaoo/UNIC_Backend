using BusinessLogic.Services.Background;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
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
    public class TokenCleanupServiceTest
    {
        private readonly Mock<ILogger<TokenCleanupService>> _mockLogger;
        private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepo;
        private readonly Mock<IPasswordResetTokenRepository> _mockPasswordResetRepo;
        private readonly Mock<IEmailVerificationTokenRepository> _mockEmailVerifyRepo;
        private readonly IServiceProvider _serviceProvider;

        public TokenCleanupServiceTest()
        {
            _mockLogger = new Mock<ILogger<TokenCleanupService>>();
            _mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
            _mockPasswordResetRepo = new Mock<IPasswordResetTokenRepository>();
            _mockEmailVerifyRepo = new Mock<IEmailVerificationTokenRepository>();

            var services = new ServiceCollection();
            services.AddSingleton(_mockRefreshTokenRepo.Object);
            services.AddSingleton(_mockPasswordResetRepo.Object);
            services.AddSingleton(_mockEmailVerifyRepo.Object);
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task ExecuteAsync_StopsGracefully_WhenCancelled()
        {
            var service = new TokenCleanupService(_mockLogger.Object, _serviceProvider);
            var cts = new CancellationTokenSource();

            _mockRefreshTokenRepo.Setup(r => r.GetExpiredTokensAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(Enumerable.Empty<RefreshToken>());
            _mockPasswordResetRepo.Setup(r => r.GetExpiredTokensAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(Enumerable.Empty<PasswordResetToken>());
            _mockEmailVerifyRepo.Setup(r => r.GetExpiredTokensAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(Enumerable.Empty<EmailVerificationToken>());

            // Cancel almost immediately
            cts.CancelAfter(100);

            await service.StartAsync(cts.Token);
            await Task.Delay(200);
            await service.StopAsync(CancellationToken.None);

            // Should have attempted cleanup at least once
            _mockRefreshTokenRepo.Verify(r => r.GetExpiredTokensAsync(It.IsAny<DateTime>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_DeletesExpiredTokens()
        {
            var expiredRefreshTokens = new List<RefreshToken> { new RefreshToken() };
            var expiredPasswordTokens = new List<PasswordResetToken> { new PasswordResetToken() };
            var expiredEmailTokens = new List<EmailVerificationToken> { new EmailVerificationToken() };

            _mockRefreshTokenRepo.Setup(r => r.GetExpiredTokensAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(expiredRefreshTokens);
            _mockPasswordResetRepo.Setup(r => r.GetExpiredTokensAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(expiredPasswordTokens);
            _mockEmailVerifyRepo.Setup(r => r.GetExpiredTokensAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(expiredEmailTokens);

            var service = new TokenCleanupService(_mockLogger.Object, _serviceProvider);
            var cts = new CancellationTokenSource();

            cts.CancelAfter(200);
            await service.StartAsync(cts.Token);
            await Task.Delay(300);
            await service.StopAsync(CancellationToken.None);

            _mockRefreshTokenRepo.Verify(r => r.DeleteRangeAsync(expiredRefreshTokens), Times.AtLeastOnce);
            _mockPasswordResetRepo.Verify(r => r.DeleteRangeAsync(expiredPasswordTokens), Times.AtLeastOnce);
            _mockEmailVerifyRepo.Verify(r => r.DeleteRangeAsync(expiredEmailTokens), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_ContinuesOnError()
        {
            _mockRefreshTokenRepo.Setup(r => r.GetExpiredTokensAsync(It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("DB Error"));

            var service = new TokenCleanupService(_mockLogger.Object, _serviceProvider);
            var cts = new CancellationTokenSource();

            cts.CancelAfter(300);

            // Should not throw — exception is caught and logged
            await service.StartAsync(cts.Token);
            await Task.Delay(400);
            await service.StopAsync(CancellationToken.None);
        }
    }
}
