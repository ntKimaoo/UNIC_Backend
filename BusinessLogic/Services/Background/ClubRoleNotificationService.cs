using BusinessLogic.Services.Interface;
using DataAccess.Models;
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

        private const string RoleNotificationType = "CLUB_ROLE_WARNING";
        private const string RoleNotificationTitle = "Cảnh báo vai trò câu lạc bộ";
        private const string RoleNotificationMessage = "Hãy tạo thêm các vai trò bên trong câu lạc bộ của bạn";

        private const string InfoNotificationType = "CLUB_INFO_WARNING";
        private const string InfoNotificationTitle = "Thông tin câu lạc bộ chưa đầy đủ";
        private const string InfoNotificationMessage = "Hãy cập nhật đầy đủ thông tin câu lạc bộ (mô tả, email, số điện thoại, địa chỉ, website)";

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
                    await CheckClubRolesAsync();
                    await CheckClubInfoAsync();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ClubRole Notification Service. Retrying in 24 hours.");
                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation("ClubRole Notification Service stopped.");
        }

        private async Task CheckClubRolesAsync()
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
                await notificationService.SendNotificationAsync(
                    managerId, RoleNotificationTitle, RoleNotificationMessage, RoleNotificationType);

                _logger.LogInformation("Role warning sent to manager {UserId}", managerId);
            }
        }

        private async Task CheckClubInfoAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var clubRepo = scope.ServiceProvider.GetRequiredService<IClubRepository>();
            var clubRoleRepo = scope.ServiceProvider.GetRequiredService<IClubRoleRepository>();
            var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var activeClubs = await clubRepo.GetActiveClubsAsync();

            var incompleteClubIds = activeClubs
                .Where(c => !IsClubInfoComplete(c))
                .Select(c => c.ClubId)
                .ToList();

            if (!incompleteClubIds.Any()) return;

            var managerIds = await clubRoleRepo.GetManagerIdsForClubsAsync(incompleteClubIds);

            foreach (var managerId in managerIds)
            {
                await notificationService.SendNotificationAsync(
                    managerId, InfoNotificationTitle, InfoNotificationMessage, InfoNotificationType);

                _logger.LogInformation("Info warning sent to manager {UserId}", managerId);
            }
        }

        private static bool IsClubInfoComplete(Club club)
        {
            return !string.IsNullOrWhiteSpace(club.Description)
                && !string.IsNullOrWhiteSpace(club.Email)
                && !string.IsNullOrWhiteSpace(club.PhoneNumber)
                && !string.IsNullOrWhiteSpace(club.Address)
                && !string.IsNullOrWhiteSpace(club.WebsiteUrl);
        }
    }
}
