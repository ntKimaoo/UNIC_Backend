using BusinessLogic.Services.Interface;
using Microsoft.Extensions.Configuration;
using System;
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
            _appBaseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5173";
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

        public async Task<bool> SendInterviewStatusChangeEmailAsync(
    string toEmail, string fullName, string interviewTitle,
    string status, DateTime scheduledAt, int durationMinutes,
    string? cancelReason = null, string? confirmDeadline = null)
        {
            var dateStr = scheduledAt.ToString("dd/MM/yyyy, HH:mm");
            var endTimeStr = scheduledAt.AddMinutes(durationMinutes).ToString("HH:mm");
            var subject = $"[UniClub] Cập nhật lịch phỏng vấn: {interviewTitle}";

            // SVG Icons
            const string iconCalendar = "<svg width='13' height='13' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><rect x='3' y='4' width='18' height='18' rx='3'/><line x1='16' y1='2' x2='16' y2='6'/><line x1='8' y1='2' x2='8' y2='6'/><line x1='3' y1='10' x2='21' y2='10'/></svg>";
            const string iconClock = "<svg width='13' height='13' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><circle cx='12' cy='12' r='10'/><polyline points='12 6 12 12 16 14'/></svg>";
            const string iconTag = "<svg width='13' height='13' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z'/><line x1='7' y1='7' x2='7.01' y2='7'/></svg>";
            const string iconFile = "<svg width='13' height='13' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z'/><polyline points='14 2 14 8 20 8'/><line x1='16' y1='13' x2='8' y2='13'/><line x1='16' y1='17' x2='8' y2='17'/></svg>";
            const string iconInfo = "<svg width='14' height='14' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'><circle cx='12' cy='12' r='10'/><line x1='12' y1='8' x2='12' y2='12'/><line x1='12' y1='16' x2='12.01' y2='16'/></svg>";
            const string iconArrow = "<svg width='14' height='14' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'><line x1='5' y1='12' x2='19' y2='12'/><polyline points='12 5 19 12 12 19'/></svg>";

            string badgeLabel, badgeBg, badgeColor, badgeIcon;
            string headerBg, titleColor, subColor;
            string infoBg, infoBorder, infoIconColor;
            string contentHtml;

            string InfoRow(string iconColor, string icon, string label, string value) => $@"
        <tr>
            <td style='padding:8px 0;border-bottom:0.5px solid rgba(0,0,0,0.06);vertical-align:top;width:110px;'>
                <span style='display:inline-flex;align-items:center;gap:6px;font-size:13px;color:#888;'>
                    <span style='color:{iconColor};'>{icon}</span>{label}
                </span>
            </td>
            <td style='padding:8px 0 8px 12px;border-bottom:0.5px solid rgba(0,0,0,0.06);font-size:13px;font-weight:500;color:#1a1a1a;vertical-align:top;'>{value}</td>
        </tr>";

            string AlertBox(string bg, string border, string color, string strongColor, string deadline) => $@"
        <div style='background:{bg};border:0.5px solid {border};border-radius:8px;padding:12px 16px;font-size:13px;font-weight:500;color:{color};margin-bottom:20px;display:flex;align-items:center;gap:8px;'>
            <span style='color:{color};flex-shrink:0;'>{iconInfo}</span>
            Vui lòng xác nhận trước <strong style='color:{strongColor};margin-left:4px;'>{deadline}</strong>
        </div>";

            string CtaButton(string bg, string label, string url) => $@"
        <a href='{url}' style='display:inline-flex;align-items:center;gap:8px;padding:12px 24px;background:{bg};color:#fff;text-decoration:none;border-radius:8px;font-size:14px;font-weight:600;font-family:Arial,sans-serif;'>
            {label} {iconArrow}
        </a>";

            switch (status)
            {
                case "Scheduled":
                    badgeLabel = "Đã lên lịch"; badgeBg = "#dbeafe"; badgeColor = "#1e40af";
                    badgeIcon = "<svg width='13' height='13' viewBox='0 0 24 24' fill='none' stroke='#1e40af' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'><rect x='3' y='4' width='18' height='18' rx='3'/><line x1='16' y1='2' x2='16' y2='6'/><line x1='8' y1='2' x2='8' y2='6'/><line x1='3' y1='10' x2='21' y2='10'/></svg>";
                    headerBg = "linear-gradient(135deg,#eff6ff,#dbeafe)"; titleColor = "#1e3a8a"; subColor = "#3b82f6";
                    infoBg = "#eff6ff"; infoBorder = "#bfdbfe"; infoIconColor = "#60a5fa";

                    var deadlineHtml = !string.IsNullOrEmpty(confirmDeadline)
                        ? AlertBox("#fff7ed", "#fdba74", "#c2410c", "#9a3412", confirmDeadline)
                        : $@"<div style='background:#fff7ed;border:0.5px solid #fdba74;border-radius:8px;padding:12px 16px;font-size:13px;font-weight:500;color:#c2410c;margin-bottom:20px;display:flex;align-items:center;gap:8px;'>
                        <span style='flex-shrink:0;color:#c2410c;'>{iconInfo}</span>
                        Vui lòng xác nhận lịch phỏng vấn sớm nhất có thể.
                    </div>";

                    contentHtml = $@"
                <p style='font-size:15px;color:#1a1a1a;margin-bottom:14px;'>Bạn đã được mời tham gia buổi phỏng vấn. Vui lòng xem thông tin và xác nhận sớm!</p>
                <table width='100%' cellpadding='0' cellspacing='0' style='background:{infoBg};border:0.5px solid {infoBorder};border-radius:10px;padding:6px 18px;margin-bottom:18px;'>
                    {InfoRow(infoIconColor, iconTag, "Tiêu đề", interviewTitle)}
                    {InfoRow(infoIconColor, iconCalendar, "Thời gian", $"{dateStr} – {endTimeStr}")}
                    {InfoRow(infoIconColor, iconClock, "Thời lượng", $"{durationMinutes} phút")}
                </table>
                {deadlineHtml}
                {CtaButton("#1e40af", "Xác nhận lịch phỏng vấn", _appBaseUrl)}";
                    break;

                case "Confirmed":
                    badgeLabel = "Đã xác nhận"; badgeBg = "#dcfce7"; badgeColor = "#166534";
                    badgeIcon = "<svg width='13' height='13' viewBox='0 0 24 24' fill='none' stroke='#16a34a' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'><polyline points='20 6 9 17 4 12'/></svg>";
                    headerBg = "linear-gradient(135deg,#f0fdf4,#dcfce7)"; titleColor = "#14532d"; subColor = "#16a34a";
                    infoBg = "#f0fdf4"; infoBorder = "#86efac"; infoIconColor = "#4ade80";

                    contentHtml = $@"
                <p style='font-size:15px;color:#1a1a1a;margin-bottom:14px;'>Lịch phỏng vấn của bạn đã được <strong>xác nhận thành công</strong>. Hãy chuẩn bị thật tốt và đến đúng giờ!</p>
                <table width='100%' cellpadding='0' cellspacing='0' style='background:{infoBg};border:0.5px solid {infoBorder};border-radius:10px;padding:6px 18px;margin-bottom:18px;'>
                    {InfoRow(infoIconColor, iconTag, "Tiêu đề", interviewTitle)}
                    {InfoRow(infoIconColor, iconCalendar, "Thời gian", $"{dateStr} – {endTimeStr}")}
                    {InfoRow(infoIconColor, iconClock, "Thời lượng", $"{durationMinutes} phút")}
                </table>
                <p style='font-size:14px;color:#555;line-height:1.65;'>Chúc bạn có buổi phỏng vấn thật tốt!</p>";
                    break;

                case "InProgress":
                    badgeLabel = "Đang diễn ra"; badgeBg = "#ffedd5"; badgeColor = "#9a3412";
                    badgeIcon = "<svg width='13' height='13' viewBox='0 0 24 24' fill='#ea580c' stroke='none'><circle cx='12' cy='12' r='10'/></svg>";
                    headerBg = "linear-gradient(135deg,#fff7ed,#ffedd5)"; titleColor = "#7c2d12"; subColor = "#ea580c";
                    infoBg = "#fff7ed"; infoBorder = "#fdba74"; infoIconColor = "#fb923c";

                    contentHtml = $@"
                <p style='font-size:15px;color:#1a1a1a;margin-bottom:14px;'>Buổi phỏng vấn <strong>{interviewTitle}</strong> đã chính thức bắt đầu. Nếu chưa tham gia, vào ngay nhé!</p>
                <table width='100%' cellpadding='0' cellspacing='0' style='background:{infoBg};border:0.5px solid {infoBorder};border-radius:10px;padding:6px 18px;margin-bottom:18px;'>
                    {InfoRow(infoIconColor, iconCalendar, "Bắt đầu lúc", dateStr)}
                    {InfoRow(infoIconColor, iconClock, "Thời lượng", $"{durationMinutes} phút")}
                </table>
                {CtaButton("#ea580c", "Tham gia phỏng vấn", _appBaseUrl)}";
                    break;

                case "Completed":
                    badgeLabel = "Đã hoàn thành"; badgeBg = "#dcfce7"; badgeColor = "#14532d";
                    badgeIcon = "<svg width='13' height='13' viewBox='0 0 24 24' fill='none' stroke='#16a34a' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'><path d='M22 11.08V12a10 10 0 1 1-5.93-9.14'/><polyline points='22 4 12 14.01 9 11.01'/></svg>";
                    headerBg = "linear-gradient(135deg,#f0fdf4,#dcfce7)"; titleColor = "#14532d"; subColor = "#16a34a";
                    infoBg = "#f0fdf4"; infoBorder = "#86efac"; infoIconColor = "#4ade80";

                    contentHtml = $@"
                <p style='font-size:15px;color:#1a1a1a;margin-bottom:14px;'>Buổi phỏng vấn <strong>{interviewTitle}</strong> đã kết thúc. Cảm ơn bạn đã tham gia!</p>
                <table width='100%' cellpadding='0' cellspacing='0' style='background:{infoBg};border:0.5px solid {infoBorder};border-radius:10px;padding:6px 18px;margin-bottom:18px;'>
                    {InfoRow(infoIconColor, iconCalendar, "Thời gian", dateStr)}
                </table>
                <p style='font-size:14px;color:#555;line-height:1.65;'>Kết quả sẽ được thông báo sớm nhất qua email và hệ thống. Hãy theo dõi nhé!</p>";
                    break;

                case "Cancelled":
                    badgeLabel = "Đã hủy"; badgeBg = "#ffe4e6"; badgeColor = "#9f1239";
                    badgeIcon = "<svg width='13' height='13' viewBox='0 0 24 24' fill='none' stroke='#e11d48' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'><line x1='18' y1='6' x2='6' y2='18'/><line x1='6' y1='6' x2='18' y2='18'/></svg>";
                    headerBg = "linear-gradient(135deg,#fff1f2,#ffe4e6)"; titleColor = "#881337"; subColor = "#e11d48";
                    infoBg = "#fff1f2"; infoBorder = "#fda4af"; infoIconColor = "#f87171";

                    var reasonRow = !string.IsNullOrEmpty(cancelReason)
                        ? InfoRow(infoIconColor, iconFile, "Lý do", cancelReason)
                        : "";

                    contentHtml = $@"
                <p style='font-size:15px;color:#1a1a1a;margin-bottom:14px;'>Rất tiếc, buổi phỏng vấn <strong>{interviewTitle}</strong> đã bị hủy.</p>
                <table width='100%' cellpadding='0' cellspacing='0' style='background:{infoBg};border:0.5px solid {infoBorder};border-radius:10px;padding:6px 18px;margin-bottom:18px;'>
                    {InfoRow(infoIconColor, iconCalendar, "Dự kiến lúc", dateStr)}
                    {reasonRow}
                </table>
                <p style='font-size:14px;color:#555;line-height:1.65;'>Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ ban tổ chức qua hệ thống.</p>";
                    break;

                case "Rescheduled":
                    badgeLabel = "Đã đổi lịch"; badgeBg = "#f3e8ff"; badgeColor = "#6b21a8";
                    badgeIcon = "<svg width='13' height='13' viewBox='0 0 24 24' fill='none' stroke='#9333ea' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'><polyline points='23 4 23 10 17 10'/><polyline points='1 20 1 14 7 14'/><path d='M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15'/></svg>";
                    headerBg = "linear-gradient(135deg,#fdf4ff,#f3e8ff)"; titleColor = "#581c87"; subColor = "#9333ea";
                    infoBg = "#fdf4ff"; infoBorder = "#d8b4fe"; infoIconColor = "#c084fc";

                    var rescheduleAlert = !string.IsNullOrEmpty(confirmDeadline)
                        ? AlertBox("#fdf4ff", "#d8b4fe", "#7e22ce", "#581c87", confirmDeadline)
                        : $@"<div style='background:#fdf4ff;border:0.5px solid #d8b4fe;border-radius:8px;padding:12px 16px;font-size:13px;font-weight:500;color:#7e22ce;margin-bottom:20px;display:flex;align-items:center;gap:8px;'>
                        <span style='flex-shrink:0;color:#7e22ce;'>{iconInfo}</span>
                        Vui lòng xác nhận lịch mới sớm nhất có thể.
                    </div>";

                    contentHtml = $@"
                <p style='font-size:15px;color:#1a1a1a;margin-bottom:14px;'>Lịch phỏng vấn <strong>{interviewTitle}</strong> đã được thay đổi. Xem thời gian mới bên dưới!</p>
                <table width='100%' cellpadding='0' cellspacing='0' style='background:{infoBg};border:0.5px solid {infoBorder};border-radius:10px;padding:6px 18px;margin-bottom:18px;'>
                    {InfoRow(infoIconColor, iconTag, "Tiêu đề", interviewTitle)}
                    {InfoRow(infoIconColor, iconCalendar, "Lịch mới", $"{dateStr} – {endTimeStr}")}
                    {InfoRow(infoIconColor, iconClock, "Thời lượng", $"{durationMinutes} phút")}
                </table>
                {rescheduleAlert}
                {CtaButton("#7c3aed", "Xác nhận lịch mới", _appBaseUrl)}";
                    break;

                default:
                    badgeLabel = "Cập nhật"; badgeBg = "#f1f5f9"; badgeColor = "#475569";
                    badgeIcon = "<svg width='13' height='13' viewBox='0 0 24 24' fill='none' stroke='#64748b' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'><circle cx='12' cy='12' r='10'/><line x1='12' y1='8' x2='12' y2='12'/><line x1='12' y1='16' x2='12.01' y2='16'/></svg>";
                    headerBg = "linear-gradient(135deg,#f8fafc,#f1f5f9)"; titleColor = "#1e293b"; subColor = "#64748b";
                    infoBg = ""; infoBorder = ""; infoIconColor = "";
                    contentHtml = $"<p style='font-size:14px;color:#555;'>Trạng thái lịch phỏng vấn <strong>{interviewTitle}</strong> đã được cập nhật thành: <strong>{status}</strong>.</p>";
                    break;
            }

            const string brandIcon = "<svg width='13' height='13' viewBox='0 0 24 24' fill='none' stroke='#fff' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'><path d='M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2'/><circle cx='9' cy='7' r='4'/><path d='M23 21v-2a4 4 0 0 0-3-3.87'/><path d='M16 3.13a4 4 0 0 1 0 7.75'/></svg>";

            var body = $@"
    <html>
    <body style='margin:0;padding:0;background:#f1f5f9;font-family:Arial,sans-serif;'>
        <table width='100%' cellpadding='0' cellspacing='0' style='padding:32px 16px;'>
            <tr><td align='center'>
                <table width='560' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:14px;overflow:hidden;border:0.5px solid #e2e8f0;'>

                    <!-- HEADER -->
                    <tr><td style='background:{headerBg};padding:28px 32px 24px;'>
                        <div style='display:inline-flex;align-items:center;gap:7px;background:{badgeBg};color:{badgeColor};padding:5px 12px 5px 8px;border-radius:20px;font-size:12px;font-weight:600;margin-bottom:12px;'>
                            {badgeIcon} {badgeLabel}
                        </div>
                        <div style='font-size:19px;font-weight:600;color:{titleColor};line-height:1.3;margin-bottom:4px;'>{interviewTitle}</div>
                        <div style='font-size:12px;color:{subColor};'>UniClub · Thông báo tự động</div>
                    </td></tr>

                    <!-- BODY -->
                    <tr><td style='padding:26px 32px;'>
                        <p style='font-size:15px;color:#1a1a1a;margin:0 0 14px;'>Chào <strong>{fullName}</strong>,</p>
                        {contentHtml}
                    </td></tr>

                    <!-- FOOTER -->
                    <tr><td style='padding:18px 32px 24px;border-top:0.5px solid #e2e8f0;'>
                        <table cellpadding='0' cellspacing='0'>
                            <tr>
                                <td style='width:22px;height:22px;background:#1e40af;border-radius:6px;text-align:center;vertical-align:middle;padding:0;'>
                                    {brandIcon}
                                </td>
                                <td style='padding-left:8px;font-size:13px;font-weight:600;color:#1a1a1a;'>UniClub</td>
                            </tr>
                        </table>
                        <p style='font-size:11px;color:#94a3b8;line-height:1.6;margin:6px 0 0;'>
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

        public async Task<bool> SendClubAcceptanceEmailAsync(string toEmail, string fullName, string campaignName)
        {
            var subject = $"[UniClub] Chúc mừng bạn đã trúng tuyển đợt: {campaignName}";

            // Mẫu email tương tự các chuẩn khác của hệ thống
            string body = $@"
    <!DOCTYPE html>
    <html lang='vi'>
    <head>
        <meta charset='UTF-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <style>
            @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
            body {{ margin: 0; padding: 0; background-color: #f8fafc; font-family: 'Inter', system-ui, -apple-system, sans-serif; }}
        </style>
    </head>
    <body style='margin:0;padding:24px 16px;background-color:#f8fafc;font-family:""Inter"",system-ui,-apple-system,sans-serif;'>
        <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width:540px;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 10px 25px -5px rgba(0,0,0,0.05);margin:0 auto;'>
            <tr><td style='padding:40px 40px 32px;'>
                <div style='text-align:center;margin-bottom:28px;'>
                    <table border='0' cellpadding='0' cellspacing='0' style='margin:0 auto;'><tr>
                        <td style='height:48px;width:48px;background:linear-gradient(135deg,#e0e7ff 0%,#c7d2fe 100%);border-radius:12px;text-align:center;'>
                            <svg width='28' height='28' viewBox='0 0 24 24' fill='none' stroke='#4f46e5' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='vertical-align:middle;margin-top:10px;'><path d='M22 11.08V12a10 10 0 1 1-5.93-9.14'></path><polyline points='22 4 12 14.01 9 11.01'></polyline></svg>
                        </td>
                    </tr></table>
                    <h2 style='margin:20px 0 0;font-size:22px;font-weight:700;color:#1e293b;letter-spacing:-0.5px;'>
                        Chúc mừng bạn đã trúng tuyển!
                    </h2>
                </div>

                <p style='font-size:15px;color:#475569;line-height:1.6;margin:0 0 20px;'>
                    Xin chào <span style='font-weight:600;color:#1e293b;'>{fullName}</span>,
                </p>
                <p style='font-size:15px;color:#475569;line-height:1.6;margin:0 0 24px;'>
                    Chúng tôi rất vui mừng thông báo bạn đã xuất sắc vượt qua các vòng phỏng vấn và chính thức trở thành thành viên Câu lạc bộ thông qua chiến dịch tuyển dụng: <b style='color:#4f46e5;'>{campaignName}</b>.
                </p>

                <div style='background-color:#f8fafc;border-left:4px solid #4f46e5;padding:16px 20px;border-radius:0 8px 8px 0;margin-bottom:24px;'>
                    <p style='font-size:14px;color:#334155;line-height:1.5;margin:0;'>
                        Chào mừng bạn gia nhập gia đình UniClub. Các quản lý câu lạc bộ sẽ sớm liên hệ với bạn để phổ biến công việc và lịch sinh hoạt!
                    </p>
                </div>

                <p style='font-size:15px;color:#475569;line-height:1.5;margin:0;'>
                    Trân trọng,<br>
                    <span style='font-weight:600;color:#1e293b;'>Đội ngũ UniClub</span>
                </p>
            </td></tr>
            
            <tr><td style='background:#f1f5f9;padding:24px 40px;text-align:center;'>
                <p style='font-size:12px;color:#94a3b8;margin:0;'>
                    © {DateTime.UtcNow.Year} UniClub System. Email này được gửi tự động.
                </p>
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
