using API.Services;
using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.BusinessLogic.Test.Services
{
    public class AuthServiceTest
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepo;
        private readonly Mock<IPasswordResetTokenRepository> _mockPasswordResetRepo;
        private readonly Mock<IEmailVerificationTokenRepository> _mockEmailVerifRepo;
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfiguration> _mockConfiguration;

        private readonly AuthService _authService;

        public AuthServiceTest()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
            _mockPasswordResetRepo = new Mock<IPasswordResetTokenRepository>();
            _mockEmailVerifRepo = new Mock<IEmailVerificationTokenRepository>();
            _mockJwtService = new Mock<IJwtService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockConfiguration = new Mock<IConfiguration>();

            _mockConfiguration.Setup(x => x["Jwt:ExpireMinutes"]).Returns("7");

            _authService = new AuthService(
                _mockUserRepository.Object,
                _mockRefreshTokenRepo.Object,
                _mockPasswordResetRepo.Object,
                _mockEmailVerifRepo.Object,
                _mockJwtService.Object,
                _mockEmailService.Object,
                _mockConfiguration.Object
            );
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnNull_WhenUserNotFound()
        {
            // Arrange
            var loginRequest = new LoginRequestDto { Email = "test@test.com", Password = "password123" };

            _mockUserRepository.Setup(repo => repo.GetByEmailAsync(loginRequest.Email))
                               .ReturnsAsync((User?)null);

            // Act
            var result = await _authService.LoginAsync(loginRequest, "127.0.0.1");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnNull_WhenUserInactive()
        {
            // Arrange
            var loginRequest = new LoginRequestDto { Email = "test@test.com", Password = "password123" };
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = loginRequest.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(loginRequest.Password),
                Status = "Pending" // Not active
            };

            _mockUserRepository.Setup(repo => repo.GetByEmailAsync(loginRequest.Email))
                               .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginRequest, "127.0.0.1");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnLoginResponse_WhenCredentialsAreValid()
        {
            // Arrange
            var loginRequest = new LoginRequestDto { Email = "test@test.com", Password = "password123", DeviceInfo = "Test Device" };
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = loginRequest.Email,
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(loginRequest.Password),
                Status = "Active",
                UserRoles = new System.Collections.Generic.List<UserRole>()
            };

            _mockUserRepository.Setup(repo => repo.GetByEmailAsync(loginRequest.Email))
                               .ReturnsAsync(user);

            _mockJwtService.Setup(jwt => jwt.GenerateAccessToken(user))
                           .Returns("mock-access-token");
                           
            _mockJwtService.Setup(jwt => jwt.GenerateRefreshToken())
                           .Returns("mock-refresh-token");

            // Act
            var result = await _authService.LoginAsync(loginRequest, "127.0.0.1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("mock-access-token", result.AccessToken);
            Assert.Equal("mock-refresh-token", result.RefreshToken);
            Assert.Equal(user.Email, result.User.Email);
            
            _mockRefreshTokenRepo.Verify(repo => repo.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnNull_WhenTokenNotFoundOrRevoked()
        {
            // Arrange
            var refreshToken = "old-refresh-token";
            _mockRefreshTokenRepo.Setup(repo => repo.GetByTokenHashWithUserAsync(It.IsAny<string>()))
                                 .ReturnsAsync((RefreshToken?)null);

            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken, "127.0.0.1");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnNull_WhenTokenExpired()
        {
            // Arrange
            var refreshToken = "old-refresh-token";
            var storedToken = new RefreshToken
            {
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired
            };

            _mockRefreshTokenRepo.Setup(repo => repo.GetByTokenHashWithUserAsync(It.IsAny<string>()))
                                 .ReturnsAsync(storedToken);

            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken, "127.0.0.1");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnNull_WhenUserInactive()
        {
            // Arrange
            var refreshToken = "old-refresh-token";
            var user = new User { Status = "Pending" }; // Not active
            var storedToken = new RefreshToken
            {
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddDays(1), // Valid
                User = user
            };

            _mockRefreshTokenRepo.Setup(repo => repo.GetByTokenHashWithUserAsync(It.IsAny<string>()))
                                 .ReturnsAsync(storedToken);

            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken, "127.0.0.1");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnNewTokens_WhenValid()
        {
            // Arrange
            var refreshToken = "old-refresh-token";
            var user = new User 
            { 
                UserId = Guid.NewGuid(), 
                Email = "test@test.com", 
                Status = "Active",
                UserRoles = new System.Collections.Generic.List<UserRole>() 
            };
            var storedToken = new RefreshToken
            {
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                User = user,
                UserId = user.UserId,
                DeviceInfo = "Test Device"
            };

            _mockRefreshTokenRepo.Setup(repo => repo.GetByTokenHashWithUserAsync(It.IsAny<string>()))
                                 .ReturnsAsync(storedToken);
            _mockRefreshTokenRepo.Setup(repo => repo.UpdateAsync(It.IsAny<RefreshToken>()))
                                 .ReturnsAsync(true);

            _mockJwtService.Setup(jwt => jwt.GenerateAccessToken(user))
                           .Returns("new-access-token");
            _mockJwtService.Setup(jwt => jwt.GenerateRefreshToken())
                           .Returns("new-refresh-token");

            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken, "127.0.0.1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new-access-token", result.AccessToken);
            Assert.Equal("new-refresh-token", result.RefreshToken);
            
            _mockRefreshTokenRepo.Verify(repo => repo.UpdateAsync(It.Is<RefreshToken>(t => t.IsRevoked == true)), Times.Once);
            _mockRefreshTokenRepo.Verify(repo => repo.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
        }

        [Fact]
        public async Task RevokeTokenAsync_ShouldReturnFalse_WhenTokenNotFoundOrRevoked()
        {
            // Arrange
            _mockRefreshTokenRepo.Setup(repo => repo.GetByTokenHashAsync(It.IsAny<string>()))
                                 .ReturnsAsync((RefreshToken?)null);

            // Act
            var result = await _authService.RevokeTokenAsync("some-token");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RevokeTokenAsync_ShouldReturnTrue_WhenValid()
        {
            // Arrange
            var storedToken = new RefreshToken { IsRevoked = false };
            _mockRefreshTokenRepo.Setup(repo => repo.GetByTokenHashAsync(It.IsAny<string>()))
                                 .ReturnsAsync(storedToken);
            _mockRefreshTokenRepo.Setup(repo => repo.UpdateAsync(storedToken))
                                 .ReturnsAsync(true);

            // Act
            var result = await _authService.RevokeTokenAsync("some-token");

            // Assert
            Assert.True(result);
            Assert.True(storedToken.IsRevoked);
        }

        [Fact]
        public async Task LogoutAllDevicesAsync_ShouldCallRepoToRevokeAll()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockRefreshTokenRepo.Setup(repo => repo.RevokeAllByUserIdAsync(userId))
                                 .ReturnsAsync(true);

            // Act
            var result = await _authService.LogoutAllDevicesAsync(userId);

            // Assert
            Assert.True(result);
            _mockRefreshTokenRepo.Verify(repo => repo.RevokeAllByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowException_WhenEmailExists()
        {
            // Arrange
            var request = new RegisterRequestDto { Email = "existing@test.com" };
            _mockUserRepository.Setup(repo => repo.EmailExistsAsync(request.Email))
                               .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(request));
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnUserInfo_WhenSuccessful()
        {
            // Arrange
            var request = new RegisterRequestDto 
            { 
                FullName = "New User", 
                Email = "new@test.com", 
                Password = "pwd" 
            };
            var createdUser = new User
            {
                UserId = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                Status = "Pending",
                UserRoles = new System.Collections.Generic.List<UserRole>()
            };

            _mockUserRepository.Setup(repo => repo.EmailExistsAsync(request.Email))
                               .ReturnsAsync(false);
            _mockUserRepository.Setup(repo => repo.CreateAsync(It.IsAny<User>()))
                               .ReturnsAsync(createdUser);

            // Act
            var result = await _authService.RegisterAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createdUser.Email, result.Email);
            _mockEmailVerifRepo.Verify(repo => repo.CreateAsync(It.IsAny<EmailVerificationToken>()), Times.Once);
            // EmailQueueService is static, checking if method ran without error is enough for simple unit test
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldReturnFalse_WhenUserNotFound()
        {
            // Arrange
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>()))
                               .ReturnsAsync((User?)null);

            // Act
            var result = await _authService.ChangePasswordAsync(Guid.NewGuid(), new ChangePasswordRequestDto());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrowException_WhenCurrentPasswordIncorrect()
        {
            // Arrange
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("old-pwd") };
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>()))
                               .ReturnsAsync(user);
            
            var request = new ChangePasswordRequestDto { CurrentPassword = "wrong-pwd", NewPassword = "new-pwd" };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.ChangePasswordAsync(Guid.NewGuid(), request));
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldReturnTrueAndRevokeTokens_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { UserId = userId, PasswordHash = BCrypt.Net.BCrypt.HashPassword("old-pwd") };
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
                               .ReturnsAsync(user);
            _mockUserRepository.Setup(repo => repo.UpdateAsync(user))
                               .ReturnsAsync(true);
            
            var request = new ChangePasswordRequestDto { CurrentPassword = "old-pwd", NewPassword = "new-pwd" };

            // Act
            var result = await _authService.ChangePasswordAsync(userId, request);

            // Assert
            Assert.True(result);
            _mockUserRepository.Verify(repo => repo.UpdateAsync(user), Times.Once);
            _mockRefreshTokenRepo.Verify(repo => repo.RevokeAllByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_ShouldReturnTrue_WhenUserNotFound()
        {
            // Arrange
            _mockUserRepository.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>()))
                               .ReturnsAsync((User?)null);

            // Act
            var result = await _authService.ForgotPasswordAsync(new ForgotPasswordRequestDto { Email = "not-found@test.com" });

            // Assert
            Assert.True(result); // Prevent enumeration
        }

        [Fact]
        public async Task ForgotPasswordAsync_ShouldReturnTrueAndCreateToken_WhenSuccessful()
        {
            // Arrange
            var user = new User { UserId = Guid.NewGuid(), Email = "found@test.com" };
            _mockUserRepository.Setup(repo => repo.GetByEmailAsync(user.Email))
                               .ReturnsAsync(user);

            // Act
            var result = await _authService.ForgotPasswordAsync(new ForgotPasswordRequestDto { Email = user.Email });

            // Assert
            Assert.True(result);
            _mockPasswordResetRepo.Verify(repo => repo.CreateAsync(It.IsAny<PasswordResetToken>()), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldReturnFalse_WhenTokenNotFoundOrEmailMismatch()
        {
            // Arrange
            _mockPasswordResetRepo.Setup(repo => repo.GetByTokenHashAsync(It.IsAny<string>()))
                                  .ReturnsAsync((PasswordResetToken?)null);

            // Act
            var result = await _authService.ResetPasswordAsync(new ResetPasswordRequestDto { Email = "test@test.com", ResetToken = "token" });

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldReturnTrue_WhenSuccessful()
        {
            // Arrange
            var user = new User { UserId = Guid.NewGuid(), Email = "test@test.com" };
            var storedToken = new PasswordResetToken { PasswordResetTokenId = 1, User = user };
            var request = new ResetPasswordRequestDto { Email = "test@test.com", ResetToken = "token", NewPassword = "new-pwd" };

            _mockPasswordResetRepo.Setup(repo => repo.GetByTokenHashAsync(It.IsAny<string>()))
                                  .ReturnsAsync(storedToken);
            _mockUserRepository.Setup(repo => repo.UpdateAsync(user))
                               .ReturnsAsync(true);

            // Act
            var result = await _authService.ResetPasswordAsync(request);

            // Assert
            Assert.True(result);
            _mockPasswordResetRepo.Verify(repo => repo.MarkAsUsedAsync(storedToken.PasswordResetTokenId), Times.Once);
            _mockRefreshTokenRepo.Verify(repo => repo.RevokeAllByUserIdAsync(user.UserId), Times.Once);
        }

        [Fact]
        public async Task VerifyEmailAsync_ShouldReturnFalse_WhenTokenNotFoundOrEmailMismatch()
        {
            // Arrange
            _mockEmailVerifRepo.Setup(repo => repo.GetByTokenHashAsync(It.IsAny<string>()))
                               .ReturnsAsync((EmailVerificationToken?)null);

            // Act
            var result = await _authService.VerifyEmailAsync(new VerifyEmailRequestDto { Email = "test@test.com", VerificationToken = "token" });

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task VerifyEmailAsync_ShouldReturnTrue_WhenSuccessful()
        {
            // Arrange
            var user = new User { UserId = Guid.NewGuid(), Email = "test@test.com", Status = "Pending" };
            var storedToken = new EmailVerificationToken { EmailVerificationTokenId = 1, User = user };
            var request = new VerifyEmailRequestDto { Email = "test@test.com", VerificationToken = "token" };

            _mockEmailVerifRepo.Setup(repo => repo.GetByTokenHashAsync(It.IsAny<string>()))
                               .ReturnsAsync(storedToken);
            _mockUserRepository.Setup(repo => repo.UpdateAsync(user))
                               .ReturnsAsync(true);

            // Act
            var result = await _authService.VerifyEmailAsync(request);

            // Assert
            Assert.True(result);
            Assert.Equal("Active", user.Status);
            _mockEmailVerifRepo.Verify(repo => repo.MarkAsUsedAsync(storedToken.EmailVerificationTokenId), Times.Once);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_ShouldReturnFalse_WhenUserNotFound()
        {
            // Arrange
            _mockUserRepository.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>()))
                               .ReturnsAsync((User?)null);

            // Act
            var result = await _authService.ResendVerificationEmailAsync("not-found@test.com");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_ShouldReturnFalse_WhenUserAlreadyActive()
        {
            // Arrange
            var user = new User { Status = "Active" };
            _mockUserRepository.Setup(repo => repo.GetByEmailAsync("active@test.com"))
                               .ReturnsAsync(user);

            // Act
            var result = await _authService.ResendVerificationEmailAsync("active@test.com");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_ShouldReturnTrue_WhenSuccessful()
        {
            // Arrange
            var user = new User { UserId = Guid.NewGuid(), Email = "pending@test.com", Status = "Pending" };
            _mockUserRepository.Setup(repo => repo.GetByEmailAsync(user.Email))
                               .ReturnsAsync(user);

            // Act
            var result = await _authService.ResendVerificationEmailAsync(user.Email);

            // Assert
            Assert.True(result);
            _mockEmailVerifRepo.Verify(repo => repo.InvalidateAllByUserIdAsync(user.UserId), Times.Once);
            _mockEmailVerifRepo.Verify(repo => repo.CreateAsync(It.IsAny<EmailVerificationToken>()), Times.Once);
        }
    }
}
