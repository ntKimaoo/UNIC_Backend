using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Route("api/clubs/{clubId:int}/reports")]
    [ApiController]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IClubMemberService _clubMemberService;

        public ReportController(IReportService reportService, IClubMemberService clubMemberService)
        {
            _reportService = reportService;
            _clubMemberService = clubMemberService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetClubSummary(
            int clubId,
            [FromQuery] int? year,
            [FromQuery] int? month)
        {
            try
            {
                var userId = GetCurrentUserId();
                var isSystemAdmin = User.IsInRole("Admin");

                if (!isSystemAdmin && !await _clubMemberService.IsMemberAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });

                var data = await _reportService.GetSummaryAsync(clubId, userId, isSystemAdmin, year, month);
                return Ok(new { success = true, data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
            }
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics(
            int clubId,
            [FromQuery] int? year)
        {
            try
            {
                var userId = GetCurrentUserId();
                var isSystemAdmin = User.IsInRole("Admin");

                if (!isSystemAdmin && !await _clubMemberService.IsMemberAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });

                var data = await _reportService.GetAnalyticsAsync(clubId, year);
                return Ok(new { success = true, data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")
                ?? User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub")
                ?? User.FindFirst("userId");

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                throw new UnauthorizedAccessException("Không xác định được người dùng");

            return userId;
        }
    }
}

