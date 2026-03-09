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

        [HttpGet("{fundId}")]
        public async Task<IActionResult> GetFund(int fundId)
        {
            var fund = await _clubFundService.GetFundByIdAsync(fundId);
            if (fund == null)
                return NotFound(new { success = false, message = "Quỹ không tồn tại." });
            return Ok(new { success = true, data = fund });
        }

        [HttpGet("club/{clubId}")]
        public async Task<IActionResult> GetFundsByClub(int clubId)
        {
            var funds = await _clubFundService.GetFundsByClubIdAsync(clubId);
            return Ok(new { success = true, data = funds });
        }

        [HttpPost("request")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateFundRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _clubFundService.CreateRequestAsync(userId, request);
                return Ok(new { success = true, data = new { transactionId = result.TransactionId, message = "Tạo yêu cầu thành công." } });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessRequest([FromBody] ProcessFundRequestDto request)
        {
            try
            {
                var managerId = GetCurrentUserId();
                await _clubFundService.ProcessRequestAsync(managerId, request);
                return Ok(new { success = true, message = "Xử lý yêu cầu thành công." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("history/{fundId}")]
        public async Task<IActionResult> GetHistory(int fundId, [FromQuery] string? status)
        {
            try
            {
                var history = await _clubFundService.GetFundHistoryAsync(fundId, status);
                return Ok(new { success = true, data = history });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null) throw new Exception("Không xác định được người dùng");
            return Guid.Parse(userIdClaim.Value);
        }
    }
}