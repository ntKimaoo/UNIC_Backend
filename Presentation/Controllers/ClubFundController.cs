using System.Text.Json;
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

        private readonly IPayOSService _payOSService;

        public ClubFundController(IClubFundService clubFundService, IClubMemberService clubMemberService, IPayOSService payOSService)
        {
            _clubFundService = clubFundService;
            _clubMemberService = clubMemberService;
            _payOSService = payOSService;
        }

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
            var userId = GetCurrentUserId();
            if (!await CanAccessClubAsync(userId, fund.ClubId))
                return StatusCode(403, new { success = false, message = "Bạn không có quyền xem quỹ của câu lạc bộ này." });
            return Ok(new { success = true, data = fund });
        }

        [HttpGet("club/{clubId}")]
        public async Task<IActionResult> GetFundsByClub(int clubId)
        {
            var userId = GetCurrentUserId();
            if (!await CanAccessClubAsync(userId, clubId))
                return StatusCode(403, new { success = false, message = "Bạn không có quyền xem quỹ của câu lạc bộ này." });
            var funds = await _clubFundService.GetFundsByClubIdAsync(clubId);
            return Ok(new { success = true, data = funds });
        }

        /// <summary>
        /// Tạo yêu cầu nộp tiền vào quỹ và nhận link/QR PayOS để thanh toán.
        /// Sau khi thanh toán thành công (PayOS gọi webhook), quỹ sẽ được cập nhật tự động.
        /// </summary>
        [HttpPost("contribute")]
        public async Task<IActionResult> Contribute([FromBody] ContributeRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _clubFundService.CreateContributionAsync(userId, request, cancellationToken);
                return Ok(new { success = true, data = result, message = "Tạo yêu cầu nộp tiền thành công. Quét QR hoặc mở link để thanh toán." });
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

        [HttpPost("approve")]
        public async Task<IActionResult> ApproveFund([FromBody] ApproveFundDto dto)
        {
            try
            {
                var managerId = GetCurrentUserId();
                await _clubFundService.ApproveFundAsync(managerId, dto);
                return Ok(new { success = true, message = "Xử lý duyệt quỹ thành công." });
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

        /// <summary>
        /// Webhook PayOS gọi khi có thanh toán thành công. Không dùng JWT (AllowAnonymous).
        /// Cấu hình URL này tại https://my.payos.vn (ví dụ: https://your-api.com/api/clubfund/payos-webhook).
        /// </summary>
        [HttpPost("payos-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOSWebhook(CancellationToken cancellationToken)
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(body))
                    return BadRequest(new { success = false, message = "Empty body" });

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var code = root.TryGetProperty("code", out var c) ? c.GetString() : null;
                var success = root.TryGetProperty("success", out var s) && s.GetBoolean();
                if (!success || code != "00")
                    return Ok(new { success = true }); // PayOS vẫn cần 200 để không retry

                if (!root.TryGetProperty("data", out var dataEl) || !root.TryGetProperty("signature", out var sigEl))
                    return BadRequest(new { success = false, message = "Missing data or signature" });

                var receivedSignature = sigEl.GetString();
                if (string.IsNullOrEmpty(receivedSignature) || !_payOSService.VerifyWebhookSignature(receivedSignature, dataEl))
                    return BadRequest(new { success = false, message = "Invalid signature" });

                var orderCode = dataEl.TryGetProperty("orderCode", out var oc) ? oc.GetInt32() : 0;
                if (orderCode <= 0)
                    return BadRequest(new { success = false, message = "Invalid orderCode" });

                await _clubFundService.ProcessPayOSPaymentSuccessAsync(orderCode);
                return Ok(new { success = true });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Webhook processing error" });
            }
        }

        [HttpGet("history/{fundId}")]
        public async Task<IActionResult> GetHistory(int fundId, [FromQuery] string? status)
        {
            try
            {
                var fund = await _clubFundService.GetFundByIdAsync(fundId);
                if (fund == null)
                    return NotFound(new { success = false, message = "Quỹ không tồn tại." });
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, fund.ClubId))
                    return StatusCode(403, new { success = false, message = "Bạn không có quyền xem lịch sử quỹ của câu lạc bộ này." });
                var history = await _clubFundService.GetFundHistoryAsync(fundId, status);
                return Ok(new { success = true, data = history });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private async Task<bool> CanAccessClubAsync(Guid userId, int clubId)
        {
            if (User.IsInRole("Admin"))
                return true;
            return await _clubMemberService.IsMemberAsync(userId, clubId);
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