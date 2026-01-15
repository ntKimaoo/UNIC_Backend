using BusinessLogic.DTOs;
using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, string? ipAddress);
        Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken, string? ipAddress);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<bool> LogoutAllDevicesAsync(int memberId);
        Task<MemberInfoDto?> RegisterAsync(RegisterRequestDto request);
        Task<bool> ChangePasswordAsync(int memberId, ChangePasswordRequestDto request);
        Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<bool> ResetPasswordAsync(ResetPasswordRequestDto request);
        Task<bool> VerifyEmailAsync(VerifyEmailRequestDto request);
        Task<bool> ResendVerificationEmailAsync(string email);
    }
}
