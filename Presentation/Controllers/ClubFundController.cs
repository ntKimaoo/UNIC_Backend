using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using BusinessLogic.DTOs;
using BusinessLogic.PaymentGateways;
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
    [Authorize]
    [Route("api/clubs/{clubId:int}/funds")]
    [ApiController]
    public class ClubFundController : ControllerBase
    {
        private readonly IClubFundService _clubFundService;
        private readonly IClubMemberService _clubMemberService;
        private readonly IClubPayOSSettingsService _clubPayOSSettingsService;

        private readonly IFundPaymentGatewayRegistry _paymentGatewayRegistry;
        private readonly IFundRepository _fundRepository;
        private readonly IClubPayOSSettingsRepository _clubPayOSSettingsRepository;
        private readonly IWebHostEnvironment _environment;

        public ClubFundController(
            IClubFundService clubFundService,
            IClubMemberService clubMemberService,
            IClubPayOSSettingsService clubPayOSSettingsService,
            IFundPaymentGatewayRegistry paymentGatewayRegistry,
            IFundRepository fundRepository,
            IClubPayOSSettingsRepository clubPayOSSettingsRepository,
            IWebHostEnvironment environment)
        {
            _clubFundService = clubFundService;
            _clubMemberService = clubMemberService;
            _clubPayOSSettingsService = clubPayOSSettingsService;
            _paymentGatewayRegistry = paymentGatewayRegistry;
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
            var providerGuide = PaymentGatewayProviderCodes.Normalize(settings?.PaymentProvider);
            var isConfigured = settings != null && providerGuide switch
            {
                PaymentGatewayProviderCodes.PayOS => !string.IsNullOrWhiteSpace(settings.ClientId)
                                                     && !string.IsNullOrWhiteSpace(settings.ApiKey)
                                                     && !string.IsNullOrWhiteSpace(settings.ChecksumKey),
                PaymentGatewayProviderCodes.VNPay => !string.IsNullOrWhiteSpace(settings.ClientId)
                                                   && !string.IsNullOrWhiteSpace(settings.ApiKey),
                _ => false
            };
            var isEnabled = settings?.IsEnabled ?? false;
            var onlineProviders = _paymentGatewayRegistry.ListOnlineProviders()
                .Select(p => new
                {
                    code = p.Code,
                    labelVi = p.DisplayNameVi,
                    credentialFields = p.CredentialFields
                        .OrderBy(f => f.SortOrder)
                        .Select(f => new
                        {
                            name = f.Name,
                            labelVi = f.LabelVi,
                            requiredWhenEnabled = f.RequiredWhenEnabled,
                            maxLength = f.MaxLength,
                            inputType = f.InputType,
                            helpTextVi = f.HelpTextVi
                        })
                        .ToList()
                })
                .ToList();

            return Ok(new
            {
                success = true,
                data = new
                {
                    clubId,
                    paymentCredentialSchemaVersion = 1,
                    onlinePaymentProviders = onlineProviders,
                    payos = new
                    {
                        isConfigured,
                        isEnabled,
                        noteVi = "Chỉ Club Manager mới có quyền cài đặt cổng thanh toán. Nếu CLB chưa cài đặt, có thể dùng chuyển khoản thủ công (ghi nhận tiền mặt)."
                    },
                    stepsVi = new[]
                    {
                        "Chọn cổng trong onlinePaymentProviders; mỗi cổng có credentialFields — FE dựng form động.",
                        "Đăng ký merchant trên trang của cổng thanh toán, rồi dán key vào form.",
                        "VNPay: cấu hình IPN/Return URL trên cổng trỏ về API backend (xem tài liệu triển khai).",
                        "Club Manager vào Cài đặt Thanh toán, chọn cổng và nhập thông tin để bật thanh toán trực tuyến."
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
                    var fund = await _clubFundService.GetFundByIdAsync(
                        fundId.Value, userId, User.IsInRole("Admin"));
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
                var data = await _clubFundService.GetFundCapabilitiesAsync(userId, clubId, User.IsInRole("Admin"));
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

        [HttpGet("{fundId:int}")]
        [RequireClubPolicy("viewfinance")]
        public async Task<IActionResult> GetFund(int fundId)
        {
            var userId = GetCurrentUserId();
            var fund = await _clubFundService.GetFundByIdAsync(fundId, userId, User.IsInRole("Admin"));
            if (fund == null)
                return NotFound(new { success = false, message = "Quỹ không tồn tại." });
            if (!await CanAccessClubAsync(userId, fund.ClubId))
                return StatusCode(403, new { success = false, message = "Bạn không có quyền xem quỹ của câu lạc bộ này." });
            return Ok(new { success = true, data = fund });
        }

        [HttpGet("{publicId:guid}")]
        [RequireClubPolicy("viewfinance")]
        public async Task<IActionResult> GetFundByPublicId(Guid publicId)
        {
            var userId = GetCurrentUserId();
            var fund = await _clubFundService.GetFundByPublicIdAsync(publicId, userId, User.IsInRole("Admin"));
            if (fund == null)
                return NotFound(new { success = false, message = "Quỹ không tồn tại." });
            if (!await CanAccessClubAsync(userId, fund.ClubId))
                return StatusCode(403, new { success = false, message = "Bạn không có quyền xem quỹ của câu lạc bộ này." });
            return Ok(new { success = true, data = fund });
        }

        [HttpDelete("{fundId:int}")]
        [RequireClubPolicy("deletefinance")]
        public async Task<IActionResult> SoftDeleteFund(int clubId, int fundId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });
                var isSystemAdmin = User.IsInRole("Admin");
                await _clubFundService.SoftDeleteFundAsync(userId, clubId, fundId, isSystemAdmin);
                return Ok(new { success = true, message = "Đã đóng quỹ (xóa mềm)." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
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

                var isSystemAdmin = User.IsInRole("Admin");
                var paged = await _clubFundService.GetMyFundsByClubIdPagedAsync(
                    clubId, userId, isSystemAdmin, mineType, status, search, sort, page, pageSize);

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
                var isSystemAdmin = User.IsInRole("Admin");
                var fund = await _clubFundService.GetFundByIdAsync(
                    request.FundId, userId, isSystemAdmin, includeSoftDeletedIfPrivileged: false);
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

        [HttpPost("contributions/cash")]
        [RequireClubPolicy("editfinance")]
        public async Task<IActionResult> RecordCashContribution(int clubId, [FromBody] RecordCashContributionRequestDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });
                var isAdmin = User.IsInRole("Admin");
                var data = await _clubFundService.RecordCashContributionAsync(userId, clubId, isAdmin, dto);
                return Ok(new { success = true, data, message = "Đã ghi nhận đóng góp tiền mặt." });
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
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("refund-requests")]
        public async Task<IActionResult> CreateFundRefundRequest(int clubId, [FromBody] CreateFundRefundRequestDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });
                var data = await _clubFundService.CreateFundRefundRequestAsync(userId, clubId, dto);
                return Ok(new { success = true, data, message = "Đã gửi yêu cầu hoàn tiền." });
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

        [HttpGet("refund-requests/mine")]
        public async Task<IActionResult> GetMyFundRefundRequests(int clubId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1)
                    return BadRequest(new { success = false, message = "Page phải >= 1." });
                if (pageSize < 1 || pageSize > 100)
                    return BadRequest(new { success = false, message = "PageSize từ 1 đến 100." });
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });
                var data = await _clubFundService.GetMyFundRefundRequestsPagedAsync(userId, clubId, page, pageSize);
                return Ok(new { success = true, data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("refund-requests")]
        [RequireClubPolicy("editfinance")]
        public async Task<IActionResult> GetClubFundRefundRequests(
            int clubId,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
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
                var isAdmin = User.IsInRole("Admin");
                var data = await _clubFundService.GetClubFundRefundRequestsPagedAsync(userId, clubId, isAdmin, status, page, pageSize);
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
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("refund-requests/{refundRequestId:int}/cancel")]
        public async Task<IActionResult> CancelFundRefundRequest(int clubId, int refundRequestId)
        {
            var userId = GetCurrentUserId();
            if (!await CanAccessClubAsync(userId, clubId))
                return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });
            var ok = await _clubFundService.CancelFundRefundRequestAsync(userId, clubId, refundRequestId);
            if (!ok)
                return BadRequest(new { success = false, message = "Không thể hủy yêu cầu (không tồn tại, không phải của bạn, hoặc không còn ở trạng thái chờ xử lý)." });
            return Ok(new { success = true, message = "Đã hủy yêu cầu hoàn tiền." });
        }

        [HttpPost("refund-requests/{refundRequestId:int}/complete")]
        [RequireClubPolicy("editfinance")]
        public async Task<IActionResult> CompleteFundRefundRequest(
            int clubId,
            int refundRequestId,
            [FromBody] CompleteFundRefundRequestDto? dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });
                var isAdmin = User.IsInRole("Admin");
                var ok = await _clubFundService.CompleteFundRefundRequestAsync(userId, clubId, isAdmin, refundRequestId, dto ?? new CompleteFundRefundRequestDto());
                if (!ok)
                    return BadRequest(new { success = false, message = "Không thể xác nhận hoàn tất (không tồn tại, trạng thái không hợp lệ, hoặc số dư quỹ không đủ)." });
                return Ok(new { success = true, message = "Đã xác nhận hoàn tiền và ghi chi vào quỹ." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("refund-requests/{refundRequestId:int}/reject")]
        [RequireClubPolicy("editfinance")]
        public async Task<IActionResult> RejectFundRefundRequest(int clubId, int refundRequestId, [FromBody] RejectFundRefundRequestDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });
                var isAdmin = User.IsInRole("Admin");
                var ok = await _clubFundService.RejectFundRefundRequestAsync(userId, clubId, isAdmin, refundRequestId, dto);
                if (!ok)
                    return BadRequest(new { success = false, message = "Không thể từ chối yêu cầu (không tồn tại hoặc trạng thái không hợp lệ)." });
                return Ok(new { success = true, message = "Đã từ chối yêu cầu hoàn tiền." });
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
        /// Club Manager hoàn tiền chủ động (không cần member tạo refund-request).
        /// Tạo 1 giao dịch EXPENSE (APPROVED) có RefundForTransactionId trỏ tới giao dịch INCOME gốc.
        /// </summary>
        [HttpPost("{fundId:int}/manager-refunds")]
        [RequireClubPolicy("editfinance")]
        public async Task<IActionResult> ManagerRefundContribution(
            int clubId,
            int fundId,
            [FromBody] ManagerRefundContributionDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });

                var isAdmin = User.IsInRole("Admin");
                var data = await _clubFundService.ManagerRefundContributionAsync(userId, clubId, fundId, isAdmin, dto);
                return Ok(new { success = true, data, message = "Đã hoàn tiền." });
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

        [HttpGet("~/api/fund-contributions/payos-return/{orderCode:long}")]
        public async Task<IActionResult> GetPayOsContributionReturn(long orderCode)
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
                if (!TryReadPayOsWebhookOrderCode(dataEl, out var orderCode))
                    return BadRequest(new { success = false, message = "Invalid orderCode" });

                var tx = await _fundRepository.GetTransactionForExternalCheckoutCompletionAsync(orderCode);
                if (tx?.ClubFund == null)
                    return Ok(new { success = true });

                var providerCode = PaymentGatewayProviderCodes.Normalize(tx.PaymentProvider);
                if (!string.Equals(providerCode, PaymentGatewayProviderCodes.PayOS, StringComparison.Ordinal))
                    return Ok(new { success = true });

                var settings = await _clubPayOSSettingsRepository.GetByClubIdAsync(tx.ClubFund.ClubId);
                if (settings == null || !settings.IsEnabled || string.IsNullOrWhiteSpace(settings.ChecksumKey))
                    return BadRequest(new { success = false, message = "Payment gateway not configured for club" });
                if (!string.Equals(PaymentGatewayProviderCodes.Normalize(settings.PaymentProvider), providerCode, StringComparison.Ordinal))
                    return BadRequest(new { success = false, message = "Payment provider mismatch" });

                var gateway = _paymentGatewayRegistry.Get(providerCode);
                if (string.IsNullOrEmpty(receivedSignature) || !gateway.VerifyWebhookSignature(settings, receivedSignature, dataEl))
                    return BadRequest(new { success = false, message = "Invalid signature" });

                await _clubFundService.ProcessPayOSPaymentSuccessAsync(orderCode);
                return Ok(new { success = true });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Webhook processing error" });
            }
        }

        private static bool TryReadPayOsWebhookOrderCode(JsonElement dataEl, out long orderCode)
        {
            orderCode = 0;
            if (!dataEl.TryGetProperty("orderCode", out var oc))
                return false;
            if (oc.ValueKind == JsonValueKind.Number && oc.TryGetInt64(out var n) && n > 0)
            {
                orderCode = n;
                return true;
            }

            if (oc.ValueKind == JsonValueKind.String
                && long.TryParse(oc.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var p)
                && p > 0)
            {
                orderCode = p;
                return true;
            }

            return false;
        }

        /// <summary>IPN VNPay (server-to-server). Đăng ký URL này trên cổng VNPay: GET/POST ~/api/vnpay/ipn</summary>
        [HttpGet("~/api/vnpay/ipn")]
        [HttpPost("~/api/vnpay/ipn")]
        [AllowAnonymous]
        public async Task<IActionResult> VnpayIpn()
        {
            try
            {
                var flat = FlattenVnpRequest(Request);
                if (!flat.TryGetValue("vnp_TxnRef", out var txnRef) || !int.TryParse(txnRef, out var txnId) || txnId <= 0)
                    return VnpIpnPlain("01", "Order not found");

                var tx = await _fundRepository.GetTransactionByIdAsync(txnId);
                if (tx?.ClubFund == null)
                    return VnpIpnPlain("01", "Order not found");

                var clubSettings = await _clubPayOSSettingsRepository.GetByClubIdAsync(tx.ClubFund.ClubId);
                if (clubSettings == null || !clubSettings.IsEnabled || string.IsNullOrWhiteSpace(clubSettings.ApiKey))
                    return VnpIpnPlain("02", "Gateway not configured");

                var txProvider = PaymentGatewayProviderCodes.Normalize(tx.PaymentProvider);
                var clubProvider = PaymentGatewayProviderCodes.Normalize(clubSettings.PaymentProvider);
                if (!string.Equals(txProvider, PaymentGatewayProviderCodes.VNPay, StringComparison.Ordinal)
                    || !string.Equals(clubProvider, PaymentGatewayProviderCodes.VNPay, StringComparison.Ordinal))
                    return VnpIpnPlain("03", "Provider mismatch");

                if (!VnPaySignature.VerifyIpn(flat, clubSettings.ApiKey.Trim(), out _))
                    return VnpIpnPlain("97", "Checksum failed");

                if (!flat.TryGetValue("vnp_ResponseCode", out var rc) || rc != "00")
                    return VnpIpnPlain("00", "Confirm Success");

                if (flat.TryGetValue("vnp_TransactionStatus", out var tst) && tst != "00")
                    return VnpIpnPlain("00", "Confirm Success");

                if (!flat.TryGetValue("vnp_Amount", out var amtStr)
                    || !long.TryParse(amtStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var vnpAmountMinor))
                    return VnpIpnPlain("04", "Invalid amount");

                var expectedMinor = (long)(tx.Amount * 100m);
                if (vnpAmountMinor != expectedMinor)
                    return VnpIpnPlain("04", "Invalid amount");

                await _fundRepository.TryApproveMemberContributionAsync(txnId);
                return VnpIpnPlain("00", "Confirm Success");
            }
            catch (Exception)
            {
                return VnpIpnPlain("99", "Unknown error");
            }
        }

        [HttpGet("~/api/vnpay/return")]
        [AllowAnonymous]
        public IActionResult VnpayReturn()
        {
            return Content(
                "<html><body><p>Giao dịch kết thúc. Bạn có thể đóng trang và quay lại ứng dụng.</p></body></html>",
                "text/html",
                Encoding.UTF8);
        }

        private static ContentResult VnpIpnPlain(string rspCode, string message) =>
            new ContentResult
            {
                Content = $"RspCode={rspCode}&Message={message}",
                ContentType = "text/plain; charset=utf-8",
                StatusCode = 200
            };

        private static Dictionary<string, string> FlattenVnpRequest(HttpRequest request)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in request.Query)
                d[kv.Key] = kv.Value.ToString();
            if (request.HasFormContentType)
            {
                foreach (var kv in request.Form)
                    d[kv.Key] = kv.Value.ToString();
            }

            return d;
        }

        [HttpGet("history/{fundId}")]
        [RequireClubPolicy("viewfinance")]
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
                var userId = GetCurrentUserId();
                var fund = await _clubFundService.GetFundByIdAsync(fundId, userId, User.IsInRole("Admin"));
                if (fund == null)
                    return NotFound(new { success = false, message = "Quỹ không tồn tại." });
                if (fund.ClubId != clubId)
                    return StatusCode(403, new { success = false, message = "Quỹ không thuộc câu lạc bộ này." });
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

        [HttpGet("{fundId:int}/member-contributions")]
        [RequireClubPolicy("viewfinance")]
        public async Task<IActionResult> GetFundMemberContributionOverview(int clubId, int fundId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!await CanAccessClubAsync(userId, clubId))
                    return StatusCode(403, new { success = false, message = "Bạn không thuộc câu lạc bộ này." });

                var data = await _clubFundService.GetFundMemberContributionOverviewAsync(userId, clubId, fundId);
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        [HttpGet("~/api/funds/{fundId}/location")]
        [RequireClubPolicy("viewfinance")]
        public async Task<IActionResult> GetFundLocation(int fundId)
        {
            var userId = GetCurrentUserId();
            var fund = await _clubFundService.GetFundByIdAsync(fundId, userId, User.IsInRole("Admin"));
            if (fund == null)
            {
                return NotFound(new { success = false, message = "Quỹ không tồn tại." });
            }

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