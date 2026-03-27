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
    /// Proactive background service that syncs stale event statuses every 1 minute.
    /// Rules:
    ///   1. REGISTRATION_OPEN + RegistrationEndDate passed → REGISTRATION_CLOSED
    ///   2. Non-terminal status + EndDate passed            → ENDED
    /// </summary>
    public class EventStatusSyncService : BackgroundService
    {
        private readonly ILogger<EventStatusSyncService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

        public EventStatusSyncService(
            ILogger<EventStatusSyncService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EventStatusSyncService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var eventRepo = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Events;

                    int updated = await eventRepo.BulkSyncStatusAsync();
                    if (updated > 0)
                    {
                        _logger.LogInformation("EventStatusSync: updated {Count} event(s).", updated);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EventStatusSync: error during sync.");
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

            _logger.LogInformation("EventStatusSyncService stopped.");
        }
    }
}
