using DataAccess.Repositories.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Background
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly ILogger<TokenCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Chạy mỗi giờ

        public TokenCleanupService(
            ILogger<TokenCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredTokensAsync();
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Graceful shutdown — exit the loop
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up expired tokens.");
                    try { await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); }
                    catch (OperationCanceledException) { break; }
                }
            }

            _logger.LogInformation("Token Cleanup Service is stopping.");
        }

        private async Task CleanupExpiredTokensAsync()
        {
            _logger.LogInformation("Starting token cleanup at {Time}", DateTime.UtcNow);

            using var scope = _serviceProvider.CreateScope();

            var refreshTokenRepo = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
            var passwordResetTokenRepo = scope.ServiceProvider.GetRequiredService<IPasswordResetTokenRepository>();
            var emailVerificationTokenRepo = scope.ServiceProvider.GetRequiredService<IEmailVerificationTokenRepository>();

            try
            {
                var expiredRefreshTokens = await refreshTokenRepo.GetExpiredTokensAsync(DateTime.UtcNow.AddDays(-30));
                if (expiredRefreshTokens.Any())
                {
                    await refreshTokenRepo.DeleteRangeAsync(expiredRefreshTokens);
                    _logger.LogInformation("Deleted {Count} expired refresh tokens", expiredRefreshTokens.Count());
                }

                var expiredPasswordTokens = await passwordResetTokenRepo.GetExpiredTokensAsync(DateTime.UtcNow.AddDays(-7));
                if (expiredPasswordTokens.Any())
                {
                    await passwordResetTokenRepo.DeleteRangeAsync(expiredPasswordTokens);
                    _logger.LogInformation("Deleted {Count} expired password reset tokens", expiredPasswordTokens.Count());
                }

                var expiredVerificationTokens = await emailVerificationTokenRepo.GetExpiredTokensAsync(DateTime.UtcNow.AddDays(-7));
                if (expiredVerificationTokens.Any())
                {
                    await emailVerificationTokenRepo.DeleteRangeAsync(expiredVerificationTokens);
                    _logger.LogInformation("Deleted {Count} expired email verification tokens", expiredVerificationTokens.Count());
                }

                _logger.LogInformation("Token cleanup completed successfully at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token cleanup");
                throw;
            }
        }
    }
}

