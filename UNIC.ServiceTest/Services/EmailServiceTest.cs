using BusinessLogic.Services.Implementation;
using BusinessLogic.Services.Interface;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class EmailServiceTest
    {
        private readonly EmailService _service;
        private readonly Mock<IQRCodeGeneratorService> _mockQrService;

        public EmailServiceTest()
        {
            var configData = new Dictionary<string, string?>
            {
                { "Email:SmtpServer", "localhost" },
                { "Email:SmtpPort", "25" },
                { "Email:Username", "test" },
                { "Email:Password", "test" },
                { "Email:FromEmail", "test@test.com" },
                { "Email:FromName", "Test System" },
                { "AppSettings:BaseUrl", "http://localhost:5173" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            _mockQrService = new Mock<IQRCodeGeneratorService>();
            _service = new EmailService(configuration, _mockQrService.Object);
        }

        /// <summary>
        /// All email sends will fail because SMTP is pointing to localhost:25 (no real server).
        /// We test that the service handles errors gracefully and returns false.
        /// </summary>

        #region SendVerificationEmailAsync

        [Fact]
        public async Task SendVerificationEmailAsync_ReturnsFalse_WhenSmtpFails()
        {
            var result = await _service.SendVerificationEmailAsync("test@test.com", "token", "User");
            Assert.False(result);
        }

        #endregion

        #region SendPasswordResetEmailAsync

        [Fact]
        public async Task SendPasswordResetEmailAsync_ReturnsFalse_WhenSmtpFails()
        {
            var result = await _service.SendPasswordResetEmailAsync("test@test.com", "reset-token", "User");
            Assert.False(result);
        }

        #endregion

        #region SendWelcomeEmailAsync

        [Fact]
        public async Task SendWelcomeEmailAsync_ReturnsFalse_WhenSmtpFails()
        {
            var result = await _service.SendWelcomeEmailAsync("test@test.com", "User");
            Assert.False(result);
        }

        #endregion

        #region SendEventRegistrationSuccessAsync

        [Fact]
        public async Task SendEventRegistrationSuccessAsync_ReturnsFalse_WhenSmtpFails()
        {
            var result = await _service.SendEventRegistrationSuccessAsync(
                "test@test.com", "User", "Event 1", DateTime.Now, null);
            Assert.False(result);
        }

        [Fact]
        public async Task SendEventRegistrationSuccessAsync_WithQRCode_ReturnsFalse_WhenSmtpFails()
        {
            _mockQrService.Setup(q => q.GetQrCodePngBytes(It.IsAny<string>()))
                .Returns(new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header

            var result = await _service.SendEventRegistrationSuccessAsync(
                "test@test.com", "User", "Event 1", DateTime.Now, "qr-token-123");
            Assert.False(result);
        }

        [Fact]
        public async Task SendEventRegistrationSuccessAsync_WithNullStartDate_ReturnsFalse_WhenSmtpFails()
        {
            var result = await _service.SendEventRegistrationSuccessAsync(
                "test@test.com", "User", "Event 1", null, null);
            Assert.False(result);
        }

        [Fact]
        public async Task SendEventRegistrationSuccessAsync_WithVerifyLink_ReturnsFalse_WhenSmtpFails()
        {
            _mockQrService.Setup(q => q.GetQrCodePngBytes(It.IsAny<string>()))
                .Returns(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

            var result = await _service.SendEventRegistrationSuccessAsync(
                "test@test.com", "User", "Event 1", DateTime.Now, "qr-token", "http://api.test.com");
            Assert.False(result);
        }

        #endregion

        #region SendEventCheckInCodeAsync

        [Fact]
        public async Task SendEventCheckInCodeAsync_ReturnsFalse_WhenSmtpFails()
        {
            var result = await _service.SendEventCheckInCodeAsync(
                "test@test.com", "User", "Event 1", "ABC123");
            Assert.False(result);
        }

        #endregion

        #region Configuration Defaults

        [Fact]
        public void Constructor_UsesDefaults_WhenConfigMissing()
        {
            var emptyConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            var mockQr = new Mock<IQRCodeGeneratorService>();

            // Should not throw - uses default values
            var service = new EmailService(emptyConfig, mockQr.Object);
            Assert.NotNull(service);
        }

        #endregion
    }
}
