using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClubFundController : ControllerBase
    {
        private readonly IClubFundService _clubFundService;
        private readonly IClubMemberService _clubMemberService;

        public ClubFundController(IClubFundService clubFundService, IClubMemberService clubMemberService)
        {
            _clubFundService = clubFundService;
            _clubMemberService = clubMemberService;
        }

        /// <summary>
        /// Tạo quỹ mới cho một câu lạc bộ.
        /// Chỉ Club Manager / Vice Manager của club đó mới được tạo.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateFund([FromBody] CreateFundDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var fund = await _clubFundService.CreateFundAsync(userId, dto);
                return Ok(new { success = true, data = fund, message = "Tạo quỹ thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách club mà user hiện tại đã gia nhập (để hiển thị trang Quỹ / Budget Overview).
        /// Luôn đọc từ DB để phản ánh quyền mới nhất sau khi được gán vào club.
        /// </summary>
        [HttpGet("my-clubs")]
        public async Task<IActionResult> GetMyClubs()
        {
            var userId = GetCurrentUserId();
            var clubs = await _clubMemberService.GetMyClubsAsync(userId);
            return Ok(new { success = true, data = clubs });
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
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
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
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
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
            var userIdClaim = User.FindFirst("UserId")
                ?? User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                throw new UnauthorizedAccessException("Không xác định được người dùng");
            return userId;
        }
    }
}