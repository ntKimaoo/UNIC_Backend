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
