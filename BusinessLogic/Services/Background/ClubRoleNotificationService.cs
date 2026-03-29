using BusinessLogic.Services.Interface;
using DataAccess.Repositories.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Background
{
    public class ClubRoleNotificationService : BackgroundService
    {
        private readonly ILogger<ClubRoleNotificationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

        private const int MinRoleThreshold = 2;
        private const string NotificationType = "CLUB_ROLE_WARNING";
        private const string NotificationTitle = "Cảnh báo vai trò câu lạc bộ";
        private const string NotificationMessage = "Hãy tạo thêm các vai trò bên trong câu lạc bộ của bạn";

        public ClubRoleNotificationService(
            ILogger<ClubRoleNotificationService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ClubRole Notification Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndNotifyAsync();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ClubRole Notification Service. Retrying in 5 minutes.");
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation("ClubRole Notification Service stopped.");
        }

        private async Task CheckAndNotifyAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var clubRoleRepo = scope.ServiceProvider.GetRequiredService<IClubRoleRepository>();
            var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var clubIds = (await clubRoleRepo.GetClubIdsWithFewRolesAsync(MinRoleThreshold)).ToList();
            if (!clubIds.Any()) return;

            var managerIds = await clubRoleRepo.GetManagerIdsForClubsAsync(clubIds);

            foreach (var managerId in managerIds)
            {
                var alreadyNotified = await notificationRepo.HasRecentNotificationAsync(
                    managerId, NotificationType, TimeSpan.FromHours(24));
                if (alreadyNotified) continue;

                await notificationService.SendNotificationAsync(
                    managerId, NotificationTitle, NotificationMessage, NotificationType);

                _logger.LogInformation("Notification sent to manager {UserId}", managerId);
            }
        }
    }
}
