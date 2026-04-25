using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IEmailService
    {
        Task<bool> SendVerificationEmailAsync(string toEmail, string verificationToken, string fullName);
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, string fullName);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string fullName);
        Task<bool> SendEventRegistrationSuccessAsync(string toEmail, string fullName, string eventName, DateTime? startDate, string? checkInQrToken = null, string? apiBaseUrl = null);
        Task<bool> SendEventCheckInCodeAsync(string toEmail, string fullName, string eventName, string checkInCode);
        Task<bool> SendRegistrationRejectedAsync(string toEmail, string fullName, string eventName);
        Task<bool> SendInterviewStatusChangeEmailAsync(string toEmail, string fullName, string interviewTitle, string status, DateTime scheduledAt, int durationMinutes, List<DateTime>? proposedTimes = null, string? cancelReason = null, string? confirmDeadline = null);
        Task<bool> SendInterviewReminderEmailAsync(string toEmail, string fullName, string interviewTitle, DateTime scheduledAt, int durationMinutes);
        Task<bool> SendInterviewRoomOpenedEmailAsync(string toEmail, string fullName, string interviewTitle, DateTime scheduledAt, string roomCode);
        Task<bool> SendInterviewFeedbackNudgeEmailAsync(string toEmail, string fullName, string interviewTitle, DateTime scheduledAt);
        Task<bool> SendClubAcceptanceEmailAsync(string toEmail, string fullName, string campaignName);
        Task<bool> SendApplicationSuccessEmailAsync(string toEmail, string fullName, string campaignName);
        Task<bool> SendApplicationRejectedEmailAsync(string toEmail, string fullName, string campaignName);
    }
}
