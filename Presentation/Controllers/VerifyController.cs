using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace UNIC.Presentation.Controllers
{
    [ApiController]
    [Route("api/verify")]
    public class VerifyController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;

        public VerifyController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Duration = 0)]
        public async Task<IActionResult> Verify([FromQuery] string? email, [FromQuery] string? gmail, [FromQuery] string? code)
        {
            var emailParam = !string.IsNullOrWhiteSpace(email) ? email : gmail;
            if (string.IsNullOrWhiteSpace(code))
                return ReturnError("Thiếu mã xác nhận.", 400, Request);

            var result = await _attendanceService.VerifyAttendanceByLinkAsync(emailParam, code);

            var wantJson = Request.GetTypedHeaders().Accept?.Any(a => a.MediaType.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase)) == true
                || string.Equals(Request.Query["format"], "json", StringComparison.OrdinalIgnoreCase);

            if (wantJson)
                return result.Success ? Ok(result) : BadRequest(result);

            if (result.Success)
            {
                var html = $"<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Xác nhận điểm danh</title></head><body style='font-family:Arial,sans-serif;max-width:480px;margin:40px auto;padding:20px;text-align:center'><h2 style='color:#2e7d32'>{(result.AlreadyCheckedIn ? "Đã điểm danh trước đó" : "Đã xác nhận điểm danh thành công")}</h2><p>{result.Message}</p><p>{result.MemberName ?? ""} - {result.EventName ?? ""}</p></body></html>";
                return Content(html, "text/html; charset=utf-8");
            }
            var errHtml = $"<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Lỗi xác nhận</title></head><body style='font-family:Arial,sans-serif;max-width:480px;margin:40px auto;padding:20px;text-align:center'><h2 style='color:#c62828'>Xác nhận thất bại</h2><p>{result.Message}</p></body></html>";
            return new ContentResult { Content = errHtml, ContentType = "text/html; charset=utf-8", StatusCode = 400 };
        }

        private static IActionResult ReturnError(string message, int statusCode, HttpRequest request)
        {
            var wantJson = request.GetTypedHeaders().Accept?.Any(a => a.MediaType.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase)) == true
                || string.Equals(request.Query["format"], "json", StringComparison.OrdinalIgnoreCase);
            if (wantJson)
                return statusCode == 400 ? new BadRequestObjectResult(new VerifyByLinkResult { Success = false, Message = message }) : new ObjectResult(new VerifyByLinkResult { Success = false, Message = message }) { StatusCode = statusCode };
            var html = $"<!DOCTYPE html><html><head><meta charset='utf-8'><title>Lỗi</title></head><body style='font-family:Arial;margin:40px'><h2 style='color:#c62828'>Lỗi</h2><p>{message}</p></body></html>";
            var content = new ContentResult { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = statusCode };
            return content;
        }
    }
}
