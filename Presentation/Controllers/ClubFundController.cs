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

        [HttpGet("report-summary")]
        [RequireMemberPolicy("viewfinance")]
        public async Task<IActionResult> GetFundReportSummary(
            int clubId,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc)
        {
            if (!IsValidDateRange(fromUtc, toUtc))
                return BuildBadRequest(
                    "INVALID_DATE_RANGE",
                    "Từ ngày không được lớn hơn đến ngày.",
                    new { fromField = "fromUtc", toField = "toUtc" });

            var userId = GetCurrentUserId();
            if (!await CanAccessClubAsync(userId, clubId))
                return StatusCode(403, new { success = false, message = "Bạn không có quyền xem quỹ của câu lạc bộ này." });
            var data = await _clubFundService.GetClubFundReportSummaryAsync(clubId, fromUtc, toUtc);
            return Ok(new { success = true, data });
        }

        [HttpGet("transactions")]
        [RequireMemberPolicy("viewfinance")]
        public async Task<IActionResult> GetClubFundTransactions(
            int clubId,
            [FromQuery] int? fundId,
            [FromQuery] string? status,
            [FromQuery] string? scope,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1)
                    return BuildBadRequest("INVALID_PAGE", "Page phải >= 1.");
                if (pageSize < 1 || pageSize > 100)
                    return BuildBadRequest("INVALID_PAGE_SIZE", "PageSize từ 1 đến 100.");
                if (!IsValidDateRange(fromUtc, toUtc))
                    return BuildBadRequest(
                        "INVALID_DATE_RANGE",
                        "Từ ngày không được lớn hơn đến ngày.",
                        new { fromField = "fromUtc", toField = "toUtc" });

                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không có quyền xem quỹ của câu lạc bộ này." });

                if (fundId.HasValue)
                {
                    var fund = await _clubFundService.GetFundByIdAsync(fundId.Value);
                    if (fund == null)
                        return NotFound(new { success = false, message = "Quỹ không tồn tại." });
                    if (fund.ClubId != clubId)
                        return BuildBadRequest("INVALID_FUND_ID", "Quỹ không thuộc câu lạc bộ này.");
                }

                var data = await _clubFundService.GetClubFundTransactionsPagedAsync(
                    clubId, fundId, status, scope, userId, fromUtc, toUtc, page, pageSize);
                return Ok(new { success = true, data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BuildBadRequest("INVALID_REQUEST", ex.Message);
            }
            catch (Exception ex)
            {
                return BuildBadRequest("TRANSACTION_QUERY_ERROR", ex.Message);
            }
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

        [HttpGet("categories")]
        [RequireMemberPolicy("viewfinance")]
        public async Task<IActionResult> GetFundCategories(int clubId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });
                var data = await _clubFundService.GetFundCategoriesForClubAsync(clubId);
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

        [HttpGet]
        [RequireMemberPolicy("viewfinance")]
        public async Task<IActionResult> GetFundsByClub(
            int clubId,
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] string? sort,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 9)
        {
            if (page < 1)
                return BadRequest(new { success = false, message = "Page phải >= 1." });
            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { success = false, message = "PageSize từ 1 đến 100." });
            var userId = GetCurrentUserId();
            if (!await CanAccessClubAsync(userId, clubId))
                return StatusCode(403, new { success = false, message = "Bạn không có quyền xem quỹ của câu lạc bộ này." });
            var paged = await _clubFundService.GetFundsByClubIdPagedAsync(clubId, status, search, sort, page, pageSize);
            return Ok(new { success = true, data = paged });
        }

        [HttpGet("my")]
        [RequireMemberPolicy("viewfinance")]
        public async Task<IActionResult> GetMyFunds(
            int clubId,
            [FromQuery] string? mineType,
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] string? sort,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 9)
        {
            try
            {
                if (page < 1)
                    return BadRequest(new { success = false, message = "Page phải >= 1." });
                if (pageSize < 1 || pageSize > 100)
                    return BadRequest(new { success = false, message = "PageSize từ 1 đến 100." });
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không có quyền xem quỹ của câu lạc bộ này." });

                var paged = await _clubFundService.GetMyFundsByClubIdPagedAsync(
                    clubId, userId, mineType, status, search, sort, page, pageSize);

                return Ok(new { success = true, data = paged });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
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
            int clubId,
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
                if (fund.ClubId != clubId)
                    return StatusCode(403, new { success = false, message = "Quỹ không thuộc câu lạc bộ này." });
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

        private static bool IsValidDateRange(DateTime? fromUtc, DateTime? toUtc)
        {
            if (!fromUtc.HasValue || !toUtc.HasValue)
                return true;

            return fromUtc.Value <= toUtc.Value;
        }

        private BadRequestObjectResult BuildBadRequest(string errorCode, string message, object? details = null)
        {
            return BadRequest(new
            {
                success = false,
                errorCode,
                message,
                details
            });
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