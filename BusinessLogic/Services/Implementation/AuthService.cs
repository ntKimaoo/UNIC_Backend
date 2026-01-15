using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
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
    private readonly IMemberRepository _memberRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthService(
        IMemberRepository memberRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IEmailVerificationTokenRepository emailVerificationTokenRepository,
        IJwtService jwtService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _memberRepository = memberRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _emailVerificationTokenRepository = emailVerificationTokenRepository;
        _jwtService = jwtService;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, string? ipAddress)
    {
        // Find member by email
        var member = await _memberRepository.GetByEmailAsync(request.Email);

        if (member == null || !VerifyPassword(request.Password, member.PasswordHash!))
        {
            return null;
        }

        // Check if member is active
        if (member.Status?.ToLower() != "active")
        {
            return null;
        }

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(member);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiration = DateTime.UtcNow.AddDays(
            double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7"));

        // Save refresh token to database
        var tokenHash = HashToken(refreshToken);
        var refreshTokenEntity = new RefreshToken
        {
            MemberId = member.MemberId,
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
            Member = MapToMemberInfoDto(member)
        };
    }

    public async Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken, string? ipAddress)
    {
        var tokenHash = HashToken(refreshToken);

        // Find refresh token in database
        var storedToken = await _refreshTokenRepository.GetByTokenHashWithMemberAsync(tokenHash);

        if (storedToken == null || storedToken.IsRevoked == true)
        {
            return null;
        }

        // Check if token is expired
        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        // Check if member is active
        if (storedToken.Member.Status?.ToLower() != "active")
        {
            return null;
        }

        // Revoke old refresh token
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(storedToken);

        // Generate new tokens
        var newAccessToken = _jwtService.GenerateAccessToken(storedToken.Member);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var newRefreshTokenExpiration = DateTime.UtcNow.AddDays(
            double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7"));

        // Save new refresh token
        var newTokenHash = HashToken(newRefreshToken);
        var newRefreshTokenEntity = new RefreshToken
        {
            MemberId = storedToken.MemberId,
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
            Member = MapToMemberInfoDto(storedToken.Member)
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

    public async Task<bool> LogoutAllDevicesAsync(int memberId)
    {
        return await _refreshTokenRepository.RevokeAllByMemberIdAsync(memberId);
    }

    public async Task<MemberInfoDto?> RegisterAsync(RegisterRequestDto request)
    {
        // Check if email already exists
        if (await _memberRepository.EmailExistsAsync(request.Email))
        {
            throw new InvalidOperationException("Email already exists");
        }

        // Create new member
        var member = new Member
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            JoinDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "Pending", // Email verification required
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdMember = await _memberRepository.CreateAsync(member);

        // Generate verification token
        var verificationToken = GenerateResetToken();
        var tokenHash = HashToken(verificationToken);

        var emailVerificationToken = new EmailVerificationToken
        {
            MemberId = createdMember.MemberId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _emailVerificationTokenRepository.CreateAsync(emailVerificationToken);

        // Send verification email
        await _emailService.SendVerificationEmailAsync(
            createdMember.Email,
            verificationToken,
            createdMember.FullName
        );

        return MapToMemberInfoDto(createdMember);
    }

    public async Task<bool> ChangePasswordAsync(int memberId, ChangePasswordRequestDto request)
    {
        var member = await _memberRepository.GetByIdAsync(memberId);

        if (member == null)
        {
            return false;
        }

        // Verify current password
        if (!VerifyPassword(request.CurrentPassword, member.PasswordHash!))
        {
            throw new InvalidOperationException("Current password is incorrect");
        }

        // Update password
        member.PasswordHash = HashPassword(request.NewPassword);
        member.UpdatedAt = DateTime.UtcNow;

        var result = await _memberRepository.UpdateAsync(member);

        if (result)
        {
            // Revoke all refresh tokens (force re-login on all devices)
            await _refreshTokenRepository.RevokeAllByMemberIdAsync(memberId);
        }

        return result;
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        var member = await _memberRepository.GetByEmailAsync(request.Email);

        if (member == null)
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
            MemberId = member.MemberId,
            TokenHash = tokenHash,
            ExpiresAt = resetTokenExpiry,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _passwordResetTokenRepository.CreateAsync(passwordResetToken);

        // Send reset password email
        await _emailService.SendPasswordResetEmailAsync(
            member.Email,
            resetToken,
            member.FullName
        );

        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var tokenHash = HashToken(request.ResetToken);
        var storedToken = await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHash);

        if (storedToken == null || storedToken.Member.Email != request.Email)
        {
            return false;
        }

        // Update password
        var member = storedToken.Member;
        member.PasswordHash = HashPassword(request.NewPassword);
        member.UpdatedAt = DateTime.UtcNow;

        var result = await _memberRepository.UpdateAsync(member);

        if (result)
        {
            // Mark token as used
            await _passwordResetTokenRepository.MarkAsUsedAsync(storedToken.PasswordResetTokenId);

            // Revoke all refresh tokens
            await _refreshTokenRepository.RevokeAllByMemberIdAsync(member.MemberId);
        }

        return result;
    }

    public async Task<bool> VerifyEmailAsync(VerifyEmailRequestDto request)
    {
        var tokenHash = HashToken(request.VerificationToken);
        var storedToken = await _emailVerificationTokenRepository.GetByTokenHashAsync(tokenHash);

        if (storedToken == null || storedToken.Member.Email != request.Email)
        {
            return false;
        }

        // Update member status to Active
        var member = storedToken.Member;
        member.Status = "Active";
        member.UpdatedAt = DateTime.UtcNow;

        var result = await _memberRepository.UpdateAsync(member);

        if (result)
        {
            // Mark token as used
            await _emailVerificationTokenRepository.MarkAsUsedAsync(storedToken.EmailVerificationTokenId);

            // Send welcome email
            await _emailService.SendWelcomeEmailAsync(member.Email, member.FullName);
        }

        return result;
    }

    public async Task<bool> ResendVerificationEmailAsync(string email)
    {
        var member = await _memberRepository.GetByEmailAsync(email);

        if (member == null || member.Status == "Active")
        {
            return false;
        }

        // Invalidate old tokens
        await _emailVerificationTokenRepository.InvalidateAllByMemberIdAsync(member.MemberId);

        // Generate new verification token
        var verificationToken = GenerateResetToken();
        var tokenHash = HashToken(verificationToken);

        var emailVerificationToken = new EmailVerificationToken
        {
            MemberId = member.MemberId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _emailVerificationTokenRepository.CreateAsync(emailVerificationToken);

        // Send verification email
        await _emailService.SendVerificationEmailAsync(
            member.Email,
            verificationToken,
            member.FullName
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

    private MemberInfoDto MapToMemberInfoDto(Member member)
    {
        return new MemberInfoDto
        {
            MemberId = member.MemberId,
            FullName = member.FullName,
            Email = member.Email,
            Avatar = member.Avatar,
            StudentId = member.StudentId,
            Major = member.Major,
            Status = member.Status
        };
    }
}