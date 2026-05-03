using DataAccess.Repositories.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Background
{
    /// <summary>
    /// Background service that automatically closes expired recruitment campaigns every hour.
    /// Rule: Status = "OPEN" + EndDate passed → Status = "CLOSED"
    /// </summary>
    public class RecruitmentCampaignStatusSyncService : BackgroundService
    {
        private readonly ILogger<RecruitmentCampaignStatusSyncService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromHours(1);

        public RecruitmentCampaignStatusSyncService(
            ILogger<RecruitmentCampaignStatusSyncService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RecruitmentCampaignStatusSyncService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IRecruitmentCampaignRepository>();

                    int updated = await repo.BulkCloseExpiredAsync();
                    if (updated > 0)
                    {
                        _logger.LogInformation("RecruitmentCampaignStatusSync: closed {Count} expired campaign(s).", updated);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RecruitmentCampaignStatusSync: error during sync.");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("RecruitmentCampaignStatusSyncService stopped.");
        }
    }
}
