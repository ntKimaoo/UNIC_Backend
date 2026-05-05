using BusinessLogic.DTOs;
using BusinessLogic.Services.Background;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace BusinessLogic.Services.Implementation;

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

        EmailQueueService.EnqueueEmail(new EmailQueueItem
        {
            ToEmail = createdUser.Email,
            FullName = createdUser.FullName,
            Token = verificationToken,
            EmailType = EmailType.Verification
        });

        return MapToUserInfoDto(createdUser);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash!))
        {
            throw new InvalidOperationException("Current password is incorrect");
        }

        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userRepository.UpdateAsync(user);

        if (result)
        {
            await _refreshTokenRepository.RevokeAllByUserIdAsync(userId);
        }

        return result;
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null)
        {
            return true;
        }

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

        EmailQueueService.EnqueueEmail(new EmailQueueItem
        {
            ToEmail = user.Email,
            FullName = user.FullName,
            Token = resetToken,
            EmailType = EmailType.PasswordReset
        });

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

            EmailQueueService.EnqueueEmail(new EmailQueueItem
            {
                ToEmail = user.Email,
                FullName = user.FullName,
                EmailType = EmailType.Welcome
            });
        }

        return result;
    }

        public async Task<LoginResponseDto?> GoogleLoginAsync(string idToken, string? ipAddress)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        using var document = System.Text.Json.JsonDocument.Parse(content);
        var root = document.RootElement;

        if (!root.TryGetProperty("email", out var emailElement))
        {
            return null;
        }

        var email = emailElement.GetString();
        var name = root.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : "Google User";

        if (string.IsNullOrEmpty(email)) return null;

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            // Auto register
            user = new User
            {
                FullName = name ?? "Google User",
                Email = email,
                PasswordHash = HashPassword(Guid.NewGuid().ToString()), // Random password
                JoinDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            user = await _userRepository.CreateAsync(user);
        }
        else if (user.Status?.ToLower() != "active")
        {
            user.Status = "Active";
            await _userRepository.UpdateAsync(user);
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
            DeviceInfo = "Google Auth",
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
            User = MapToUserInfoDto(user)
        };
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

        EmailQueueService.EnqueueEmail(new EmailQueueItem
        {
            ToEmail = user.Email,
            FullName = user.FullName,
            Token = verificationToken,
            EmailType = EmailType.Verification
        });

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
            Status = user.Status,
            Roles = user.UserRoles?.Select(ur => ur.RoleName).ToList() ?? new List<string>(),
            ClubRoles = user.ClubMembers?.Where(cm => string.Equals(cm.Status, "Active", StringComparison.OrdinalIgnoreCase))
                .SelectMany(cm => cm.RoleAssignments?.Select(ra => new UserClubRoleDto
                {
                    ClubId = cm.ClubId,
                    RoleName = ra.ClubRole?.RoleName ?? "Unknown",
                    Level = ra.ClubRole?.Level ?? 3
                }) ?? new List<UserClubRoleDto>())
                .ToList() ?? new List<UserClubRoleDto>()
        };
    }
}

