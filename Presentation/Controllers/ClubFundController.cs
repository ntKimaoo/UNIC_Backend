using System.Text.Json;
using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Repositories.Interface;
using UNIC.DataAccess.Repositories.Interface;
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
    //[Authorize]
    public class ClubFundController : ControllerBase
    {
        private readonly IClubFundService _clubFundService;
        private readonly IClubMemberService _clubMemberService;
        private readonly IClubPayOSSettingsService _clubPayOSSettingsService;

        private readonly IPayOSService _payOSService;
        private readonly IFundRepository _fundRepository;
        private readonly IClubPayOSSettingsRepository _clubPayOSSettingsRepository;
        private readonly IWebHostEnvironment _environment;

        public ClubFundController(
            IClubFundService clubFundService,
            IClubMemberService clubMemberService,
            IClubPayOSSettingsService clubPayOSSettingsService,
            IPayOSService payOSService,
            IFundRepository fundRepository,
            IClubPayOSSettingsRepository clubPayOSSettingsRepository,
            IWebHostEnvironment environment)
        {
            _clubFundService = clubFundService;
            _clubMemberService = clubMemberService;
            _clubPayOSSettingsService = clubPayOSSettingsService;
            _payOSService = payOSService;
            _fundRepository = fundRepository;
            _clubPayOSSettingsRepository = clubPayOSSettingsRepository;
            _environment = environment;
        }

        [HttpGet("payos-guide")]
        public async Task<IActionResult> GetPayOSGuide(int clubId)
        {
            var userId = GetCurrentUserId();
            if (!await CanAccessClubAsync(userId, clubId))
                return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });

            var settings = await _clubPayOSSettingsRepository.GetByClubIdAsync(clubId);
            var isConfigured = settings != null
                               && !string.IsNullOrWhiteSpace(settings.ClientId)
                               && !string.IsNullOrWhiteSpace(settings.ApiKey)
                               && !string.IsNullOrWhiteSpace(settings.ChecksumKey);
            var isEnabled = settings?.IsEnabled ?? false;

            return Ok(new
            {
                success = true,
                data = new
                {
                    clubId,
                    payos = new
                    {
                        isConfigured,
                        isEnabled,
                        noteVi = "Chỉ Club Manager mới có quyền cài đặt PayOS. Nếu CLB chưa cài đặt, có thể dùng chuyển khoản thủ công."
                    },
                    stepsVi = new[]
                    {
                        "Tạo/đăng ký tài khoản merchant trên PayOS cho chính CLB.",
                        "Lấy 3 thông tin: ClientId, ApiKey, ChecksumKey trên PayOS dashboard.",
                        "Club Manager vào mục Cài đặt Thanh toán và nhập 3 key để bật thanh toán qua QR."
                    }
                }
            });
        }

        [HttpGet("payos-settings")]
        [RequireClubPolicy("editfinance")]
        public async Task<IActionResult> GetClubPayOSSettings(int clubId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var isSystemAdmin = User.IsInRole("Admin");
                var data = await _clubPayOSSettingsService.GetAsync(userId, clubId, isSystemAdmin);
                return Ok(new { success = true, data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("payos-settings")]
        [RequireClubPolicy("editfinance")]
        public async Task<IActionResult> UpsertClubPayOSSettings(int clubId, [FromBody] UpsertClubPayOSSettingsDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var isSystemAdmin = User.IsInRole("Admin");
                var data = await _clubPayOSSettingsService.UpsertAsync(userId, clubId, isSystemAdmin, dto);
                return Ok(new { success = true, data, message = "Cập nhật liên kết PayOS thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [RequireClubPolicy("createfinance")]
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
        [RequireClubPolicy("viewfinance")]
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
        [RequireClubPolicy("viewfinance")]
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
        [RequireClubPolicy("viewfinance")]
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
        [RequireClubPolicy("viewfinance")]
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
        [RequireClubPolicy("viewfinance")]
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
            var isSystemAdmin = User.IsInRole("Admin");
            var paged = await _clubFundService.GetFundsByClubIdPagedAsync(
                clubId, userId, isSystemAdmin, status, search, sort, page, pageSize);
            return Ok(new { success = true, data = paged });
        }

        [HttpGet("my")]
        [RequireClubPolicy("viewfinance")]
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
        [RequireClubPolicy("editfinance")]
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
                var orderCode = dataEl.TryGetProperty("orderCode", out var oc) ? oc.GetInt32() : 0;
                if (orderCode <= 0)
                    return BadRequest(new { success = false, message = "Invalid orderCode" });

                var tx = await _fundRepository.GetTransactionByIdAsync(orderCode);
                if (tx?.ClubFund == null)
                    return Ok(new { success = true });

                var settings = await _clubPayOSSettingsRepository.GetByClubIdAsync(tx.ClubFund.ClubId);
                if (settings == null || !settings.IsEnabled || string.IsNullOrWhiteSpace(settings.ChecksumKey))
                    return BadRequest(new { success = false, message = "PayOS not configured for club" });

                if (string.IsNullOrEmpty(receivedSignature) || !_payOSService.VerifyWebhookSignature(settings.ChecksumKey, receivedSignature, dataEl))
                    return BadRequest(new { success = false, message = "Invalid signature" });

                await _clubFundService.ProcessPayOSPaymentSuccessAsync(orderCode);
                return Ok(new { success = true });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Webhook processing error" });
            }
        }

        [HttpGet("history/{fundId}")]
        //[RequireClubPolicy("viewfinance")]
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
        [RequireClubPolicy("viewfinance")]
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