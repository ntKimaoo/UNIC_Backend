using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Background
{
    public class EmailQueueService : BackgroundService
    {
        private readonly ILogger<EmailQueueService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private static readonly ConcurrentQueue<EmailQueueItem> _emailQueue = new();
        private readonly SemaphoreSlim _signal = new(0);

        public EmailQueueService(
            ILogger<EmailQueueService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public static void EnqueueEmail(EmailQueueItem item)
        {
            _emailQueue.Enqueue(item);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Queue Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    while (_emailQueue.TryDequeue(out var emailItem))
                    {
                        await ProcessEmailAsync(emailItem);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing email queue.");
                    try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
                    catch (OperationCanceledException) { break; }
                }
            }

            _logger.LogInformation("Email Queue Service is stopping.");
        }

        private async Task ProcessEmailAsync(EmailQueueItem item)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            try
            {
                _logger.LogInformation("Processing email to {Email} with type {Type}", item.ToEmail, item.EmailType);

                bool result = item.EmailType switch
                {
                    EmailType.Verification => await emailService.SendVerificationEmailAsync(
                        item.ToEmail, item.Token!, item.FullName),
                    EmailType.PasswordReset => await emailService.SendPasswordResetEmailAsync(
                        item.ToEmail, item.Token!, item.FullName),
                    EmailType.Welcome => await emailService.SendWelcomeEmailAsync(
                        item.ToEmail, item.FullName),
                    EmailType.EventRegistration => await emailService.SendEventRegistrationSuccessAsync(
                        item.ToEmail, item.FullName, item.EventName!, item.StartDate, item.Token),
                    EmailType.EventCheckIn => await emailService.SendEventCheckInCodeAsync(
                        item.ToEmail, item.FullName, item.EventName!, item.CheckInCode!),
                    EmailType.InterviewStatusChange => await emailService.SendInterviewStatusChangeEmailAsync(
                        item.ToEmail, item.FullName, item.InterviewTitle!,
                        item.InterviewStatus!,
                        item.InterviewScheduledAt ?? DateTime.UtcNow, item.InterviewDurationMinutes,
                        item.ProposedTimes,
                        item.CancelReason, item.ConfirmDeadline),
                    EmailType.InterviewReminder => await emailService.SendInterviewReminderEmailAsync(
                        item.ToEmail, item.FullName, item.InterviewTitle!,
                        item.InterviewScheduledAt ?? DateTime.UtcNow, item.InterviewDurationMinutes),
                    EmailType.InterviewRoomOpened => await emailService.SendInterviewRoomOpenedEmailAsync(
                        item.ToEmail, item.FullName, item.InterviewTitle!,
                        item.InterviewScheduledAt ?? DateTime.UtcNow, item.RoomCode!),
                    EmailType.InterviewFeedbackNudge => await emailService.SendInterviewFeedbackNudgeEmailAsync(
                        item.ToEmail, item.FullName, item.InterviewTitle!,
                        item.InterviewScheduledAt ?? DateTime.UtcNow),
                    EmailType.ApplicationSuccess => await emailService.SendApplicationSuccessEmailAsync(
                        item.ToEmail, item.FullName, item.CampaignName ?? "Campaign"),
                    EmailType.ApplicationRejected => await emailService.SendApplicationRejectedEmailAsync(
                        item.ToEmail, item.FullName, item.CampaignName ?? "Campaign"),
                    EmailType.InterviewerAssigned => await emailService.SendInterviewerAssignedEmailAsync(
                        item.ToEmail, item.FullName, item.InterviewTitle!, item.InterviewScheduledAt),
                    _ => false
                };

                if (result)
                {
                    _logger.LogInformation("Email sent successfully to {Email}", item.ToEmail);
                }
                else
                {
                    _logger.LogWarning("Failed to send email to {Email}", item.ToEmail);

                    if (item.RetryCount < 3)
                    {
                        item.RetryCount++;
                        _emailQueue.Enqueue(item);
                        _logger.LogInformation("Re-queued email to {Email}, retry count: {Count}",
                            item.ToEmail, item.RetryCount);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", item.ToEmail);

                // Retry on exception
                if (item.RetryCount < 3)
                {
                    item.RetryCount++;
                    _emailQueue.Enqueue(item);
                }
            }
        }
    }
}
