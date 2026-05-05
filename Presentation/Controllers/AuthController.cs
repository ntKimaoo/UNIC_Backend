using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Access token and refresh token</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.LoginAsync(request, ipAddress);

            if (result == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password, or account is not active"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Login successful",
                data = result
            });
        }

        /// <summary>
        /// Login with Google ID Token
        /// </summary>
        /// <param name="request">Google Login credentials</param>
        /// <returns>Access token and refresh token</returns>
        [HttpPost("google-login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto request)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(request.IdToken))
            {
                return BadRequest(new
                {
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.GoogleLoginAsync(request.IdToken, ipAddress);

            if (result == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid Google token or account could not be created."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Login successful",
                data = result
            });
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        /// <param name="request">Refresh token</param>
        /// <returns>New access token and refresh token</returns>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);

            if (result == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid or expired refresh token"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Token refreshed successfully",
                data = result
            });
        }

        /// <summary>
        /// Revoke refresh token (logout from current device)
        /// </summary>
        /// <param name="request">Refresh token to revoke</param>
        /// <returns>Success status</returns>
        [HttpPost("revoke")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var result = await _authService.RevokeTokenAsync(request.RefreshToken);

            if (!result)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Token revocation failed. Token may be invalid or already revoked."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Logged out successfully"
            });
        }

        /// <summary>
        /// Logout from all devices
        /// </summary>
        /// <returns>Success status</returns>
        [HttpPost("logout-all")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LogoutAllDevices()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var result = await _authService.LogoutAllDevicesAsync(userId);

            if (!result)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to logout from all devices"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Logged out from all devices successfully"
            });
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        /// <returns>Current user info</returns>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("sub")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var studentId = User.FindFirst("StudentId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var roles = User.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToList() ?? new List<string>();
            var clubRolesClaim = User.FindFirst("club_roles")?.Value;
            var clubRoles = new List<BusinessLogic.DTOs.UserClubRoleDto>();

            if (!string.IsNullOrEmpty(clubRolesClaim))
            {
                try
                {
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    clubRoles = System.Text.Json.JsonSerializer.Deserialize<List<BusinessLogic.DTOs.UserClubRoleDto>>(clubRolesClaim, options) ?? new List<BusinessLogic.DTOs.UserClubRoleDto>();
                }
                catch
                {
                    // Ignore parse error, return empty
                }
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    userId = userIdClaim,
                    email = email,
                    fullName = name,
                    roles = roles,
                    clubRoles = clubRoles
                }
            });
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="request">Registration information</param>
        /// <returns>Created user information</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            try
            {
                var result = await _authService.RegisterAsync(request);

                if (result == null)
                {
                    return BadRequest(new { message = "Registration failed" });
                }

                return CreatedAtAction(nameof(Register), new
                {
                    success = true,
                    message = "Registration successful. Please check your email to verify your account.",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Change password for authenticated user
        /// </summary>
        /// <param name="request">Password change information</param>
        /// <returns>Success status</returns>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            try
            {
                var result = await _authService.ChangePasswordAsync(userId, request);

                if (!result)
                {
                    return BadRequest(new { message = "Failed to change password" });
                }

                return Ok(new
                {
                    success = true,
                    message = "Password changed successfully. Please login again with your new password."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Request password reset (forgot password)
        /// </summary>
        /// <param name="request">Email address</param>
        /// <returns>Success status</returns>
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            await _authService.ForgotPasswordAsync(request);

            // Always return success for security reasons (don't reveal if email exists)
            return Ok(new
            {
                success = true,
                message = "If your email exists in our system, you will receive a password reset link."
            });
        }

        /// <summary>
        /// Reset password using reset token
        /// </summary>
        /// <param name="request">Reset password information</param>
        /// <returns>Success status</returns>
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var result = await _authService.ResetPasswordAsync(request);

            if (!result)
            {
                return BadRequest(new { message = "Failed to reset password. Invalid or expired token." });
            }

            return Ok(new
            {
                success = true,
                message = "Password reset successfully. Please login with your new password."
            });
        }

        /// <summary>
        /// Verify email address
        /// </summary>
        /// <param name="request">Email verification information</param>
        /// <returns>Success status</returns>
        [HttpPost("verify-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var result = await _authService.VerifyEmailAsync(request);

            if (!result)
            {
                return BadRequest(new { message = "Email verification failed. Invalid or expired token." });
            }

            return Ok(new
            {
                success = true,
                message = "Email verified successfully. You can now login to your account."
            });
        }

        /// <summary>
        /// Resend email verification
        /// </summary>
        /// <param name="request">Email address</param>
        /// <returns>Success status</returns>
        [HttpPost("resend-verification")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendVerification([FromBody] ForgotPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var result = await _authService.ResendVerificationEmailAsync(request.Email);

            if (!result)
            {
                return BadRequest(new { message = "Failed to resend verification email." });
            }

            return Ok(new
            {
                success = true,
                message = "Verification email sent. Please check your inbox."
            });
        }
    }
}

