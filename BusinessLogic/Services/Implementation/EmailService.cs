using Microsoft.Extensions.Configuration;
using BusinessLogic.Services.Interface;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using QRCoder;

namespace BusinessLogic.Services.Implementation
{
    public class EmailService : IEmailService
    {
        private const string QrContentId = "eventqrcode";
        private readonly IConfiguration _configuration;
        private readonly IQRCodeGeneratorService _qrCodeGeneratorService;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _appBaseUrl;

        public EmailService(IConfiguration configuration, IQRCodeGeneratorService qrCodeGeneratorService)
        {
            _configuration = configuration;
            _qrCodeGeneratorService = qrCodeGeneratorService;
            _smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["Email:Username"] ?? "";
            _smtpPassword = _configuration["Email:Password"] ?? "";
            _fromEmail = _configuration["Email:FromEmail"] ?? "";
            _fromName = _configuration["Email:FromName"] ?? "Member Management System";
            _appBaseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://uniclub.io.vn";
        }

        public async Task<bool> SendVerificationEmailAsync(string toEmail, string verificationToken, string fullName)
        {
            var verificationLink = $"{_appBaseUrl}/auth/verify-email?token={Uri.EscapeDataString(verificationToken)}&email={Uri.EscapeDataString(toEmail)}";

            var subject = "Verify Your Email Address";
            var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Welcome {fullName}!</h2>
                <p>Thank you for registering. Please verify your email address by clicking the link below:</p>
                <p><a href='{verificationLink}' style='background-color: #4CAF50; color: white; padding: 14px 20px; text-decoration: none; border-radius: 4px; display: inline-block;'>Verify Email</a></p>
                <p>Or copy and paste this link into your browser:</p>
                <p>{verificationLink}</p>
                <p>This link will expire in 24 hours.</p>
                <br>
                <p>If you didn't create an account, please ignore this email.</p>
            </body>
            </html>
        ";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, string fullName)
        {
            var resetLink = $"{_appBaseUrl}/auth/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(toEmail)}";

            var subject = "Reset Your Password";
            var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Hello {fullName},</h2>
                <p>You requested to reset your password. Click the link below to proceed:</p>
                <p><a href='{resetLink}' style='background-color: #2196F3; color: white; padding: 14px 20px; text-decoration: none; border-radius: 4px; display: inline-block;'>Reset Password</a></p>
                <p>Or copy and paste this link into your browser:</p>
                <p>{resetLink}</p>
                <p>This link will expire in 1 hour.</p>
                <br>
                <p>If you didn't request a password reset, please ignore this email and your password will remain unchanged.</p>
            </body>
            </html>
        ";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string fullName)
        {
            var subject = "Welcome to Our Platform!";
            var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Welcome {fullName}!</h2>
                <p>Your email has been verified successfully.</p>
                <p>You can now enjoy all the features of our platform.</p>
                <br>
                <p>If you have any questions, feel free to contact our support team.</p>
            </body>
            </html>
        ";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendEventRegistrationSuccessAsync(string toEmail, string fullName, string eventName, DateTime? startDate, string? checkInQrToken = null, string? apiBaseUrl = null)
        {
            var dateStr = startDate.HasValue ? startDate.Value.ToString("dd/MM/yyyy HH:mm") : "TBD";
            var subject = $"Xác nhận vé sự kiện: {eventName}";
            var qrHtml = "";
            byte[]? qrPngBytes = null;
            if (!string.IsNullOrWhiteSpace(checkInQrToken))
            {
                qrPngBytes = _qrCodeGeneratorService.GetQrCodePngBytes(checkInQrToken);
                if (qrPngBytes != null && qrPngBytes.Length > 0)
                {
                    qrHtml = $@"
                <p><strong>Mã QR điểm danh của bạn (vui lòng giữ kín):</strong></p>
                <p>Khi đến sự kiện, hãy đưa mã QR này cho ban tổ chức quét để xác nhận tham dự.</p>
                <p style='margin: 16px 0;'><img src='cid:{QrContentId}' alt='QR điểm danh' style='max-width: 200px; height: auto; border: 1px solid #ddd; border-radius: 8px;' /></p>
                <br>";
                }
            }
            var verifyLinkHtml = "";
            if (!string.IsNullOrWhiteSpace(checkInQrToken) && !string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                var verifyUrl = $"{apiBaseUrl.TrimEnd('/')}/api/verify?email={Uri.EscapeDataString(toEmail)}&code={Uri.EscapeDataString(checkInQrToken)}";
                verifyLinkHtml = $@"
                <p><strong>Hoặc bấm link sau để xác nhận tham dự (không cần đăng nhập):</strong></p>
                <p><a href='{verifyUrl}' style='color: #1976d2; word-break: break-all;'>{verifyUrl}</a></p>
                <br>";
            }
            var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Chào {fullName}!</h2>
                <p>Bạn đã đăng ký tham gia thành công sự kiện: <strong>{eventName}</strong>.</p>
                <p>Thời gian bắt đầu dự kiến: <strong>{dateStr}</strong></p>
                <br>
                {qrHtml}
                {verifyLinkHtml}
                <p>Trân trọng,</p>
                <p>Ban Tổ Chức</p>
            </body>
            </html>
        ";
            return await SendEmailAsync(toEmail, subject, body, qrPngBytes != null ? QrContentId : null, qrPngBytes);
        }

        private static string? GenerateQrCodeBase64(string content)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new Base64QRCode(qrCodeData);
                return qrCode.GetGraphic(4);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> SendEventCheckInCodeAsync(string toEmail, string fullName, string eventName, string checkInCode)
        {
            var subject = $"Mã Check-in sự kiện: {eventName}";
            var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Chào {fullName}!</h2>
                <p>Sự kiện <strong>{eventName}</strong> đã chính thức bắt đầu và đang mở điểm danh!</p>
                <p>Đây là Mã Check-in bí mật của bạn:</p>
                <div style='background-color: #fce4ec; padding: 20px; text-align: center; border-radius: 8px; margin: 20px 0;'>
                    <h1 style='color: #d81b60; margin: 0; font-family: monospace; letter-spacing: 5px;'>{checkInCode}</h1>
                </div>
                <p>Vui lòng nhập mã này vào hệ thống sự kiện để được đánh dấu Có Mặt.</p>
                <p>Chúc bạn có trải nghiệm tuyệt vời!</p>
            </body>
            </html>
        ";
            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendRegistrationRejectedAsync(string toEmail, string fullName, string eventName)
        {
            var subject = $"Đăng ký sự kiện bị từ chối: {eventName}";
            var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Chào {fullName},</h2>
                <p>Rất tiếc, đăng ký của bạn cho sự kiện <strong>{eventName}</strong> đã bị từ chối bởi ban tổ chức.</p>
                <p>Nếu bạn cho rằng đây là nhầm lẫn, vui lòng liên hệ ban tổ chức để biết thêm chi tiết.</p>
                <p>Bạn có thể đăng ký lại nếu sự kiện vẫn còn mở đăng ký.</p>
                <br>
                <p>Trân trọng,</p>
                <p>Ban Tổ Chức</p>
            </body>
            </html>
        ";
            return await SendEmailAsync(toEmail, subject, body);
        }
        public async Task<bool> SendInterviewStatusChangeEmailAsync(
    string toEmail, string fullName, string interviewTitle,
    string status, DateTime scheduledAt, int durationMinutes,
    List<DateTime>? proposedTimes = null,
    string? cancelReason = null, string? confirmDeadline = null)
        {
            var vnTime = scheduledAt;
            var dateStr = vnTime.ToString("HH:mm, dddd, dd/MM/yyyy", new CultureInfo("vi-VN"));
            var endTimeStr = vnTime.AddMinutes(durationMinutes).ToString("HH:mm");
            var subject = $"[UniClub] Cập nhật lịch phỏng vấn: {interviewTitle}";

            string badgeLabel, badgeBg, badgeColor, badgeEmoji;
            string headerBg, titleColor, subColor;
            string infoBg, infoBorder;
            string contentHtml;

            // Gmail-safe info row using <table>
            string InfoRow(string label, string value) => $@"
        <tr>
            <td style='padding:8px 0;border-bottom:1px solid #f1f5f9;vertical-align:top;width:110px;font-size:13px;color:#64748b;font-family:Arial,sans-serif;'>{label}</td>
            <td style='padding:8px 0 8px 12px;border-bottom:1px solid #f1f5f9;font-size:13px;font-weight:bold;color:#1e293b;vertical-align:top;font-family:Arial,sans-serif;'>{value}</td>
        </tr>";

            // Gmail-safe alert box
            string AlertBox(string bg, string border, string color, string text) => $@"
        <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:20px;'>
            <tr><td style='background:{bg};border:1px solid {border};border-radius:8px;padding:12px 16px;font-size:13px;font-weight:bold;color:{color};font-family:Arial,sans-serif;'>
                Lưu ý: {text}
            </td></tr>
        </table>";

            // Gmail-safe CTA button using <table>
            string CtaButton(string bg, string label, string url) => $@"
        <table cellpadding='0' cellspacing='0' style='margin-top:8px;'>
            <tr><td style='background:{bg};border-radius:8px;text-align:center;'>
                <a href='{url}' style='display:inline-block;padding:12px 24px;color:#ffffff;text-decoration:none;font-size:14px;font-weight:bold;font-family:Arial,sans-serif;'>{label} &rarr;</a>
            </td></tr>
        </table>";

            switch (status)
            {
                case "Scheduled":
                    badgeLabel = "Đã lên lịch"; badgeBg = "#dbeafe"; badgeColor = "#1e40af"; badgeEmoji = "";
                    headerBg = "#eff6ff"; titleColor = "#1e3a8a"; subColor = "#3b82f6";
                    infoBg = "#eff6ff"; infoBorder = "#bfdbfe";

                    var deadlineText = !string.IsNullOrEmpty(confirmDeadline)
                        ? $"Vui lòng xác nhận trước <strong>{confirmDeadline}</strong>"
                        : "Vui lòng xác nhận lịch phỏng vấn sớm nhất có thể.";
                    var deadlineHtml = AlertBox("#fff7ed", "#fdba74", "#c2410c", deadlineText);

                    string timesHtml;
                    if (proposedTimes != null && proposedTimes.Any())
                    {
                        var slotsHtml = string.Join("", proposedTimes.Select(t => $@"
                        <tr><td style='padding:10px 14px;background:#ffffff;border:1px solid #e2e8f0;border-radius:8px;font-size:13px;font-weight:bold;color:#1e293b;font-family:Arial,sans-serif;'>
                            {t.ToString("HH:mm, dddd, dd/MM/yyyy", new CultureInfo("vi-VN"))}
                        </td></tr>
                        <tr><td style='height:6px;'></td></tr>"));

                        timesHtml = $@"
                        <p style='font-size:14px;color:#475569;margin-bottom:10px;font-weight:bold;font-family:Arial,sans-serif;'>Vui lòng xác nhận lịch phỏng vấn:</p>
                        <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:18px;'>{slotsHtml}</table>";
                    }
                    else
                    {
                        timesHtml = $@"
                        <table width='100%' cellpadding='0' cellspacing='0' style='background:{infoBg};border:1px solid {infoBorder};border-radius:10px;padding:6px 18px;margin-bottom:18px;'>
                            {InfoRow("Tiêu đề", interviewTitle)}
                            {InfoRow("Thời gian", $"{dateStr} &ndash; {endTimeStr}")}
                            {InfoRow("Thời lượng", $"{durationMinutes} phút")}
                        </table>";
                    }

                    contentHtml = $@"
                <p style='font-size:15px;color:#1e293b;margin-bottom:14px;font-family:Arial,sans-serif;'>Bạn đã được mời tham gia buổi phỏng vấn. Vui lòng xem thông tin và xác nhận sớm!</p>
                {timesHtml}
                {deadlineHtml}
                {CtaButton("#1e40af", "Xác nhận lịch phỏng vấn", _appBaseUrl + "/profile?tab=candidate")}";
                    break;

                case "Confirmed":
                    badgeLabel = "Đã xác nhận"; badgeBg = "#dcfce7"; badgeColor = "#166534"; badgeEmoji = "";
                    headerBg = "#f0fdf4"; titleColor = "#14532d"; subColor = "#16a34a";
                    infoBg = "#f0fdf4"; infoBorder = "#86efac";

                    contentHtml = $@"
                <p style='font-size:15px;color:#1e293b;margin-bottom:14px;font-family:Arial,sans-serif;'>Lịch phỏng vấn của bạn đã được <strong>xác nhận thành công</strong>. Hãy chuẩn bị thật tốt và đến đúng giờ!</p>
                <table width='100%' cellpadding='0' cellspacing='0' style='background:{infoBg};border:1px solid {infoBorder};border-radius:10px;padding:6px 18px;margin-bottom:18px;'>
                    {InfoRow("Tiêu đề", interviewTitle)}
                    {InfoRow("Thời gian", $"{dateStr} &ndash; {endTimeStr}")}
                    {InfoRow("Thời lượng", $"{durationMinutes} phút")}
                </table>
                <p style='font-size:14px;color:#555;line-height:1.65;font-family:Arial,sans-serif;'>Chúc bạn có buổi phỏng vấn thật tốt!</p>";
                    break;

                case "InProgress":
                    badgeLabel = "Đang diễn ra"; badgeBg = "#ffedd5"; badgeColor = "#9a3412"; badgeEmoji = "";
                    headerBg = "#fff7ed"; titleColor = "#7c2d12"; subColor = "#ea580c";
                    infoBg = "#fff7ed"; infoBorder = "#fdba74";

                    contentHtml = $@"
                <p style='font-size:15px;color:#1e293b;margin-bottom:14px;font-family:Arial,sans-serif;'>Buổi phỏng vấn <strong>{interviewTitle}</strong> đã chính thức bắt đầu. Nếu chưa tham gia, vào ngay nhé!</p>
                <table width='100%' cellpadding='0' cellspacing='0' style='background:{infoBg};border:1px solid {infoBorder};border-radius:10px;padding:6px 18px;margin-bottom:18px;'>
                    {InfoRow("Bắt đầu lúc", dateStr)}
                    {InfoRow("Thời lượng", $"{durationMinutes} phút")}
                </table>
                {CtaButton("#ea580c", "Tham gia phỏng vấn", _appBaseUrl)}";
                    break;

                case "Completed":
                    badgeLabel = "Đã hoàn thành"; badgeBg = "#dcfce7"; badgeColor = "#14532d"; badgeEmoji = "";
                    headerBg = "#f0fdf4"; titleColor = "#14532d"; subColor = "#16a34a";
                    infoBg = "#f0fdf4"; infoBorder = "#86efac";

                    contentHtml = $@"
                <p style='font-size:15px;color:#1e293b;margin-bottom:14px;font-family:Arial,sans-serif;'>Buổi phỏng vấn <strong>{interviewTitle}</strong> đã kết thúc. Cảm ơn bạn đã tham gia!</p>
                <table width='100%' cellpadding='0' cellspacing='0' style='background:{infoBg};border:1px solid {infoBorder};border-radius:10px;padding:6px 18px;margin-bottom:18px;'>
                    {InfoRow("Thời gian", dateStr)}
                </table>
                <p style='font-size:14px;color:#555;line-height:1.65;font-family:Arial,sans-serif;'>Kết quả sẽ được thông báo sớm nhất qua email và hệ thống. Hãy theo dõi nhé!</p>";
                    break;

                case "Cancelled":
                    badgeLabel = "Đã hủy"; badgeBg = "#ffe4e6"; badgeColor = "#9f1239"; badgeEmoji = "";
                    headerBg = "#fff1f2"; titleColor = "#881337"; subColor = "#e11d48";
                    infoBg = "#fff1f2"; infoBorder = "#fda4af";

                    var reasonRow = !string.IsNullOrEmpty(cancelReason)
                        ? InfoRow("Lý do", cancelReason)
                        : "";

                    contentHtml = $@"
                <p style='font-size:15px;color:#1e293b;margin-bottom:14px;font-family:Arial,sans-serif;'>Rất tiếc, buổi phỏng vấn <strong>{interviewTitle}</strong> đã bị hủy.</p>
                <table width='100%' cellpadding='0' cellspacing='0' style='background:{infoBg};border:1px solid {infoBorder};border-radius:10px;padding:6px 18px;margin-bottom:18px;'>
                    {InfoRow("Dự kiến lúc", dateStr)}
                    {reasonRow}
                </table>
                <p style='font-size:14px;color:#555;line-height:1.65;font-family:Arial,sans-serif;'>Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ ban tổ chức qua hệ thống.</p>";
                    break;

                case "Rescheduled":
                    badgeLabel = "Đã đổi lịch"; badgeBg = "#f3e8ff"; badgeColor = "#6b21a8"; badgeEmoji = "";
                    headerBg = "#fdf4ff"; titleColor = "#581c87"; subColor = "#9333ea";
                    infoBg = "#fdf4ff"; infoBorder = "#d8b4fe";

                    var rescheduleAlertText = !string.IsNullOrEmpty(confirmDeadline)
                        ? $"Vui lòng xác nhận trước <strong>{confirmDeadline}</strong>"
                        : "Vui lòng xác nhận lịch mới sớm nhất có thể.";
                    var rescheduleAlert = AlertBox("#fdf4ff", "#d8b4fe", "#7e22ce", rescheduleAlertText);

                    string rescheduleTimesHtml;
                    if (proposedTimes != null && proposedTimes.Any())
                    {
                        var slotsHtml = string.Join("", proposedTimes.Select(t => $@"
                        <tr><td style='padding:10px 14px;background:#ffffff;border:1px solid #f3e8ff;border-radius:8px;font-size:13px;font-weight:bold;color:#1e293b;font-family:Arial,sans-serif;'>
                            {t.ToString("HH:mm, dddd, dd/MM/yyyy", new CultureInfo("vi-VN"))}
                        </td></tr>
                        <tr><td style='height:6px;'></td></tr>"));

                        rescheduleTimesHtml = $@"
                        <p style='font-size:14px;color:#475569;margin-bottom:10px;font-weight:bold;font-family:Arial,sans-serif;'>Các khung giờ đề xuất mới:</p>
                        <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:18px;'>{slotsHtml}</table>";
                    }
                    else
                    {
                        rescheduleTimesHtml = $@"
                        <table width='100%' cellpadding='0' cellspacing='0' style='background:{infoBg};border:1px solid {infoBorder};border-radius:10px;padding:6px 18px;margin-bottom:18px;'>
                            {InfoRow("Tiêu đề", interviewTitle)}
                            {InfoRow("Lịch mới", $"{dateStr} &ndash; {endTimeStr}")}
                            {InfoRow("Thời lượng", $"{durationMinutes} phút")}
                        </table>";
                    }

                    contentHtml = $@"
                <p style='font-size:15px;color:#1e293b;margin-bottom:14px;font-family:Arial,sans-serif;'>Lịch phỏng vấn <strong>{interviewTitle}</strong> đã được thay đổi. Xem thời gian mới bên dưới!</p>
                {rescheduleTimesHtml}
                {rescheduleAlert}
                {CtaButton("#7c3aed", "Xác nhận lịch mới", _appBaseUrl)}";
                    break;

                default:
                    badgeLabel = "Cập nhật"; badgeBg = "#f1f5f9"; badgeColor = "#475569"; badgeEmoji = "";
                    headerBg = "#f8fafc"; titleColor = "#1e293b"; subColor = "#64748b";
                    infoBg = ""; infoBorder = "";
                    contentHtml = $"<p style='font-size:14px;color:#555;font-family:Arial,sans-serif;'>Trạng thái lịch phỏng vấn <strong>{interviewTitle}</strong> đã được cập nhật thành: <strong>{status}</strong>.</p>";
                    break;
            }

            var body = $@"
    <html>
    <body style='margin:0;padding:0;background:#f1f5f9;font-family:Arial,sans-serif;'>
        <table width='100%' cellpadding='0' cellspacing='0' style='padding:32px 16px;'>
            <tr><td align='center'>
                <table width='560' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:14px;overflow:hidden;border:1px solid #e2e8f0;'>

                    <!-- HEADER -->
                    <tr><td style='background:{headerBg};padding:28px 32px 24px;'>
                        <table cellpadding='0' cellspacing='0' style='margin-bottom:12px;'>
                            <tr><td style='background:{badgeBg};color:{badgeColor};padding:5px 14px;border-radius:20px;font-size:12px;font-weight:bold;font-family:Arial,sans-serif;'>
                                {badgeLabel}
                            </td></tr>
                        </table>
                        <div style='font-size:19px;font-weight:bold;color:{titleColor};line-height:1.3;margin-bottom:4px;font-family:Arial,sans-serif;'>{interviewTitle}</div>
                        <div style='font-size:12px;color:{subColor};font-family:Arial,sans-serif;'>UniClub &middot; Thông báo tự động</div>
                    </td></tr>

                    <!-- BODY -->
                    <tr><td style='padding:26px 32px;'>
                        <p style='font-size:15px;color:#1e293b;margin:0 0 14px;font-family:Arial,sans-serif;'>Chào <strong>{fullName}</strong>,</p>
                        {contentHtml}
                    </td></tr>

                    <!-- FOOTER -->
                    <tr><td style='padding:18px 32px 24px;border-top:1px solid #e2e8f0;'>
                        <table cellpadding='0' cellspacing='0'>
                            <tr>
                                <td style='width:22px;height:22px;background:#1e40af;border-radius:6px;text-align:center;vertical-align:middle;padding:0;font-size:12px;color:#ffffff;font-weight:bold;font-family:Arial,sans-serif;'>U</td>
                                <td style='padding-left:8px;font-size:13px;font-weight:bold;color:#1e293b;font-family:Arial,sans-serif;'>UniClub</td>
                            </tr>
                        </table>
                        <p style='font-size:11px;color:#94a3b8;line-height:1.6;margin:6px 0 0;font-family:Arial,sans-serif;'>
                            Email này được gửi tự động từ hệ thống UniClub. Vui lòng không trả lời email này.
                        </p>
                    </td></tr>

                </table>
            </td></tr>
        </table>
    </body>
    </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendInterviewReminderEmailAsync(
            string toEmail, string fullName, string interviewTitle,
            DateTime scheduledAt, int durationMinutes)
        {
            var vnTime = scheduledAt;
            var dateStr = vnTime.ToString("HH:mm, dddd, dd/MM/yyyy", new CultureInfo("vi-VN"));
            var endTimeStr = vnTime.AddMinutes(durationMinutes).ToString("HH:mm");
            var subject = $"[UniClub] Nhắc nhở: Buổi phỏng vấn \"{interviewTitle}\" diễn ra lúc {vnTime:HH:mm} hôm nay";

            var body = $@"
    <html>
    <body style='margin:0;padding:0;background:#f1f5f9;font-family:Arial,sans-serif;'>
        <table width='100%' cellpadding='0' cellspacing='0' style='padding:32px 16px;'>
            <tr><td align='center'>
                <table width='560' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:14px;overflow:hidden;border:1px solid #e2e8f0;'>
                    <tr><td style='background:#eff6ff;padding:28px 32px 24px;'>
                        <table cellpadding='0' cellspacing='0' style='margin-bottom:12px;'>
                            <tr><td style='background:#dbeafe;color:#1e40af;padding:5px 14px;border-radius:20px;font-size:12px;font-weight:bold;font-family:Arial,sans-serif;'>Nhắc nhở phỏng vấn</td></tr>
                        </table>
                        <div style='font-size:19px;font-weight:bold;color:#1e3a8a;line-height:1.3;margin-bottom:4px;font-family:Arial,sans-serif;'>{interviewTitle}</div>
                        <div style='font-size:12px;color:#3b82f6;font-family:Arial,sans-serif;'>UniClub &middot; Thông báo tự động</div>
                    </td></tr>
                    <tr><td style='padding:26px 32px;'>
                        <p style='font-size:15px;color:#1e293b;margin:0 0 14px;font-family:Arial,sans-serif;'>Chào <strong>{fullName}</strong>,</p>
                        <p style='font-size:15px;color:#1e293b;margin-bottom:14px;font-family:Arial,sans-serif;'>Bạn có buổi phỏng vấn diễn ra trong <strong>khoảng 2 giờ nữa</strong>. Hãy chuẩn bị sẵn sàng nhé!</p>
                        <table width='100%' cellpadding='0' cellspacing='0' style='background:#eff6ff;border:1px solid #bfdbfe;border-radius:10px;padding:12px 18px;margin-bottom:18px;'>
                            <tr><td style='padding:8px 0;font-size:13px;color:#64748b;width:110px;font-family:Arial,sans-serif;'>Tiêu đề</td><td style='padding:8px 0 8px 12px;font-size:13px;font-weight:bold;color:#1e293b;font-family:Arial,sans-serif;'>{interviewTitle}</td></tr>
                            <tr><td style='padding:8px 0;font-size:13px;color:#64748b;font-family:Arial,sans-serif;'>Thời gian</td><td style='padding:8px 0 8px 12px;font-size:13px;font-weight:bold;color:#1e293b;font-family:Arial,sans-serif;'>{dateStr} (kết thúc {endTimeStr})</td></tr>
                            <tr><td style='padding:8px 0;font-size:13px;color:#64748b;font-family:Arial,sans-serif;'>Thời lượng</td><td style='padding:8px 0 8px 12px;font-size:13px;font-weight:bold;color:#1e293b;font-family:Arial,sans-serif;'>{durationMinutes} phút</td></tr>
                        </table>
                        <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:20px;'>
                            <tr><td style='background:#fff7ed;border:1px solid #fdba74;border-radius:8px;padding:12px 16px;font-size:13px;font-weight:bold;color:#c2410c;font-family:Arial,sans-serif;'>
                                Lưu ý: Hãy kiểm tra micro, camera và kết nối mạng trước khi buổi phỏng vấn bắt đầu.
                            </td></tr>
                        </table>
                    </td></tr>
                    <tr><td style='padding:18px 32px 24px;border-top:1px solid #e2e8f0;'>
                        <p style='font-size:11px;color:#94a3b8;line-height:1.6;margin:0;font-family:Arial,sans-serif;'>Email này được gửi tự động từ hệ thống UniClub.</p>
                    </td></tr>
                </table>
            </td></tr>
        </table>
    </body>
    </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendInterviewRoomOpenedEmailAsync(
            string toEmail, string fullName, string interviewTitle,
            DateTime scheduledAt, string roomCode)
        {
            var vnTime = scheduledAt;
            var dateStr = vnTime.ToString("HH:mm, dddd, dd/MM/yyyy", new CultureInfo("vi-VN"));
            var roomUrl = $"{_appBaseUrl}/interview/room/{roomCode}";
            var subject = $"[UniClub] Phòng phỏng vấn \"{interviewTitle}\" đã mở – Hãy vào chuẩn bị!";

            var body = $@"
    <html>
    <body style='margin:0;padding:0;background:#f1f5f9;font-family:Arial,sans-serif;'>
        <table width='100%' cellpadding='0' cellspacing='0' style='padding:32px 16px;'>
            <tr><td align='center'>
                <table width='560' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:14px;overflow:hidden;border:1px solid #e2e8f0;'>
                    <tr><td style='background:#f0fdf4;padding:28px 32px 24px;'>
                        <table cellpadding='0' cellspacing='0' style='margin-bottom:12px;'>
                            <tr><td style='background:#dcfce7;color:#166534;padding:5px 14px;border-radius:20px;font-size:12px;font-weight:bold;font-family:Arial,sans-serif;'>Phòng đã mở</td></tr>
                        </table>
                        <div style='font-size:19px;font-weight:bold;color:#14532d;line-height:1.3;margin-bottom:4px;font-family:Arial,sans-serif;'>{interviewTitle}</div>
                        <div style='font-size:12px;color:#16a34a;font-family:Arial,sans-serif;'>UniClub &middot; Thông báo tự động</div>
                    </td></tr>
                    <tr><td style='padding:26px 32px;'>
                        <p style='font-size:15px;color:#1e293b;margin:0 0 14px;font-family:Arial,sans-serif;'>Chào <strong>{fullName}</strong>,</p>
                        <p style='font-size:15px;color:#1e293b;margin-bottom:14px;font-family:Arial,sans-serif;'>Phòng phỏng vấn cho buổi <strong>{interviewTitle}</strong> đã được mở. Bạn có thể vào ngay để chuẩn bị trước khi buổi phỏng vấn bắt đầu lúc <strong>{vnTime:HH:mm}</strong>.</p>
                        <table width='100%' cellpadding='0' cellspacing='0' style='background:#f0fdf4;border:1px solid #86efac;border-radius:10px;padding:12px 18px;margin-bottom:18px;'>
                            <tr><td style='padding:8px 0;font-size:13px;color:#64748b;width:110px;font-family:Arial,sans-serif;'>Thời gian</td><td style='padding:8px 0 8px 12px;font-size:13px;font-weight:bold;color:#1e293b;font-family:Arial,sans-serif;'>{dateStr}</td></tr>
                            <tr><td style='padding:8px 0;font-size:13px;color:#64748b;font-family:Arial,sans-serif;'>Mã phòng</td><td style='padding:8px 0 8px 12px;font-size:13px;font-weight:bold;color:#166534;font-family:Arial,sans-serif;'>{roomCode}</td></tr>
                        </table>
                        <table cellpadding='0' cellspacing='0' align='center' style='margin-bottom:16px;'>
                            <tr><td style='background:#16a34a;border-radius:8px;text-align:center;'>
                                <a href='{roomUrl}' style='display:inline-block;padding:12px 28px;color:#ffffff;text-decoration:none;font-size:14px;font-weight:bold;font-family:Arial,sans-serif;'>Vào phòng phỏng vấn &rarr;</a>
                            </td></tr>
                        </table>
                    </td></tr>
                    <tr><td style='padding:18px 32px 24px;border-top:1px solid #e2e8f0;'>
                        <p style='font-size:11px;color:#94a3b8;line-height:1.6;margin:0;font-family:Arial,sans-serif;'>Email này được gửi tự động từ hệ thống UniClub.</p>
                    </td></tr>
                </table>
            </td></tr>
        </table>
    </body>
    </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendInterviewFeedbackNudgeEmailAsync(
            string toEmail, string fullName, string interviewTitle, DateTime scheduledAt)
        {
            var vnTime = scheduledAt;
            var dateStr = vnTime.ToString("HH:mm, dddd, dd/MM/yyyy", new CultureInfo("vi-VN"));
            var subject = $"[UniClub] Nhắc nhở: Hãy gửi đánh giá cho buổi phỏng vấn \"{interviewTitle}\"";

            var body = $@"
    <html>
    <body style='margin:0;padding:0;background:#f1f5f9;font-family:Arial,sans-serif;'>
        <table width='100%' cellpadding='0' cellspacing='0' style='padding:32px 16px;'>
            <tr><td align='center'>
                <table width='560' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:14px;overflow:hidden;border:1px solid #e2e8f0;'>
                    <tr><td style='background:#fff7ed;padding:28px 32px 24px;'>
                        <table cellpadding='0' cellspacing='0' style='margin-bottom:12px;'>
                            <tr><td style='background:#ffedd5;color:#9a3412;padding:5px 14px;border-radius:20px;font-size:12px;font-weight:bold;font-family:Arial,sans-serif;'>Nhắc đánh giá</td></tr>
                        </table>
                        <div style='font-size:19px;font-weight:bold;color:#7c2d12;line-height:1.3;margin-bottom:4px;font-family:Arial,sans-serif;'>{interviewTitle}</div>
                        <div style='font-size:12px;color:#ea580c;font-family:Arial,sans-serif;'>UniClub &middot; Thông báo tự động</div>
                    </td></tr>
                    <tr><td style='padding:26px 32px;'>
                        <p style='font-size:15px;color:#1e293b;margin:0 0 14px;font-family:Arial,sans-serif;'>Chào <strong>{fullName}</strong>,</p>
                        <p style='font-size:15px;color:#1e293b;margin-bottom:14px;font-family:Arial,sans-serif;'>Buổi phỏng vấn <strong>{interviewTitle}</strong> (diễn ra vào {dateStr}) đã kết thúc, nhưng bạn <strong>chưa gửi đánh giá</strong>.</p>
                        <p style='font-size:15px;color:#1e293b;margin-bottom:18px;font-family:Arial,sans-serif;'>Vui lòng đăng nhập hệ thống và hoàn tất đánh giá ứng viên sớm nhất có thể!</p>
                        <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:20px;'>
                            <tr><td style='background:#fef3c7;border:1px solid #fbbf24;border-radius:8px;padding:12px 16px;font-size:13px;font-weight:bold;color:#92400e;font-family:Arial,sans-serif;'>
                                Lưu ý: Đánh giá của bạn rất quan trọng để ban quản lý có thể so sánh và đưa ra quyết định cuối cùng.
                            </td></tr>
                        </table>
                        <table cellpadding='0' cellspacing='0' align='center' style='margin-bottom:16px;'>
                            <tr><td style='background:#ea580c;border-radius:8px;text-align:center;'>
                                <a href='{_appBaseUrl}/interview/schedule' style='display:inline-block;padding:12px 28px;color:#ffffff;text-decoration:none;font-size:14px;font-weight:bold;font-family:Arial,sans-serif;'>Đánh giá ngay &rarr;</a>
                            </td></tr>
                        </table>
                    </td></tr>
                    <tr><td style='padding:18px 32px 24px;border-top:1px solid #e2e8f0;'>
                        <p style='font-size:11px;color:#94a3b8;line-height:1.6;margin:0;font-family:Arial,sans-serif;'>Email này được gửi tự động từ hệ thống UniClub.</p>
                    </td></tr>
                </table>
            </td></tr>
        </table>
    </body>
    </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendClubAcceptanceEmailAsync(string toEmail, string fullName, string campaignName)
        {
            var subject = $"[UniClub] Chúc mừng bạn đã trúng tuyển đợt: {campaignName}";

            string body = $@"
    <html>
    <body style='margin:0;padding:0;background:#f8fafc;font-family:Arial,sans-serif;'>
        <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width:540px;background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e2e8f0;margin:24px auto;'>
            <tr><td style='padding:40px 40px 32px;'>
                <h2 style='text-align:center;margin:0 0 24px;font-size:22px;font-weight:bold;color:#1e293b;font-family:Arial,sans-serif;'>Chúc mừng bạn đã trúng tuyển!</h2>

                <p style='font-size:15px;color:#475569;line-height:1.6;margin:0 0 20px;font-family:Arial,sans-serif;'>
                    Xin chào <strong style='color:#1e293b;'>{fullName}</strong>,
                </p>
                <p style='font-size:15px;color:#475569;line-height:1.6;margin:0 0 24px;font-family:Arial,sans-serif;'>
                    Chúng tôi rất vui mừng thông báo bạn đã xuất sắc vượt qua các vòng phỏng vấn và chính thức trở thành thành viên Câu lạc bộ thông qua chiến dịch tuyển dụng: <strong style='color:#4f46e5;'>{campaignName}</strong>.
                </p>

                <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:24px;'>
                    <tr><td style='background:#f8fafc;border-left:4px solid #4f46e5;padding:16px 20px;border-radius:0 8px 8px 0;font-size:14px;color:#334155;line-height:1.5;font-family:Arial,sans-serif;'>
                        Chào mừng bạn gia nhập gia đình UniClub. Các quản lý câu lạc bộ sẽ sớm liên hệ với bạn để phổ biến công việc và lịch sinh hoạt!
                    </td></tr>
                </table>

                <p style='font-size:15px;color:#475569;line-height:1.5;margin:0;font-family:Arial,sans-serif;'>
                    Trân trọng,<br><strong style='color:#1e293b;'>Đội ngũ UniClub</strong>
                </p>
            </td></tr>
            <tr><td style='background:#f1f5f9;padding:24px 40px;text-align:center;'>
                <p style='font-size:12px;color:#94a3b8;margin:0;font-family:Arial,sans-serif;'>&copy; {DateTime.UtcNow.Year} UniClub System. Email này được gửi tự động.</p>
            </td></tr>
        </table>
    </body>
    </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }


        public async Task<bool> SendApplicationSuccessEmailAsync(string toEmail, string fullName, string campaignName)
        {
            var subject = $"[UniClub] Chúc mừng! Bạn đã vượt qua vòng đơn: {campaignName}";

            string body = $@"
    <html>
    <body style='margin:0;padding:0;background:#f8fafc;font-family:Arial,sans-serif;'>
        <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width:540px;background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e2e8f0;margin:24px auto;'>
            <tr><td style='padding:40px 40px 32px;'>
                <h2 style='text-align:center;margin:0 0 24px;font-size:22px;font-weight:bold;color:#1e293b;font-family:Arial,sans-serif;'>Chúc mừng bạn!</h2>

                <p style='font-size:15px;color:#475569;line-height:1.6;margin:0 0 20px;font-family:Arial,sans-serif;'>
                    Xin chào <strong style='color:#1e293b;'>{fullName}</strong>,
                </p>
                <p style='font-size:15px;color:#475569;line-height:1.6;margin:0 0 24px;font-family:Arial,sans-serif;'>
                    Chúc mừng bạn đã xuất sắc vượt qua vòng xét duyệt đơn đăng ký của chiến dịch tuyển dụng: <strong style='color:#4f46e5;'>{campaignName}</strong>.
                </p>

                <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:24px;'>
                    <tr><td style='background:#f0f9ff;border-left:4px solid #0ea5e9;padding:16px 20px;border-radius:0 8px 8px 0;font-size:14px;color:#0c4a6e;line-height:1.5;font-family:Arial,sans-serif;'>
                        <strong>Bước tiếp theo:</strong> Ban tổ chức sẽ sớm liên hệ với bạn để sắp xếp lịch phỏng vấn. Vui lòng thường xuyên kiểm tra email và hệ thống UniClub để không bỏ lỡ thông báo mới nhất!
                    </td></tr>
                </table>

                <p style='font-size:15px;color:#475569;line-height:1.5;margin:0;font-family:Arial,sans-serif;'>
                    Hẹn gặp lại bạn sớm tại buổi phỏng vấn,<br><strong style='color:#1e293b;'>Đội ngũ UniClub</strong>
                </p>
            </td></tr>
            <tr><td style='background:#f1f5f9;padding:24px 40px;text-align:center;'>
                <p style='font-size:12px;color:#94a3b8;margin:0;font-family:Arial,sans-serif;'>&copy; {DateTime.UtcNow.Year} UniClub System. Email này được gửi tự động.</p>
            </td></tr>
        </table>
    </body>
    </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendApplicationRejectedEmailAsync(string toEmail, string fullName, string campaignName)
        {
            var subject = $"[UniClub] Thông báo kết quả vòng đơn: {campaignName}";

            string body = $@"
    <html>
    <body style='margin:0;padding:0;background:#f8fafc;font-family:Arial,sans-serif;'>
        <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width:540px;background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e2e8f0;margin:24px auto;'>
            <tr><td style='padding:40px 40px 32px;'>
                <h2 style='text-align:center;margin:0 0 24px;font-size:22px;font-weight:bold;color:#1e293b;font-family:Arial,sans-serif;'>Cảm ơn bạn đã quan tâm</h2>

                <p style='font-size:15px;color:#475569;line-height:1.6;margin:0 0 20px;font-family:Arial,sans-serif;'>
                    Xin chào <strong style='color:#1e293b;'>{fullName}</strong>,
                </p>
                <p style='font-size:15px;color:#475569;line-height:1.6;margin:0 0 24px;font-family:Arial,sans-serif;'>
                    Lời đầu tiên, chúng tôi xin cảm ơn bạn đã dành thời gian và tâm huyết để đăng ký tham gia chiến dịch tuyển dụng: <strong style='color:#1e293b;'>{campaignName}</strong>.
                </p>

                <p style='font-size:15px;color:#475569;line-height:1.6;margin:0 0 24px;font-family:Arial,sans-serif;'>
                    Dựa trên số lượng hồ sơ rất lớn và tiêu chí tuyển chọn của giai đoạn này, rất tiếc chúng tôi chưa thể mời bạn đi tiếp vào vòng phỏng vấn. Tuy nhiên, hồ sơ của bạn vẫn rất ấn tượng và chúng tôi hy vọng sẽ có cơ hội hợp tác với bạn trong các chiến dịch tương lai.
                </p>

                <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:24px;'>
                    <tr><td style='background:#f8fafc;border-left:4px solid #94a3b8;padding:16px 20px;border-radius:0 8px 8px 0;font-size:14px;color:#475569;line-height:1.5;font-family:Arial,sans-serif;'>
                        Chúc bạn luôn giữ vững đam mê và gặt hái được nhiều thành công trong con đường sắp tới!
                    </td></tr>
                </table>

                <p style='font-size:15px;color:#475569;line-height:1.5;margin:0;font-family:Arial,sans-serif;'>
                    Trân trọng,<br><strong style='color:#1e293b;'>Đội ngũ UniClub</strong>
                </p>
            </td></tr>
            <tr><td style='background:#f1f5f9;padding:24px 40px;text-align:center;'>
                <p style='font-size:12px;color:#94a3b8;margin:0;font-family:Arial,sans-serif;'>&copy; {DateTime.UtcNow.Year} UniClub System. Email này được gửi tự động.</p>
            </td></tr>
        </table>
    </body>
    </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendCheckInSuccessAsync(string toEmail, string fullName, string eventName, DateTime? checkInTime)
        {
            var timeStr = checkInTime.HasValue ? checkInTime.Value.ToString("HH:mm, dd/MM/yyyy") : "N/A";
            var subject = $"[UniClub] Điểm danh thành công: {eventName}";
            var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Chào {fullName}!</h2>
                <p>Bạn đã <strong>điểm danh thành công</strong> tại sự kiện: <strong>{eventName}</strong>.</p>
                <div style='background-color: #f0fdf4; border: 1px solid #86efac; padding: 16px; border-radius: 8px; margin: 16px 0;'>
                    <p style='margin: 0; color: #166534; font-weight: bold;'>Thời gian điểm danh: {timeStr}</p>
                </div>
                <p>Chúc bạn có một buổi trải nghiệm tuyệt vời!</p>
                <br>
                <p>Trân trọng,</p>
                <p>Ban Tổ Chức</p>
            </body>
            </html>
        ";
            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendEventRoleAssignedAsync(string toEmail, string fullName, string eventName, string roleName)
        {
            var subject = $"[UniClub] Bạn được giao vai trò trong sự kiện: {eventName}";
            var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Chào {fullName}!</h2>
                <p>Bạn đã được giao vai trò <strong>{roleName}</strong> trong sự kiện: <strong>{eventName}</strong>.</p>
                <div style='background-color: #f5f3ff; border: 1px solid #c4b5fd; padding: 16px; border-radius: 8px; margin: 16px 0;'>
                    <p style='margin: 0 0 8px; color: #5b21b6; font-weight: bold;'>Vai trò: {roleName}</p>
                    <p style='margin: 0; color: #5b21b6;'>Sự kiện: {eventName}</p>
                </div>
                <p>Vui lòng truy cập hệ thống để xem chi tiết quyền hạn và nhiệm vụ của bạn trong sự kiện.</p>
                <p><a href='{_appBaseUrl}' style='background-color: #7c3aed; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px; display: inline-block; font-weight: bold;'>Xem chi tiết →</a></p>
                <br>
                <p>Trân trọng,</p>
                <p>Ban Tổ Chức</p>
            </body>
            </html>
        ";
            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendInterviewerAssignedEmailAsync(string toEmail, string fullName, string interviewTitle, DateTime? scheduledAt)
        {
            var subject = $"[UniClub] Thông báo: Bạn được phân công phỏng vấn \"{interviewTitle}\"";
            
            var timeHtml = "";
            if (scheduledAt.HasValue)
            {
                var vnTime = scheduledAt.Value;
                var dateStr = vnTime.ToString("HH:mm, dddd, dd/MM/yyyy", new CultureInfo("vi-VN"));
                timeHtml = $"<tr><td style='padding:8px 0;font-size:13px;color:#64748b;width:110px;font-family:Arial,sans-serif;'>Thời gian dự kiến</td><td style='padding:8px 0 8px 12px;font-size:13px;font-weight:bold;color:#1e293b;font-family:Arial,sans-serif;'>{dateStr}</td></tr>";
            }

            string body = $@"
    <html>
    <body style='margin:0;padding:0;background:#f1f5f9;font-family:Arial,sans-serif;'>
        <table width='100%' cellpadding='0' cellspacing='0' style='padding:32px 16px;'>
            <tr><td align='center'>
                <table width='560' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:14px;overflow:hidden;border:1px solid #e2e8f0;'>
                    <tr><td style='background:#fdf4ff;padding:28px 32px 24px;'>
                        <table cellpadding='0' cellspacing='0' style='margin-bottom:12px;'>
                            <tr><td style='background:#f3e8ff;color:#7e22ce;padding:5px 14px;border-radius:20px;font-size:12px;font-weight:bold;font-family:Arial,sans-serif;'>Phân công mới</td></tr>
                        </table>
                        <div style='font-size:19px;font-weight:bold;color:#581c87;line-height:1.3;margin-bottom:4px;font-family:Arial,sans-serif;'>{interviewTitle}</div>
                        <div style='font-size:12px;color:#9333ea;font-family:Arial,sans-serif;'>UniClub &middot; Thông báo tự động</div>
                    </td></tr>
                    <tr><td style='padding:26px 32px;'>
                        <p style='font-size:15px;color:#1e293b;margin:0 0 14px;font-family:Arial,sans-serif;'>Chào <strong>{fullName}</strong>,</p>
                        <p style='font-size:15px;color:#1e293b;margin-bottom:14px;font-family:Arial,sans-serif;'>Bạn vừa được chỉ định làm phỏng vấn viên cho buổi phỏng vấn <strong>{interviewTitle}</strong>.</p>
                        <table width='100%' cellpadding='0' cellspacing='0' style='background:#fdf4ff;border:1px solid #d8b4fe;border-radius:10px;padding:12px 18px;margin-bottom:18px;'>
                            {timeHtml}
                            <tr><td style='padding:8px 0;font-size:13px;color:#64748b;width:110px;font-family:Arial,sans-serif;'>Vai trò</td><td style='padding:8px 0 8px 12px;font-size:13px;font-weight:bold;color:#1e293b;font-family:Arial,sans-serif;'>Phỏng vấn viên</td></tr>
                        </table>
                        <p style='font-size:14px;color:#555;line-height:1.65;font-family:Arial,sans-serif;'>Vui lòng đăng nhập hệ thống để xem chi tiết ứng viên và tài liệu đánh giá trước khi phỏng vấn bắt đầu.</p>
                    </td></tr>
                    <tr><td style='padding:18px 32px 24px;border-top:1px solid #e2e8f0;'>
                        <p style='font-size:11px;color:#94a3b8;line-height:1.6;margin:0;font-family:Arial,sans-serif;'>Email này được gửi tự động từ hệ thống UniClub.</p>
                    </td></tr>
                </table>
            </td></tr>
        </table>
    </body>
    </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body, string? inlineContentId = null, byte[]? inlineImageBytes = null)
        {
            try
            {
                using var smtpClient = new SmtpClient(_smtpServer, _smtpPort)
                {
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                if (!string.IsNullOrEmpty(inlineContentId) && inlineImageBytes != null && inlineImageBytes.Length > 0)
                {
                    var stream = new MemoryStream(inlineImageBytes);
                    try
                    {
                        var attachment = new Attachment(stream, "image/png");
                        attachment.ContentDisposition!.DispositionType = DispositionTypeNames.Inline;
                        attachment.ContentDisposition.Inline = true;
                        attachment.ContentId = inlineContentId;
                        attachment.ContentType.Name = "qrcode.png";
                        mailMessage.Attachments.Add(attachment);
                        await smtpClient.SendMailAsync(mailMessage);
                    }
                    finally
                    {
                        await stream.DisposeAsync();
                    }
                }
                else
                {
                    await smtpClient.SendMailAsync(mailMessage);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] Failed to send email: {ex.Message}");
                Console.WriteLine($"[EmailService] Stack: {ex.StackTrace}");
                return false;
            }
        }
    }
}
