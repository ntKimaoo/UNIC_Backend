using BusinessLogic.Services.Interface;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementation
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _appBaseUrl;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["Email:Username"] ?? "";
            _smtpPassword = _configuration["Email:Password"] ?? "";
            _fromEmail = _configuration["Email:FromEmail"] ?? "";
            _fromName = _configuration["Email:FromName"] ?? "Member Management System";
            _appBaseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7000";
        }

        public async Task<bool> SendVerificationEmailAsync(string toEmail, string verificationToken, string fullName)
        {
            var verificationLink = $"{_appBaseUrl}/verify-email?token={verificationToken}&email={toEmail}";

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
            var resetLink = $"{_appBaseUrl}/reset-password?token={resetToken}&email={toEmail}";

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

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
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

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Failed to send email: {ex.Message}");
                return false;
            }
        }
    }
}
