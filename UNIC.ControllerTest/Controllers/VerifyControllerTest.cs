using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading.Tasks;
using UNIC.Presentation.Controllers;
using Xunit;

namespace UNIC.ControllerTest.Controllers
{
    public class VerifyControllerTest
    {
        private readonly Mock<IAttendanceService> _attendance;
        private readonly VerifyController _controller;

        public VerifyControllerTest()
        {
            _attendance = new Mock<IAttendanceService>();
            _controller = new VerifyController(_attendance.Object);
        }

        private static void SetupRequest(VerifyController controller, string queryString, string? acceptHeader = null)
        {
            var http = new DefaultHttpContext();
            var qs = queryString.StartsWith('?') ? queryString : "?" + queryString;
            http.Request.QueryString = new QueryString(qs);
            if (!string.IsNullOrEmpty(acceptHeader))
                http.Request.Headers.Accept = acceptHeader;
            controller.ControllerContext = new ControllerContext { HttpContext = http };
        }

        [Fact]
        public async Task Verify_ReturnsHtml400_WhenCodeMissing()
        {
            SetupRequest(_controller, "email=a@b.com");

            var result = await _controller.Verify("a@b.com", null, null);

            _attendance.Verify(
                s => s.VerifyAttendanceByLinkAsync(It.IsAny<string?>(), It.IsAny<string>()),
                Times.Never);
            var content = result.Should().BeOfType<ContentResult>().Subject;
            content.StatusCode.Should().Be(400);
            content.ContentType.Should().Contain("text/html");
            content.Content.Should().Contain("Thiếu mã xác nhận");
        }

        [Fact]
        public async Task Verify_ReturnsBadRequestJson_WhenCodeMissing_AndAcceptJson()
        {
            SetupRequest(_controller, "email=a@b.com", "application/json");

            var result = await _controller.Verify("a@b.com", null, null);

            _attendance.Verify(
                s => s.VerifyAttendanceByLinkAsync(It.IsAny<string?>(), It.IsAny<string>()),
                Times.Never);
            var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var body = bad.Value.Should().BeOfType<VerifyByLinkResult>().Subject;
            body.Success.Should().BeFalse();
            body.Message.Should().Contain("Thiếu mã");
        }

        [Fact]
        public async Task Verify_ReturnsBadRequestJson_WhenCodeMissing_AndFormatJsonQuery()
        {
            SetupRequest(_controller, "email=a@b.com&format=json");

            var result = await _controller.Verify("a@b.com", null, null);

            var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().BeOfType<VerifyByLinkResult>();
        }

        [Fact]
        public async Task Verify_ReturnsHtml200_WhenSuccess_FirstCheckIn()
        {
            SetupRequest(_controller, "email=u@test.com&code=abc");
            _attendance.Setup(s => s.VerifyAttendanceByLinkAsync("u@test.com", "abc")).ReturnsAsync(new VerifyByLinkResult
            {
                Success = true,
                Message = "OK",
                AlreadyCheckedIn = false,
                MemberName = "M",
                EventName = "E"
            });

            var result = await _controller.Verify("u@test.com", null, "abc");

            var content = result.Should().BeOfType<ContentResult>().Subject;
            content.ContentType.Should().Contain("text/html");
            content.Content.Should().Contain("Đã xác nhận điểm danh thành công");
            content.Content.Should().Contain("M");
            content.Content.Should().Contain("E");
        }

        [Fact]
        public async Task Verify_ReturnsHtml200_WhenSuccess_AlreadyCheckedIn()
        {
            SetupRequest(_controller, "code=x");
            _attendance.Setup(s => s.VerifyAttendanceByLinkAsync(null, "x")).ReturnsAsync(new VerifyByLinkResult
            {
                Success = true,
                Message = "Trùng",
                AlreadyCheckedIn = true,
                MemberName = "A",
                EventName = "B"
            });

            var result = await _controller.Verify(null, null, "x");

            var content = result.Should().BeOfType<ContentResult>().Subject;
            content.Content.Should().Contain("Đã điểm danh trước đó");
        }

        [Fact]
        public async Task Verify_UsesGmail_WhenEmailWhitespace()
        {
            SetupRequest(_controller, "gmail=g@mail.com&code=c1");
            _attendance.Setup(s => s.VerifyAttendanceByLinkAsync("g@mail.com", "c1"))
                .ReturnsAsync(new VerifyByLinkResult { Success = true, Message = "ok" });

            await _controller.Verify("   ", "g@mail.com", "c1");

            _attendance.Verify(s => s.VerifyAttendanceByLinkAsync("g@mail.com", "c1"), Times.Once);
        }

        [Fact]
        public async Task Verify_ReturnsOkJson_WhenSuccess_AndAcceptJson()
        {
            SetupRequest(_controller, "code=c", acceptHeader: "application/json");
            var dto = new VerifyByLinkResult { Success = true, Message = "Done", AlreadyCheckedIn = false };
            _attendance.Setup(s => s.VerifyAttendanceByLinkAsync(null, "c")).ReturnsAsync(dto);

            var result = await _controller.Verify(null, null, "c");

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeSameAs(dto);
        }

        [Fact]
        public async Task Verify_ReturnsHtml400_WhenServiceReturnsFailure()
        {
            SetupRequest(_controller, "code=bad");
            _attendance.Setup(s => s.VerifyAttendanceByLinkAsync(null, "bad"))
                .ReturnsAsync(new VerifyByLinkResult { Success = false, Message = "Invalid code" });

            var result = await _controller.Verify(null, null, "bad");

            var content = result.Should().BeOfType<ContentResult>().Subject;
            content.StatusCode.Should().Be(400);
            content.Content.Should().Contain("Xác nhận thất bại");
            content.Content.Should().Contain("Invalid code");
        }

        [Fact]
        public async Task Verify_ReturnsBadRequestJson_WhenServiceFails_AndWantJson()
        {
            SetupRequest(_controller, "code=bad", "application/json");
            var fail = new VerifyByLinkResult { Success = false, Message = "Expired" };
            _attendance.Setup(s => s.VerifyAttendanceByLinkAsync(null, "bad")).ReturnsAsync(fail);

            var result = await _controller.Verify(null, null, "bad");

            var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().BeSameAs(fail);
        }
    }
}
