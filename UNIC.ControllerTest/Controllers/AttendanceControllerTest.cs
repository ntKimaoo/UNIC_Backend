using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using UNIC.Presentation.Controllers;
using Xunit;

namespace UNIC.ControllerTest.Controllers
{
    public class AttendanceControllerTest
    {
        private readonly Mock<IAttendanceService> _mockAttendanceService;
        private readonly AttendanceController _controller;

        public AttendanceControllerTest()
        {
            _mockAttendanceService = new Mock<IAttendanceService>();
            _controller = new AttendanceController(_mockAttendanceService.Object);
        }

        /// <summary>
        /// Sets up ClaimsPrincipal with a valid NameIdentifier (authenticated user).
        /// </summary>
        private Guid SetupAuthenticatedUser()
        {
            var userId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            return userId;
        }

        /// <summary>
        /// Sets up ClaimsPrincipal with NO NameIdentifier (unauthenticated).
        /// </summary>
        private void SetupUnauthenticatedUser()
        {
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        #region RegisterMember

        [Fact]
        public async Task RegisterMember_ReturnsOk_WhenSuccess()
        {
            var userId = SetupAuthenticatedUser();
            _mockAttendanceService.Setup(s => s.RegisterMemberAsync(
                It.Is<EventRegistrationRequest>(r => r.EventId == 1 && r.UserId == userId)))
                .Returns(Task.CompletedTask);

            var result = await _controller.RegisterMember(1);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task RegisterMember_ReturnsUnauthorized_WhenNoToken()
        {
            SetupUnauthenticatedUser();

            var result = await _controller.RegisterMember(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task RegisterMember_ReturnsNotFound_WhenNotFoundException()
        {
            SetupAuthenticatedUser();
            _mockAttendanceService.Setup(s => s.RegisterMemberAsync(It.IsAny<EventRegistrationRequest>()))
                .ThrowsAsync(new NotFoundException("Event", 1));

            var result = await _controller.RegisterMember(1);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task RegisterMember_ReturnsConflict_WhenConflictException()
        {
            SetupAuthenticatedUser();
            _mockAttendanceService.Setup(s => s.RegisterMemberAsync(It.IsAny<EventRegistrationRequest>()))
                .ThrowsAsync(new ConflictException("Already registered"));

            var result = await _controller.RegisterMember(1);

            result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task RegisterMember_ReturnsBadRequest_WhenDomainException()
        {
            SetupAuthenticatedUser();
            _mockAttendanceService.Setup(s => s.RegisterMemberAsync(It.IsAny<EventRegistrationRequest>()))
                .ThrowsAsync(new DomainException("Registration closed"));

            var result = await _controller.RegisterMember(1);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RegisterMember_Returns500_WhenUnexpectedException()
        {
            SetupAuthenticatedUser();
            _mockAttendanceService.Setup(s => s.RegisterMemberAsync(It.IsAny<EventRegistrationRequest>()))
                .ThrowsAsync(new Exception("Database failure"));

            var result = await _controller.RegisterMember(1);

            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region CancelRegistration

        [Fact]
        public async Task CancelRegistration_ReturnsOk_WhenSuccess()
        {
            var userId = SetupAuthenticatedUser();
            _mockAttendanceService.Setup(s => s.CancelRegistrationAsync(1, userId))
                .Returns(Task.CompletedTask);

            var result = await _controller.CancelRegistration(1);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CancelRegistration_ReturnsUnauthorized_WhenNoToken()
        {
            SetupUnauthenticatedUser();

            var result = await _controller.CancelRegistration(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task CancelRegistration_ReturnsBadRequest_WhenException()
        {
            SetupAuthenticatedUser();
            _mockAttendanceService.Setup(s => s.CancelRegistrationAsync(It.IsAny<int>(), It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("Failed"));

            var result = await _controller.CancelRegistration(1);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region CheckIn

        [Fact]
        public async Task CheckIn_ReturnsOk_WhenSuccess()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var request = new CheckInRequest { EventId = 1, Code = "token123" };
            var response = new CheckInByQrResponse { Success = true, MemberName = "Test" };

            _mockAttendanceService.Setup(s => s.CheckInByQrTokenAsync(1, "token123"))
                .ReturnsAsync(response);

            var result = await _controller.CheckIn(1, request);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CheckIn_ReturnsBadRequest_WhenIdMismatch()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var request = new CheckInRequest { EventId = 2, Code = "token123" };

            var result = await _controller.CheckIn(1, request);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CheckIn_ReturnsBadRequest_WhenEmptyCode()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var request = new CheckInRequest { EventId = 1, Code = "  " };

            var result = await _controller.CheckIn(1, request);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CheckIn_ReturnsNotFound_WhenNotFoundException()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var request = new CheckInRequest { EventId = 1, Code = "invalid" };

            _mockAttendanceService.Setup(s => s.CheckInByQrTokenAsync(1, "invalid"))
                .ThrowsAsync(new NotFoundException("Attendance", "invalid"));

            var result = await _controller.CheckIn(1, request);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion

        #region GetMyCheckInQr

        [Fact]
        public async Task GetMyCheckInQr_ReturnsOk_WhenFound()
        {
            var userId = SetupAuthenticatedUser();
            var response = new CheckInQrResponse { EventId = 1, QrContent = "token123" };

            _mockAttendanceService.Setup(s => s.GetMyCheckInQrAsync(1, userId)).ReturnsAsync(response);

            var result = await _controller.GetMyCheckInQr(1);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetMyCheckInQr_ReturnsNotFound_WhenNull()
        {
            SetupAuthenticatedUser();
            _mockAttendanceService.Setup(s => s.GetMyCheckInQrAsync(1, It.IsAny<Guid>()))
                .ReturnsAsync((CheckInQrResponse?)null);

            var result = await _controller.GetMyCheckInQr(1);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetMyCheckInQr_ReturnsUnauthorized_WhenNoToken()
        {
            SetupUnauthenticatedUser();

            var result = await _controller.GetMyCheckInQr(1);

            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        #endregion
    }
}
