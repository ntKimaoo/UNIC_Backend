using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Presentation.Controllers;
using Xunit;

namespace UNIC.Presentation.Test.Controllers
{
    public class AuthControllerTest
    {
        private readonly Mock<IAuthService> _mockService;
        private readonly AuthController _controller;

        public AuthControllerTest()
        {
            _mockService = new Mock<IAuthService>();
            _controller = new AuthController(_mockService.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Connection = { RemoteIpAddress = IPAddress.Loopback }
                }
            };
        }

        private void SetupUser(Guid userId)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Name, "Test User")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;
        }

        #region Login

        [Fact]
        public async Task Login_ReturnsOk_WhenCredentialsValid()
        {
            var request = new LoginRequestDto { Email = "user@test.com", Password = "pass" };
            _mockService.Setup(s => s.LoginAsync(request, It.IsAny<string?>()))
                .ReturnsAsync(new LoginResponseDto { AccessToken = "token" });

            var result = await _controller.Login(request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenInvalidCredentials()
        {
            var request = new LoginRequestDto { Email = "bad@test.com", Password = "wrong" };
            _mockService.Setup(s => s.LoginAsync(request, It.IsAny<string?>()))
                .ReturnsAsync((LoginResponseDto?)null);

            var result = await _controller.Login(request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        #endregion

        #region RefreshToken

        [Fact]
        public async Task RefreshToken_ReturnsOk_WhenValid()
        {
            var request = new RefreshTokenRequestDto { RefreshToken = "refresh123" };
            _mockService.Setup(s => s.RefreshTokenAsync("refresh123", It.IsAny<string?>()))
                .ReturnsAsync(new LoginResponseDto { AccessToken = "newtoken" });

            var result = await _controller.RefreshToken(request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task RefreshToken_ReturnsUnauthorized_WhenInvalid()
        {
            var request = new RefreshTokenRequestDto { RefreshToken = "badtoken" };
            _mockService.Setup(s => s.RefreshTokenAsync("badtoken", It.IsAny<string?>()))
                .ReturnsAsync((LoginResponseDto?)null);

            var result = await _controller.RefreshToken(request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        #endregion

        #region RevokeToken

        [Fact]
        public async Task RevokeToken_ReturnsOk_WhenSuccess()
        {
            var request = new RefreshTokenRequestDto { RefreshToken = "token123" };
            _mockService.Setup(s => s.RevokeTokenAsync("token123")).ReturnsAsync(true);

            var result = await _controller.RevokeToken(request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task RevokeToken_ReturnsBadRequest_WhenFailure()
        {
            var request = new RefreshTokenRequestDto { RefreshToken = "invalid" };
            _mockService.Setup(s => s.RevokeTokenAsync("invalid")).ReturnsAsync(false);

            var result = await _controller.RevokeToken(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region LogoutAllDevices

        [Fact]
        public async Task LogoutAllDevices_ReturnsOk_WhenSuccess()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId);
            _mockService.Setup(s => s.LogoutAllDevicesAsync(userId)).ReturnsAsync(true);

            var result = await _controller.LogoutAllDevices();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task LogoutAllDevices_ReturnsBadRequest_WhenFails()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId);
            _mockService.Setup(s => s.LogoutAllDevicesAsync(userId)).ReturnsAsync(false);

            var result = await _controller.LogoutAllDevices();

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task LogoutAllDevices_ReturnsUnauthorized_WhenNoUser()
        {
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            var result = await _controller.LogoutAllDevices();

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        #endregion

        #region GetCurrentUser

        [Fact]
        public void GetCurrentUser_ReturnsOk_WhenAuthenticated()
        {
            SetupUser(Guid.NewGuid());

            var result = _controller.GetCurrentUser();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public void GetCurrentUser_ReturnsUnauthorized_WhenNoUser()
        {
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            var result = _controller.GetCurrentUser();

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        #endregion

        #region Register

        [Fact]
        public async Task Register_ReturnsCreated_WhenSuccess()
        {
            var request = new RegisterRequestDto { Email = "new@test.com", Password = "pass" };
            _mockService.Setup(s => s.RegisterAsync(request))
                .ReturnsAsync(new UserInfoDto { UserId = Guid.NewGuid() });

            var result = await _controller.Register(request);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenServiceReturnsNull()
        {
            var request = new RegisterRequestDto { Email = "dup@test.com", Password = "pass" };
            _mockService.Setup(s => s.RegisterAsync(request))
                .ReturnsAsync((UserInfoDto?)null);

            var result = await _controller.Register(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenInvalidOperation()
        {
            var request = new RegisterRequestDto { Email = "dup@test.com", Password = "pass" };
            _mockService.Setup(s => s.RegisterAsync(request))
                .ThrowsAsync(new InvalidOperationException("Email already exists"));

            var result = await _controller.Register(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region ChangePassword

        [Fact]
        public async Task ChangePassword_ReturnsOk_WhenSuccess()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId);
            var request = new ChangePasswordRequestDto { CurrentPassword = "old", NewPassword = "new" };
            _mockService.Setup(s => s.ChangePasswordAsync(userId, request)).ReturnsAsync(true);

            var result = await _controller.ChangePassword(request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ChangePassword_ReturnsBadRequest_WhenFails()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId);
            var request = new ChangePasswordRequestDto { CurrentPassword = "wrong", NewPassword = "new" };
            _mockService.Setup(s => s.ChangePasswordAsync(userId, request)).ReturnsAsync(false);

            var result = await _controller.ChangePassword(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ChangePassword_ReturnsUnauthorized_WhenNoUser()
        {
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
            var request = new ChangePasswordRequestDto();

            var result = await _controller.ChangePassword(request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        #endregion

        #region ForgotPassword

        [Fact]
        public async Task ForgotPassword_ReturnsOk_Always()
        {
            var request = new ForgotPasswordRequestDto { Email = "any@test.com" };
            _mockService.Setup(s => s.ForgotPasswordAsync(request)).ReturnsAsync(true);

            var result = await _controller.ForgotPassword(request);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region ResetPassword

        [Fact]
        public async Task ResetPassword_ReturnsOk_WhenSuccess()
        {
            var request = new ResetPasswordRequestDto { ResetToken = "tok", NewPassword = "newpass", Email = "u@t.com", ConfirmNewPassword = "newpass" };
            _mockService.Setup(s => s.ResetPasswordAsync(request)).ReturnsAsync(true);

            var result = await _controller.ResetPassword(request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ResetPassword_ReturnsBadRequest_WhenFails()
        {
            var request = new ResetPasswordRequestDto { ResetToken = "bad", NewPassword = "newpass", Email = "u@t.com", ConfirmNewPassword = "newpass" };
            _mockService.Setup(s => s.ResetPasswordAsync(request)).ReturnsAsync(false);

            var result = await _controller.ResetPassword(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region VerifyEmail

        [Fact]
        public async Task VerifyEmail_ReturnsOk_WhenSuccess()
        {
            var request = new VerifyEmailRequestDto { VerificationToken = "tok", Email = "u@t.com" };
            _mockService.Setup(s => s.VerifyEmailAsync(request)).ReturnsAsync(true);

            var result = await _controller.VerifyEmail(request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task VerifyEmail_ReturnsBadRequest_WhenFails()
        {
            var request = new VerifyEmailRequestDto { VerificationToken = "bad", Email = "u@t.com" };
            _mockService.Setup(s => s.VerifyEmailAsync(request)).ReturnsAsync(false);

            var result = await _controller.VerifyEmail(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region ResendVerification

        [Fact]
        public async Task ResendVerification_ReturnsOk_WhenSuccess()
        {
            var request = new ForgotPasswordRequestDto { Email = "user@test.com" };
            _mockService.Setup(s => s.ResendVerificationEmailAsync("user@test.com")).ReturnsAsync(true);

            var result = await _controller.ResendVerification(request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ResendVerification_ReturnsBadRequest_WhenFails()
        {
            var request = new ForgotPasswordRequestDto { Email = "none@test.com" };
            _mockService.Setup(s => s.ResendVerificationEmailAsync("none@test.com")).ReturnsAsync(false);

            var result = await _controller.ResendVerification(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion
    }
}
