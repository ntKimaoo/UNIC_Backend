using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Implementation;
using DataAccess.Repositories.Interface;
using DocumentFormat.OpenXml.Math;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IEmailVerificationTokenRepository emailVerificationTokenRepository,
        IJwtService jwtService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _emailVerificationTokenRepository = emailVerificationTokenRepository;
        _jwtService = jwtService;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, string? ipAddress)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash!))
        {
            return null;
        }

        if (user.Status?.ToLower() != "active")
        {
            return null;
        }

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiration = DateTime.UtcNow.AddDays(
            double.Parse(_configuration["Jwt:ExpireMinutes"] ?? "7"));

        var tokenHash = HashToken(refreshToken);
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = tokenHash,
            DeviceInfo = request.DeviceInfo,
            Ipaddress = ipAddress,
            ExpiresAt = refreshTokenExpiration,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = refreshTokenExpiration,
            User = MapToUserInfoDto(user)
        };
    }

    public async Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken, string? ipAddress)
    {
        var tokenHash = HashToken(refreshToken);

        var storedToken = await _refreshTokenRepository.GetByTokenHashWithUserAsync(tokenHash);

        if (storedToken == null || storedToken.IsRevoked == true)
        {
            return null;
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }
        if (storedToken.User.Status?.ToLower() != "active")
        {
            return null;
        }

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(storedToken);

        // Generate new tokens
        var newAccessToken = _jwtService.GenerateAccessToken(storedToken.User);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var newRefreshTokenExpiration = DateTime.UtcNow.AddDays(
            double.Parse(_configuration["Jwt:ExpireMinutes"] ?? "7"));

        // Save new refresh token
        var newTokenHash = HashToken(newRefreshToken);
        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = newTokenHash,
            DeviceInfo = storedToken.DeviceInfo,
            Ipaddress = ipAddress,
            ExpiresAt = newRefreshTokenExpiration,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity);

        return new LoginResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = newRefreshTokenExpiration,
            User = MapToUserInfoDto(storedToken.User)
        };
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);

        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (storedToken == null || storedToken.IsRevoked == true)
        {
            return false;
        }

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        return await _refreshTokenRepository.UpdateAsync(storedToken);
    }

    public async Task<bool> LogoutAllDevicesAsync(Guid userId)
    {
        return await _refreshTokenRepository.RevokeAllByUserIdAsync(userId);
    }

    public async Task<UserInfoDto?> RegisterAsync(RegisterRequestDto request)
    {
        if (await _userRepository.EmailExistsAsync(request.Email))
        {
            throw new InvalidOperationException("Email already exists");
        }
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            JoinDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.CreateAsync(user);

        // Generate verification token
        var verificationToken = GenerateResetToken();
        var tokenHash = HashToken(verificationToken);

        var emailVerificationToken = new EmailVerificationToken
        {
            UserId = createdUser.UserId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _emailVerificationTokenRepository.CreateAsync(emailVerificationToken);

        await _emailService.SendVerificationEmailAsync(
            createdUser.Email,
            verificationToken,
            createdUser.FullName
        );

        return MapToUserInfoDto(createdUser);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        // Verify current password
        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash!))
        {
            throw new InvalidOperationException("Current password is incorrect");
        }

        // Update password
        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userRepository.UpdateAsync(user);

        if (result)
        {
            // Revoke all refresh tokens (force re-login on all devices)
            await _refreshTokenRepository.RevokeAllByUserIdAsync(userId);
        }

        return result;
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null)
        {
            // Don't reveal if email exists or not (security best practice)
            return true;
        }

        // Generate reset token
        var resetToken = GenerateResetToken();
        var tokenHash = HashToken(resetToken);
        var resetTokenExpiry = DateTime.UtcNow.AddHours(1);

        var passwordResetToken = new PasswordResetToken
        {
            UserId = user.UserId,
            TokenHash = tokenHash,
            ExpiresAt = resetTokenExpiry,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _passwordResetTokenRepository.CreateAsync(passwordResetToken);

        // Send reset password email
        await _emailService.SendPasswordResetEmailAsync(
            user.Email,
            resetToken,
            user.FullName
        );

        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var tokenHash = HashToken(request.ResetToken);
        var storedToken = await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHash);

        if (storedToken == null || storedToken.User.Email != request.Email)
        {
            return false;
        }

        // Update password
        var user = storedToken.User;
        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userRepository.UpdateAsync(user);

        if (result)
        {
            // Mark token as used
            await _passwordResetTokenRepository.MarkAsUsedAsync(storedToken.PasswordResetTokenId);

            // Revoke all refresh tokens
            await _refreshTokenRepository.RevokeAllByUserIdAsync(user.UserId);
        }

        return result;
    }

    public async Task<bool> VerifyEmailAsync(VerifyEmailRequestDto request)
    {
        var tokenHash = HashToken(request.VerificationToken);
        var storedToken = await _emailVerificationTokenRepository.GetByTokenHashAsync(tokenHash);

        if (storedToken == null || storedToken.User.Email != request.Email)
        {
            return false;
        }

        // Update user status to Active
        var user = storedToken.User;    
        user.Status = "Active";
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userRepository.UpdateAsync(user);

        if (result)
        {
            await _emailVerificationTokenRepository.MarkAsUsedAsync(storedToken.EmailVerificationTokenId);

            await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);
        }

        return result;
    }

    public async Task<bool> ResendVerificationEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null || user.Status == "Active")
        {
            return false;
        }

        // Invalidate old tokens
        await _emailVerificationTokenRepository.InvalidateAllByUserIdAsync(user.UserId);

        // Generate new verification token
        var verificationToken = GenerateResetToken();
        var tokenHash = HashToken(verificationToken);

        var emailVerificationToken = new EmailVerificationToken
        {
            UserId = user.UserId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _emailVerificationTokenRepository.CreateAsync(emailVerificationToken);

        // Send verification email
        await _emailService.SendVerificationEmailAsync(
            user.Email,
            verificationToken,
            user.FullName
        );

        return true;
    }

    private bool VerifyPassword(string password, string passwordHash)
    {
        // Using BCrypt for password verification
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }

    private string GenerateResetToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private UserInfoDto MapToUserInfoDto(User user)
    {
        return new UserInfoDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Avatar = user.Avatar,
            StudentId = user.StudentId,
            Major = user.Major,
            Status = user.Status
        };
    }
}