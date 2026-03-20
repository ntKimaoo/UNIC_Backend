using System.Text.Json;
using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;
using Presentation.Authorization;

namespace Presentation.Controllers
{
    [Route("api/clubs/{clubId:int}/funds")]
    [ApiController]
    [Authorize]
    public class ClubFundController : ControllerBase
    {
        private readonly IClubFundService _clubFundService;
        private readonly IClubMemberService _clubMemberService;

        private readonly IPayOSService _payOSService;
        private readonly IWebHostEnvironment _environment;

        public ClubFundController(
            IClubFundService clubFundService,
            IClubMemberService clubMemberService,
            IPayOSService payOSService,
            IWebHostEnvironment environment)
        {
            _clubFundService = clubFundService;
            _clubMemberService = clubMemberService;
            _payOSService = payOSService;
            _environment = environment;
        }

        [HttpPost]
        [RequireMemberPolicy("createfinance")]
        public async Task<IActionResult> CreateFund(int clubId, [FromBody] CreateFundDto dto)
        {
            try
            {
                dto.ClubId = clubId;
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

        [HttpGet("capabilities")]
        public async Task<IActionResult> GetFundCapabilities(int clubId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });
                var data = await _clubFundService.GetFundCapabilitiesAsync(userId, clubId);
                return Ok(new { success = true, data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{fundId}")]
        [RequireMemberPolicy("viewfinance")]
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

        /// <param name="page">Trang, từ 1.</param>
        /// <param name="pageSize">1–100, mặc định 10.</param>
        [HttpGet]
        [RequireMemberPolicy("viewfinance")]
        public async Task<IActionResult> GetFundsByClub(
            int clubId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1)
                return BadRequest(new { success = false, message = "Page phải >= 1." });
            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { success = false, message = "PageSize từ 1 đến 100." });
            var userId = GetCurrentUserId();
            if (!await CanAccessClubAsync(userId, clubId))
                return StatusCode(403, new { success = false, message = "Bạn không có quyền xem quỹ của câu lạc bộ này." });
            var paged = await _clubFundService.GetFundsByClubIdPagedAsync(clubId, page, pageSize);
            return Ok(new { success = true, data = paged });
        }

        [HttpPost("contribute")]
        public async Task<IActionResult> Contribute(int clubId, [FromBody] ContributeRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });
                var fund = await _clubFundService.GetFundByIdAsync(request.FundId);
                if (fund == null)
                    return NotFound(new { success = false, message = "Quỹ không tồn tại." });
                if (fund.ClubId != clubId)
                    return StatusCode(403, new { success = false, message = "Quỹ không thuộc câu lạc bộ này." });
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

        
        [HttpGet("~/api/fund-contributions/payos-return/{orderCode:int}")]
        public async Task<IActionResult> GetPayOsContributionReturn(int orderCode)
        {
            try
            {
                var userId = GetCurrentUserId();
                var data = await _clubFundService.GetContributionPaymentStatusByOrderCodeAsync(userId, orderCode);
                if (data == null)
                    return NotFound(new { success = false, message = "Không tìm thấy giao dịch nộp tiền hoặc không phải của bạn." });
                if (!await CanAccessClubAsync(userId, data.ClubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ của quỹ này." });
                return Ok(new { success = true, data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("contribute/{transactionId:int}/status")]
        public async Task<IActionResult> GetContributionPaymentStatus(int clubId, int transactionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });
                var data = await _clubFundService.GetContributionPaymentStatusAsync(userId, clubId, transactionId);
                if (data == null)
                    return NotFound(new { success = false, message = "Không tìm thấy giao dịch nộp tiền hoặc bạn không có quyền xem." });
                return Ok(new { success = true, data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("dev/simulate-payos-paid/{transactionId:int}")]
        public async Task<IActionResult> SimulatePayOsPaidForDevelopment(int clubId, int transactionId)
        {
            if (!_environment.IsDevelopment())
                return NotFound();
            try
            {
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });
                var ok = await _clubFundService.TryCompleteOwnPendingContributionForDevelopmentAsync(userId, clubId, transactionId);
                if (!ok)
                    return BadRequest(new { success = false, message = "Không thể xác nhận: giao dịch không tồn tại, không phải của bạn, hoặc đã xử lý." });
                return Ok(new { success = true, message = "Đã giả lập thanh toán thành công (dev)." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("approve")]
        [RequireMemberPolicy("editfinance")]
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

        [HttpPost("payos-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOSWebhookClubScoped(CancellationToken cancellationToken)
        {
            return await HandlePayOSWebhook(cancellationToken);
        }

        [HttpPost("~/api/payos/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOSWebhook(CancellationToken cancellationToken)
        {
            return await HandlePayOSWebhook(cancellationToken);
        }

        private async Task<IActionResult> HandlePayOSWebhook(CancellationToken cancellationToken)
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
                    return Ok(new { success = true }); 

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

        /// <param name="page">Trang, bắt đầu từ 1.</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (1–100).</param>
        /// <param name="status">Mặc định (bỏ trống): APPROVED — chỉ các lần nộp đã thanh toán thành công. PENDING / REJECTED / ALL (mọi trạng thái).</param>
        /// <param name="scope">mine = chỉ các lần nộp của tôi.</param>
        [HttpGet("history/{fundId}")]
        [RequireMemberPolicy("viewfinance")]
        public async Task<IActionResult> GetHistory(
            int fundId,
            [FromQuery] string? status,
            [FromQuery] string? scope,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1)
                    return BadRequest(new { success = false, message = "Page phải >= 1." });
                if (pageSize < 1 || pageSize > 100)
                    return BadRequest(new { success = false, message = "PageSize từ 1 đến 100." });
                var fund = await _clubFundService.GetFundByIdAsync(fundId);
                if (fund == null)
                    return NotFound(new { success = false, message = "Quỹ không tồn tại." });
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, fund.ClubId))
                    return StatusCode(403, new { success = false, message = "Bạn không có quyền xem lịch sử quỹ của câu lạc bộ này." });
                var history = await _clubFundService.GetFundHistoryPagedAsync(fundId, status, scope, userId, page, pageSize);
                return Ok(new { success = true, data = history });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        [HttpGet("~/api/funds/{fundId}/location")]
        [RequireMemberPolicy("viewfinance")]
        public async Task<IActionResult> GetFundLocation(int fundId)
        {
            var fund = await _clubFundService.GetFundByIdAsync(fundId);
            if (fund == null)
            {
                return NotFound(new { success = false, message = "Quỹ không tồn tại." });
            }

            var userId = GetCurrentUserId();
            if (!await CanAccessClubAsync(userId, fund.ClubId))
            {
                return StatusCode(403, new { success = false, message = "Bạn không có quyền xem quỹ của câu lạc bộ này." });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    fundId = fund.FundId,
                    clubId = fund.ClubId
                }
            });
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