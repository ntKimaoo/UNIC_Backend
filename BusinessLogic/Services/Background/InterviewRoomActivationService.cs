using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models.Meeting;
using DataAccess.Repositories.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Background
{

    public class InterviewRoomActivationService : BackgroundService
    {
        private readonly ILogger<InterviewRoomActivationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _preOpenDuration = TimeSpan.FromMinutes(20);
        private readonly TimeSpan _reminderBefore = TimeSpan.FromHours(2);

        public InterviewRoomActivationService(
            ILogger<InterviewRoomActivationService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("InterviewRoomActivationService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("InterviewRoomActivationService: checking at {Time}", DateTime.UtcNow);
                    using var scope = _serviceProvider.CreateScope();
                    var interviewRepo = scope.ServiceProvider.GetRequiredService<IInterviewRepository>();
                    var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

                    // ── 0. Process scheduled result announcements ─────────────
                    var interviewService = scope.ServiceProvider.GetRequiredService<IInterviewService>();
                    await ProcessScheduledPublishAsync(interviewService);

                    // ── 1. Send reminder emails (2 hours before) ──────────────
                    await SendReminderEmailsAsync(interviewRepo, userRepo);

                    // ── 1.5. Auto-remove unconfirmed secondary interviewers ──
                    await AutoRemoveUnconfirmedInterviewersAsync(interviewRepo);

                    // ── 2. Activate rooms (20 min before) + send room-opened emails ──
                    await ActivateRoomsAndNotifyAsync(interviewRepo, userRepo);

                    // ── 3. Auto-complete expired interviews ────────────────────
                    await AutoCompleteExpiredAsync(interviewRepo, userRepo);

                    // ── 4. Auto-reschedule confirmed interviews with no interviewer ──
                    await AutoRescheduleNoInterviewerAsync(interviewRepo, userRepo);

                    // ── 5. Auto-cancel unconfirmed interviews (20 mins before) ──
                    await AutoCancelUnconfirmedAsync(interviewRepo);

                    // ── 4. Send feedback nudge emails ─────────────────────────
                    await SendFeedbackNudgeEmailsAsync(interviewRepo, userRepo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "InterviewRoomActivationService: error during cycle.");
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

            _logger.LogInformation("InterviewRoomActivationService stopped.");
        }

        // ═══════════════════════════════════════════════════════════
        //  1. Reminder emails — 2 hours before
        // ═══════════════════════════════════════════════════════════
        private async Task SendReminderEmailsAsync(IInterviewRepository interviewRepo, IUserRepository userRepo)
        {
            try
            {
                var schedules = await interviewRepo.GetSchedulesNeedingReminderAsync(_reminderBefore);
                var scheduleList = schedules.ToList();
                if (!scheduleList.Any()) return;

                foreach (var schedule in scheduleList)
                {
                    // Send to candidate
                    var candidate = await userRepo.GetByIdAsync(schedule.CandidateUserId);
                    if (candidate != null)
                    {
                        EmailQueueService.EnqueueEmail(new EmailQueueItem
                        {
                            ToEmail = candidate.Email,
                            FullName = candidate.FullName,
                            EmailType = EmailType.InterviewReminder,
                            InterviewTitle = schedule.Title,
                            InterviewScheduledAt = schedule.ScheduledAt,
                            InterviewDurationMinutes = schedule.DurationMinutes
                        });
                    }

                    // Send to all assigned interviewers
                    foreach (var assignment in schedule.Assignments)
                    {
                        var interviewer = await userRepo.GetByIdAsync(assignment.InterviewerUserId);
                        if (interviewer != null)
                        {
                            EmailQueueService.EnqueueEmail(new EmailQueueItem
                            {
                                ToEmail = interviewer.Email,
                                FullName = interviewer.FullName,
                                EmailType = EmailType.InterviewReminder,
                                InterviewTitle = schedule.Title,
                                InterviewScheduledAt = schedule.ScheduledAt,
                                InterviewDurationMinutes = schedule.DurationMinutes
                            });
                        }
                    }

                    // Mark reminder as sent
                    schedule.ReminderSentAt = DateTime.UtcNow.AddHours(7);
                    await interviewRepo.UpdateScheduleAsync(schedule);
                }

                _logger.LogInformation("InterviewRoomActivationService: sent reminder emails for {Count} interview(s).", scheduleList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InterviewRoomActivationService: error sending reminder emails.");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  2. Activate rooms + notify
        // ═══════════════════════════════════════════════════════════
        private async Task ActivateRoomsAndNotifyAsync(IInterviewRepository interviewRepo, IUserRepository userRepo)
        {
            try
            {
                int activatedCount = await interviewRepo.SyncRoomStatusesAsync(_preOpenDuration);
                if (activatedCount <= 0) return;

                _logger.LogInformation("InterviewRoomActivationService: activated {Count} room(s).", activatedCount);

                // Query freshly activated schedules (InProgress with no room-opened email sent yet)
                var freshlyActivated = (await interviewRepo.GetSchedulesAsync(null, "InProgress", null, null))
                    .Where(s => s.RoomOpenedEmailSentAt == null && s.MeetingRoom != null)
                    .ToList();

                foreach (var schedule in freshlyActivated)
                {
                    var roomCode = schedule.MeetingRoom!.RoomCode;

                    // Send to candidate
                    var candidate = await userRepo.GetByIdAsync(schedule.CandidateUserId);
                    if (candidate != null)
                    {
                        EmailQueueService.EnqueueEmail(new EmailQueueItem
                        {
                            ToEmail = candidate.Email,
                            FullName = candidate.FullName,
                            EmailType = EmailType.InterviewRoomOpened,
                            InterviewTitle = schedule.Title,
                            InterviewScheduledAt = schedule.ScheduledAt,
                            RoomCode = roomCode
                        });
                    }

                    // Send to all assigned interviewers
                    foreach (var assignment in schedule.Assignments)
                    {
                        var interviewer = await userRepo.GetByIdAsync(assignment.InterviewerUserId);
                        if (interviewer != null)
                        {
                            EmailQueueService.EnqueueEmail(new EmailQueueItem
                            {
                                ToEmail = interviewer.Email,
                                FullName = interviewer.FullName,
                                EmailType = EmailType.InterviewRoomOpened,
                                InterviewTitle = schedule.Title,
                                InterviewScheduledAt = schedule.ScheduledAt,
                                RoomCode = roomCode
                            });
                        }
                    }

                    // Mark room-opened email as sent
                    schedule.RoomOpenedEmailSentAt = DateTime.UtcNow.AddHours(7);
                    await interviewRepo.UpdateScheduleAsync(schedule);
                }

                if (freshlyActivated.Any())
                    _logger.LogInformation("InterviewRoomActivationService: sent room-opened emails for {Count} interview(s).", freshlyActivated.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InterviewRoomActivationService: error activating rooms or sending notifications.");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  3. Auto-complete expired interviews
        // ═══════════════════════════════════════════════════════════
        private async Task AutoCompleteExpiredAsync(IInterviewRepository interviewRepo, IUserRepository userRepo)
        {
            try
            {
                int autoCompletedCount = await interviewRepo.AutoCompleteExpiredInterviewsAsync(20);
                if (autoCompletedCount > 0)
                {
                    _logger.LogInformation("InterviewRoomActivationService: auto-completed {Count} expired interview(s).", autoCompletedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InterviewRoomActivationService: error auto-completing expired interviews.");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  4. Feedback nudge emails
        // ═══════════════════════════════════════════════════════════
        private async Task SendFeedbackNudgeEmailsAsync(IInterviewRepository interviewRepo, IUserRepository userRepo)
        {
            try
            {
                var schedules = await interviewRepo.GetCompletedSchedulesWithPendingFeedbackAsync();
                var scheduleList = schedules.ToList();
                if (!scheduleList.Any()) return;

                foreach (var schedule in scheduleList)
                {
                    var pendingInterviewers = schedule.Assignments
                        .Where(a => a.FeedbackSubmittedAt == null)
                        .ToList();

                    foreach (var assignment in pendingInterviewers)
                    {
                        var interviewer = await userRepo.GetByIdAsync(assignment.InterviewerUserId);
                        if (interviewer != null)
                        {
                            EmailQueueService.EnqueueEmail(new EmailQueueItem
                            {
                                ToEmail = interviewer.Email,
                                FullName = interviewer.FullName,
                                EmailType = EmailType.InterviewFeedbackNudge,
                                InterviewTitle = schedule.Title,
                                InterviewScheduledAt = schedule.ScheduledAt
                            });
                        }
                    }

                    // Mark feedback nudge as sent
                    schedule.FeedbackNudgeSentAt = DateTime.UtcNow.AddHours(7);
                    await interviewRepo.UpdateScheduleAsync(schedule);
                }

                _logger.LogInformation("InterviewRoomActivationService: sent feedback nudge emails for {Count} interview(s).", scheduleList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InterviewRoomActivationService: error sending feedback nudge emails.");
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        //  0. Process scheduled result announcements
        // ═══════════════════════════════════════════════════════════
        private async Task ProcessScheduledPublishAsync(IInterviewService interviewService)
        {
            try
            {
                await interviewService.ProcessScheduledPublishAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InterviewRoomActivationService: error processing scheduled publish.");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  4. Auto-reschedule confirmed interviews with no interviewer
        // ═══════════════════════════════════════════════════════════
        private async Task AutoRescheduleNoInterviewerAsync(IInterviewRepository interviewRepo, IUserRepository userRepo)
        {
            try
            {
                var rescheduled = (await interviewRepo.AutoRescheduleNoInterviewerAsync(TimeSpan.FromHours(2))).ToList();
                if (!rescheduled.Any()) return;

                foreach (var schedule in rescheduled)
                {
                    var creator = await userRepo.GetByIdAsync(schedule.CreatedByUserId);
                    if (creator == null) continue;

                    var oldTime = schedule.ScheduledAt!.Value.AddDays(-1);
                    EmailQueueService.EnqueueEmail(new EmailQueueItem
                    {
                        ToEmail = creator.Email,
                        FullName = creator.FullName,
                        EmailType = EmailType.NoInterviewerReschedule,
                        InterviewTitle = schedule.Title,
                        InterviewScheduledAt = oldTime,
                        NewScheduledAt = schedule.ScheduledAt.Value
                    });
                }

                _logger.LogInformation("InterviewRoomActivationService: auto-rescheduled {Count} interview(s) due to no interviewer.", rescheduled.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InterviewRoomActivationService: error auto-rescheduling no-interviewer interviews.");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  5. Auto-cancel unconfirmed interviews
        // ═══════════════════════════════════════════════════════════
        private async Task AutoCancelUnconfirmedAsync(IInterviewRepository interviewRepo)
        {
            try
            {
                int cancelledCount = await interviewRepo.AutoCancelUnconfirmedInterviewsAsync(TimeSpan.FromDays(1), TimeSpan.FromHours(5));
                if (cancelledCount > 0)
                {
                    _logger.LogInformation("InterviewRoomActivationService: auto-cancelled {Count} unconfirmed interview(s).", cancelledCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InterviewRoomActivationService: error auto-cancelling unconfirmed interviews.");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  1.5. Auto-remove unconfirmed secondary interviewers
        // ═══════════════════════════════════════════════════════════
        private async Task AutoRemoveUnconfirmedInterviewersAsync(IInterviewRepository interviewRepo)
        {
            try
            {
                int removedCount = await interviewRepo.AutoRemoveUnconfirmedSecondaryInterviewersAsync(_preOpenDuration);
                if (removedCount > 0)
                {
                    _logger.LogInformation(
                        "InterviewRoomActivationService: auto-removed {Count} unconfirmed secondary interviewer(s).",
                        removedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InterviewRoomActivationService: error auto-removing unconfirmed secondary interviewers.");
            }
        }
    }
}
