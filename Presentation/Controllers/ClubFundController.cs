using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize]
    public class ClubFundController : ControllerBase
    {
        private readonly IClubFundService _clubFundService;

        public ClubFundController(IClubFundService clubFundService)
        {
            _clubFundService = clubFundService;
        }

        [HttpPost("request")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateFundRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _clubFundService.CreateRequestAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessRequest([FromBody] ProcessFundRequestDto request)
        {
            try
            {
                var managerId = GetCurrentUserId();
                await _clubFundService.ProcessRequestAsync(managerId, request);
                return Ok(new { message = "Xử lý yêu cầu thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId"); 
            if (userIdClaim == null) throw new Exception("Không xác định được người dùng");
            return Guid.Parse(userIdClaim.Value);
        }

        [HttpGet("history/{fundId}")]
        public async Task<IActionResult> GetHistory(int fundId, [FromQuery] string? status)
        {
            try
            {
                var history = await _clubFundService.GetFundHistoryAsync(fundId, status);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}