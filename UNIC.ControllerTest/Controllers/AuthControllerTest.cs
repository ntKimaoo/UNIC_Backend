using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Controllers;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ControllerTest.Controllers
{
    public class AuthControllerTest
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly AuthController _controller;

        public AuthControllerTest()
        {
            _mockAuthService = new Mock<IAuthService>();
            _controller = new AuthController(_mockAuthService.Object);

            // Setup default HttpContext
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        private void SetupAuthenticatedUser(Guid userId, string email = "test@test.com", string name = "Test User")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext.HttpContext.User = principal;
        }

        #region Login

        [Fact]
        public async Task Login_ReturnsOk_WhenValid()
        {
            var request = new LoginRequestDto { Email = "test@test.com", Password = "password" };
            var response = new LoginResponseDto { AccessToken = "token", RefreshToken = "refresh" };

            _mockAuthService.Setup(s => s.LoginAsync(request, It.IsAny<string>()))
                            .ReturnsAsync(response);

            var result = await _controller.Login(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenCredentialsFail()
        {
            var request = new LoginRequestDto { Email = "test@test.com", Password = "wrong" };

            _mockAuthService.Setup(s => s.LoginAsync(request, It.IsAny<string>()))
                            .ReturnsAsync((LoginResponseDto?)null);

            var result = await _controller.Login(request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        #endregion

        #region Register

        [Fact]
        public async Task Register_ReturnsCreated_WhenSuccess()
        {
            var request = new RegisterRequestDto { FullName = "User", Email = "new@test.com", Password = "pwd" };
            var userInfo = new UserInfoDto { Email = "new@test.com", FullName = "User" };

            _mockAuthService.Setup(s => s.RegisterAsync(request))
                            .ReturnsAsync(userInfo);

            var result = await _controller.Register(request);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenEmailExists()
        {
            var request = new RegisterRequestDto { FullName = "User", Email = "exists@test.com", Password = "pwd" };

            _mockAuthService.Setup(s => s.RegisterAsync(request))
                            .ThrowsAsync(new InvalidOperationException("Email already exists"));

            var result = await _controller.Register(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenResultNull()
        {
            var request = new RegisterRequestDto { FullName = "User", Email = "test@test.com", Password = "pwd" };

            _mockAuthService.Setup(s => s.RegisterAsync(request))
                            .ReturnsAsync((UserInfoDto?)null);

            var result = await _controller.Register(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region VerifyEmail

        [Fact]
        public async Task VerifyEmail_ReturnsOk_WhenValid()
        {
            var request = new VerifyEmailRequestDto { Email = "test@test.com", VerificationToken = "token" };

            _mockAuthService.Setup(s => s.VerifyEmailAsync(request))
                            .ReturnsAsync(true);

            var result = await _controller.VerifyEmail(request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task VerifyEmail_ReturnsBadRequest_WhenTokenInvalid()
        {
            var request = new VerifyEmailRequestDto { Email = "test@test.com", VerificationToken = "bad" };

            _mockAuthService.Setup(s => s.VerifyEmailAsync(request))
                            .ReturnsAsync(false);

            var result = await _controller.VerifyEmail(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region RefreshToken

        [Fact]
        public async Task RefreshToken_ReturnsOk_WhenValid()
        {
            var request = new RefreshTokenRequestDto { RefreshToken = "valid-token" };
            var response = new LoginResponseDto { AccessToken = "new-token" };

            _mockAuthService.Setup(s => s.RefreshTokenAsync("valid-token", It.IsAny<string>()))
                            .ReturnsAsync(response);

            var result = await _controller.RefreshToken(request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task RefreshToken_ReturnsUnauthorized_WhenInvalid()
        {
            var request = new RefreshTokenRequestDto { RefreshToken = "invalid-token" };

            _mockAuthService.Setup(s => s.RefreshTokenAsync("invalid-token", It.IsAny<string>()))
                            .ReturnsAsync((LoginResponseDto?)null);

            var result = await _controller.RefreshToken(request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        #endregion

        #region ForgotPassword

        [Fact]
        public async Task ForgotPassword_ReturnsOk_Always()
        {
            var request = new ForgotPasswordRequestDto { Email = "test@test.com" };

            _mockAuthService.Setup(s => s.ForgotPasswordAsync(request))
                            .ReturnsAsync(true);

            var result = await _controller.ForgotPassword(request);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region ResetPassword

        [Fact]
        public async Task ResetPassword_ReturnsOk_WhenValid()
        {
            var request = new ResetPasswordRequestDto { Email = "test@test.com", ResetToken = "token", NewPassword = "new" };

            _mockAuthService.Setup(s => s.ResetPasswordAsync(request))
                            .ReturnsAsync(true);

            var result = await _controller.ResetPassword(request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ResetPassword_ReturnsBadRequest_WhenTokenExpired()
        {
            var request = new ResetPasswordRequestDto { Email = "test@test.com", ResetToken = "bad", NewPassword = "new" };

            _mockAuthService.Setup(s => s.ResetPasswordAsync(request))
                            .ReturnsAsync(false);

            var result = await _controller.ResetPassword(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region RevokeToken (Logout)

        [Fact]
        public async Task Logout_ReturnsOk_WhenValid()
        {
            var request = new RefreshTokenRequestDto { RefreshToken = "valid-token" };

            _mockAuthService.Setup(s => s.RevokeTokenAsync("valid-token"))
                            .ReturnsAsync(true);

            var result = await _controller.RevokeToken(request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Logout_ReturnsBadRequest_WhenTokenInvalid()
        {
            var request = new RefreshTokenRequestDto { RefreshToken = "invalid" };

            _mockAuthService.Setup(s => s.RevokeTokenAsync("invalid"))
                            .ReturnsAsync(false);

            var result = await _controller.RevokeToken(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region LogoutAllDevices

        [Fact]
        public async Task LogoutAll_ReturnsOk_WhenValid()
        {
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            _mockAuthService.Setup(s => s.LogoutAllDevicesAsync(userId))
                            .ReturnsAsync(true);

            var result = await _controller.LogoutAllDevices();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task LogoutAll_ReturnsUnauthorized_WhenNotAuthenticated()
        {
            // No claims set
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            var result = await _controller.LogoutAllDevices();

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        #endregion

        #region ChangePassword

        [Fact]
        public async Task ChangePassword_ReturnsOk_WhenValid()
        {
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);
            var request = new ChangePasswordRequestDto { CurrentPassword = "old", NewPassword = "new" };

            _mockAuthService.Setup(s => s.ChangePasswordAsync(userId, request))
                            .ReturnsAsync(true);

            var result = await _controller.ChangePassword(request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ChangePassword_ReturnsBadRequest_WhenWrongPassword()
        {
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);
            var request = new ChangePasswordRequestDto { CurrentPassword = "wrong", NewPassword = "new" };

            _mockAuthService.Setup(s => s.ChangePasswordAsync(userId, request))
                            .ThrowsAsync(new InvalidOperationException("Current password is incorrect"));

            var result = await _controller.ChangePassword(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region ResendVerification

        [Fact]
        public async Task ResendVerification_ReturnsOk_WhenValid()
        {
            var request = new ForgotPasswordRequestDto { Email = "test@test.com" };

            _mockAuthService.Setup(s => s.ResendVerificationEmailAsync("test@test.com"))
                            .ReturnsAsync(true);

            var result = await _controller.ResendVerification(request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ResendVerification_ReturnsBadRequest_WhenUserMissing()
        {
            var request = new ForgotPasswordRequestDto { Email = "missing@test.com" };

            _mockAuthService.Setup(s => s.ResendVerificationEmailAsync("missing@test.com"))
                            .ReturnsAsync(false);

            var result = await _controller.ResendVerification(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region GetCurrentUser (GetProfile)

        [Fact]
        public async Task GetProfile_ReturnsOk_WhenAuthenticated()
        {
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "test@test.com", "Test User");

            var result = _controller.GetCurrentUser();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetProfile_ReturnsUnauthorized_WhenNotAuthenticated()
        {
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            var result = _controller.GetCurrentUser();

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        #endregion
    }
}