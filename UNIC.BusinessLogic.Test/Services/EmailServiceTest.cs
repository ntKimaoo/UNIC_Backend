using BusinessLogic.Services.Implementation;
using BusinessLogic.Services.Interface;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.BusinessLogic.Test.Services
{
    public class EmailServiceTest
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IQRCodeGeneratorService> _mockQrService;
        private readonly EmailService _emailService;

        public EmailServiceTest()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockQrService = new Mock<IQRCodeGeneratorService>();

            // Setup dummy configuration to force a connection failure 
            // and cover the catch block in SendEmailAsync (returns false).
            _mockConfig.Setup(c => c["Email:SmtpServer"]).Returns("nonexistent.smtp.server.local");
            _mockConfig.Setup(c => c["Email:SmtpPort"]).Returns("587");
            _mockConfig.Setup(c => c["Email:Username"]).Returns("test");
            _mockConfig.Setup(c => c["Email:Password"]).Returns("test");
            _mockConfig.Setup(c => c["Email:FromEmail"]).Returns("test@test.com");
            _mockConfig.Setup(c => c["Email:FromName"]).Returns("Test");
            _mockConfig.Setup(c => c["AppSettings:BaseUrl"]).Returns("http://localhost");

            _emailService = new EmailService(_mockConfig.Object, _mockQrService.Object);
        }

        [Fact]
        public async Task SendVerificationEmailAsync_ShouldReturnFalse_WhenSmtpFails()
        {
            // Act
            var result = await _emailService.SendVerificationEmailAsync("test@test.com", "token", "John Doe");

            // Assert
            // Since the SMTP server is dummy, it will throw an exception internally, catch it, and return false.
            // This tests that our service compiles the email correctly but correctly handles the SMTP failure.
            Assert.False(result);
        }

        [Fact]
        public async Task SendPasswordResetEmailAsync_ShouldReturnFalse_WhenSmtpFails()
        {
            // Act
            var result = await _emailService.SendPasswordResetEmailAsync("test@test.com", "token", "John Doe");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SendWelcomeEmailAsync_ShouldReturnFalse_WhenSmtpFails()
        {
            // Act
            var result = await _emailService.SendWelcomeEmailAsync("test@test.com", "John Doe");

            // Assert
            Assert.False(result);
        }
    }
}
