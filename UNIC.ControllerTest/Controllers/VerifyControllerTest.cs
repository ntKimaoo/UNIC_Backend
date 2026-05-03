using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Moq;
using System;
using System.Threading.Tasks;
using UNIC.Presentation.Controllers;
using Xunit;
using FluentAssertions;

namespace UNIC.ControllerTest.Controllers
{
    public class VerifyControllerTest
    {
        private readonly Mock<IAttendanceService> _mockAttendanceService;
        private readonly VerifyController _controller;

        public VerifyControllerTest()
        {
            _mockAttendanceService = new Mock<IAttendanceService>();
            _controller = new VerifyController(_mockAttendanceService.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        /// <summary>
        /// Helper: set Accept header to application/json so the controller returns JSON.
        /// </summary>
        private void SetJsonAccept()
        {
            _controller.HttpContext.Request.Headers["Accept"] = "application/json";
        }

        /// <summary>
        /// Helper: set ?format=json query param.
        /// </summary>
        private void SetFormatJsonQuery()
        {
            _controller.HttpContext.Request.QueryString = new QueryString("?format=json");
        }

        #region Missing code — ReturnError


        [Fact]
        public async Task Verify_MissingCode_ReturnsHtml400()
        {
            // No Accept header → HTML response
            var result = await _controller.Verify("test@uni.edu", null, null);

            var content = result.Should().BeOfType<ContentResult>().Subject;
            content.StatusCode.Should().Be(400);
            content.ContentType.Should().Contain("text/html");
            content.Content.Should().Contain("Thiếu mã xác nhận");
        }

        [Fact]
        public async Task Verify_EmptyCode_ReturnsHtml400()
        {
            var result = await _controller.Verify("test@uni.edu", null, "   ");

            var content = result.Should().BeOfType<ContentResult>().Subject;
            content.StatusCode.Should().Be(400);
            content.Content.Should().Contain("Thiếu mã xác nhận");
        }

        [Fact]
        public async Task Verify_MissingCode_AcceptJson_ReturnsBadRequestJson()
        {
            SetJsonAccept();

            var result = await _controller.Verify("test@uni.edu", null, null);

            var badReq = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badReq.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Verify_MissingCode_FormatJsonQuery_ReturnsBadRequestJson()
        {
            SetFormatJsonQuery();

            var result = await _controller.Verify("test@uni.edu", null, null);

            var badReq = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badReq.StatusCode.Should().Be(400);
        }

        #endregion

        #region Success — HTML

        [Fact]
        public async Task Verify_Success_ReturnsHtmlWithSuccessMessage()
        {
            _mockAttendanceService
                .Setup(s => s.VerifyAttendanceByLinkAsync("user@test.com", "ABC123"))
                .ReturnsAsync(new VerifyByLinkResult
                {
                    Success = true,
                    Message = "Điểm danh OK",
                    MemberName = "Nguyen Van A",
                    EventName = "Tech Talk",
                    AlreadyCheckedIn = false
                });

            var result = await _controller.Verify("user@test.com", null, "ABC123");

            var content = result.Should().BeOfType<ContentResult>().Subject;
            content.ContentType.Should().Contain("text/html");
            content.Content.Should().Contain("Đã xác nhận điểm danh thành công");
            content.Content.Should().Contain("Nguyen Van A");
            content.Content.Should().Contain("Tech Talk");
        }

        [Fact]
        public async Task Verify_AlreadyCheckedIn_ReturnsHtmlWithAlreadyMessage()
        {
            _mockAttendanceService
                .Setup(s => s.VerifyAttendanceByLinkAsync("user@test.com", "ABC123"))
                .ReturnsAsync(new VerifyByLinkResult
                {
                    Success = true,
                    Message = "Đã điểm danh",
                    MemberName = "Nguyen Van A",
                    EventName = "Tech Talk",
                    AlreadyCheckedIn = true
                });

            var result = await _controller.Verify("user@test.com", null, "ABC123");

            var content = result.Should().BeOfType<ContentResult>().Subject;
            content.Content.Should().Contain("Đã điểm danh trước đó");
        }

        #endregion

        #region Success — JSON

        [Fact]
        public async Task Verify_Success_AcceptJson_ReturnsOk()
        {
            SetJsonAccept();
            _mockAttendanceService
                .Setup(s => s.VerifyAttendanceByLinkAsync("user@test.com", "CODE1"))
                .ReturnsAsync(new VerifyByLinkResult { Success = true, Message = "OK" });

            var result = await _controller.Verify("user@test.com", null, "CODE1");

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Verify_Success_FormatJson_ReturnsOk()
        {
            SetFormatJsonQuery();
            _mockAttendanceService
                .Setup(s => s.VerifyAttendanceByLinkAsync("user@test.com", "CODE1"))
                .ReturnsAsync(new VerifyByLinkResult { Success = true, Message = "OK" });

            var result = await _controller.Verify("user@test.com", null, "CODE1");

            result.Should().BeOfType<OkObjectResult>();
        }

        #endregion

        #region Failure

        [Fact]
        public async Task Verify_Failure_ReturnsHtml400()
        {
            _mockAttendanceService
                .Setup(s => s.VerifyAttendanceByLinkAsync("user@test.com", "BADCODE"))
                .ReturnsAsync(new VerifyByLinkResult
                {
                    Success = false,
                    Message = "Mã không hợp lệ"
                });

            var result = await _controller.Verify("user@test.com", null, "BADCODE");

            var content = result.Should().BeOfType<ContentResult>().Subject;
            content.StatusCode.Should().Be(400);
            content.ContentType.Should().Contain("text/html");
            content.Content.Should().Contain("Xác nhận thất bại");
            content.Content.Should().Contain("Mã không hợp lệ");
        }

        [Fact]
        public async Task Verify_Failure_AcceptJson_ReturnsBadRequest()
        {
            SetJsonAccept();
            _mockAttendanceService
                .Setup(s => s.VerifyAttendanceByLinkAsync("user@test.com", "BADCODE"))
                .ReturnsAsync(new VerifyByLinkResult { Success = false, Message = "Invalid" });

            var result = await _controller.Verify("user@test.com", null, "BADCODE");

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region Email fallback (gmail param)

        [Fact]
        public async Task Verify_UsesGmailParam_WhenEmailIsNull()
        {
            _mockAttendanceService
                .Setup(s => s.VerifyAttendanceByLinkAsync("gmail@test.com", "X1"))
                .ReturnsAsync(new VerifyByLinkResult { Success = true, Message = "OK" });

            SetJsonAccept();
            var result = await _controller.Verify(null, "gmail@test.com", "X1");

            result.Should().BeOfType<OkObjectResult>();
            _mockAttendanceService.Verify(
                s => s.VerifyAttendanceByLinkAsync("gmail@test.com", "X1"), Times.Once);
        }

        [Fact]
        public async Task Verify_UsesGmailParam_WhenEmailIsWhitespace()
        {
            _mockAttendanceService
                .Setup(s => s.VerifyAttendanceByLinkAsync("gmail@test.com", "X1"))
                .ReturnsAsync(new VerifyByLinkResult { Success = true, Message = "OK" });

            SetJsonAccept();
            var result = await _controller.Verify("   ", "gmail@test.com", "X1");

            result.Should().BeOfType<OkObjectResult>();
            _mockAttendanceService.Verify(
                s => s.VerifyAttendanceByLinkAsync("gmail@test.com", "X1"), Times.Once);
        }

        [Fact]
        public async Task Verify_PrefersEmail_OverGmail()
        {
            _mockAttendanceService
                .Setup(s => s.VerifyAttendanceByLinkAsync("email@test.com", "X1"))
                .ReturnsAsync(new VerifyByLinkResult { Success = true, Message = "OK" });

            SetJsonAccept();
            var result = await _controller.Verify("email@test.com", "gmail@test.com", "X1");

            result.Should().BeOfType<OkObjectResult>();
            _mockAttendanceService.Verify(
                s => s.VerifyAttendanceByLinkAsync("email@test.com", "X1"), Times.Once);
        }

        #endregion

        #region Both email and gmail null

        [Fact]
        public async Task Verify_BothEmailAndGmailNull_PassesNullToService()
        {
            _mockAttendanceService
                .Setup(s => s.VerifyAttendanceByLinkAsync(null, "CODE"))
                .ReturnsAsync(new VerifyByLinkResult { Success = false, Message = "Email required" });

            SetJsonAccept();
            var result = await _controller.Verify(null, null, "CODE");

            result.Should().BeOfType<BadRequestObjectResult>();
            _mockAttendanceService.Verify(
                s => s.VerifyAttendanceByLinkAsync(null, "CODE"), Times.Once);
        }

        #endregion

        #region Null MemberName / EventName in HTML

        [Fact]
        public async Task Verify_Success_NullMemberAndEvent_DoesNotCrash()
        {
            _mockAttendanceService
                .Setup(s => s.VerifyAttendanceByLinkAsync("u@t.com", "C1"))
                .ReturnsAsync(new VerifyByLinkResult
                {
                    Success = true,
                    Message = "Done",
                    MemberName = null,
                    EventName = null,
                    AlreadyCheckedIn = false
                });

            var result = await _controller.Verify("u@t.com", null, "C1");

            var content = result.Should().BeOfType<ContentResult>().Subject;
            content.ContentType.Should().Contain("text/html");
            content.Content.Should().Contain("Đã xác nhận điểm danh thành công");
        }

        #endregion
    }
}
