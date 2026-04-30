using BusinessLogic.Services.Interface;
using DataAccess.Repositories.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataAccess.Enums;

namespace BusinessLogic.Services.Background
{
    public class EventReminderService : BackgroundService
    {
        private readonly ILogger<EventReminderService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30); // Check every 30 minutes
        
        // In-memory track of events we've already sent reminders for, to avoid duplicates without DB changes
        private static readonly HashSet<int> _remindedEventIds = new HashSet<int>();
        // Track 30-min online reminders separately
        private static readonly HashSet<int> _reminded30MinEventIds = new HashSet<int>();

        public EventReminderService(
            ILogger<EventReminderService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event Reminder Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRemindersAsync();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing event reminders.");
                    try { await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); }
                    catch (OperationCanceledException) { break; }
                }
            }

            _logger.LogInformation("Event Reminder Service is stopping.");
        }

        private async Task ProcessRemindersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            DateTime now = DateTime.UtcNow;
            // Look for events starting within the next 12.5 hours
            DateTime windowEnd = now.AddHours(12.5);

            try
            {
                var upcomingEvents = await unitOfWork.Events.GetUpcomingEventsAsync();
                
                // ===== 12h general reminder =====
                var eventsToRemind = upcomingEvents.Where(e => 
                    e.StartDate.HasValue && 
                    e.StartDate.Value <= windowEnd && 
                    e.StartDate.Value > now &&
                    e.Status != "CANCELED" && 
                    !_remindedEventIds.Contains(e.EventId)
                ).ToList();

                foreach (var ev in eventsToRemind)
                {
                    _logger.LogInformation($"Sending reminders for event: {ev.EventName}");
                    
                    var attendances = await unitOfWork.Attendances.GetAttendeesByEventAsync(ev.EventId);
                    var registeredUsers = attendances.Where(a => a.AttendanceStatus == nameof(AttendanceStatus.REGISTERED)).ToList();

                    foreach (var att in registeredUsers)
                    {
                        if (att.User != null && !string.IsNullOrEmpty(att.User.Email))
                        {
                            _ = Task.Run(() => emailService.SendEventRegistrationSuccessAsync(
                                att.User.Email, 
                                att.User.FullName, 
                                "Nhắc nhở: " + ev.EventName + " sắp diễn ra", 
                                ev.StartDate, 
                                "Xin chào, sự kiện sẽ bắt đầu trong khoảng 12 tiếng nữa. Địa điểm/Link: " + ev.Location));
                        }
                    }

                    _remindedEventIds.Add(ev.EventId);
                }

                // ===== 30-min reminder for ONLINE events =====
                DateTime window30Min = now.AddMinutes(35); // 35 phút để có buffer
                var onlineEventsToRemind = upcomingEvents.Where(e =>
                    e.StartDate.HasValue &&
                    e.IsOnline &&
                    e.StartDate.Value <= window30Min &&
                    e.StartDate.Value > now &&
                    e.Status != "CANCELED" &&
                    !_reminded30MinEventIds.Contains(e.EventId)
                ).ToList();

                foreach (var ev in onlineEventsToRemind)
                {
                    _logger.LogInformation($"Sending 30-min online reminder for event: {ev.EventName}");

                    var attendances = await unitOfWork.Attendances.GetAttendeesByEventAsync(ev.EventId);
                    var registeredUsers = attendances.Where(a => a.AttendanceStatus == nameof(AttendanceStatus.REGISTERED)).ToList();

                    var meetInfo = !string.IsNullOrEmpty(ev.MeetLink)
                        ? $"Link tham gia: {ev.MeetLink}"
                        : "Link sẽ được cung cấp khi sự kiện bắt đầu.";

                    foreach (var att in registeredUsers)
                    {
                        if (att.User != null && !string.IsNullOrEmpty(att.User.Email))
                        {
                            _ = Task.Run(() => emailService.SendEventRegistrationSuccessAsync(
                                att.User.Email,
                                att.User.FullName,
                                "⚡ " + ev.EventName + " sẽ bắt đầu sau 30 phút!",
                                ev.StartDate,
                                meetInfo));
                        }
                    }

                    _reminded30MinEventIds.Add(ev.EventId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during event reminder processing");
            }
        }
    }
}
