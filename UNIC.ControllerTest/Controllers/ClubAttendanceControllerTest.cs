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
using System.Text.Json;
using System.Threading.Tasks;
using UNIC.Presentation.Controllers;
using Xunit;

namespace UNIC.ControllerTest.Controllers
{
    public class ClubAttendanceControllerTest
    {
        private readonly Mock<IAttendanceService> _mockAttendanceService;
        private readonly Mock<IEventService> _mockEventService;
        private readonly Mock<global::DataAccess.Repositories.Interface.IUnitOfWork> _mockUnitOfWork;
        private readonly ClubAttendanceController _controller;
        private const int ClubId = 1;

        public ClubAttendanceControllerTest()
        {
            _mockAttendanceService = new Mock<IAttendanceService>();
            _mockEventService = new Mock<IEventService>();
            _mockUnitOfWork = new Mock<global::DataAccess.Repositories.Interface.IUnitOfWork>();
            _controller = new ClubAttendanceController(_mockAttendanceService.Object, _mockEventService.Object, _mockUnitOfWork.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        private void SetupEventBelongsToClub(int eventId, int clubId = ClubId)
        {
            _mockEventService.Setup(s => s.GetEventByIdAsync(eventId))
                .ReturnsAsync(new EventDetailDto { EventId = eventId, ClubId = clubId });
        }

        #region ApproveRegistration

        [Fact]
        public async Task ApproveRegistration_ReturnsOk_WhenSuccess()
        {
            SetupEventBelongsToClub(1);
            var userId = Guid.NewGuid();
            _mockAttendanceService.Setup(s => s.ApproveRegistrationAsync(1, userId)).Returns(Task.CompletedTask);
            var result = await _controller.ApproveRegistration(ClubId, 1, userId);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ApproveRegistration_ReturnsBadRequest_WhenWrongClub()
        {
            SetupEventBelongsToClub(1, clubId: 999);
            var result = await _controller.ApproveRegistration(ClubId, 1, Guid.NewGuid());
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ApproveRegistration_ReturnsBadRequest_WhenServiceThrows()
        {
            SetupEventBelongsToClub(1);
            var userId = Guid.NewGuid();
            _mockAttendanceService.Setup(s => s.ApproveRegistrationAsync(1, userId)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.ApproveRegistration(ClubId, 1, userId);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ApproveRegistration_ReturnsBadRequest_WhenGetEventThrows()
        {
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ThrowsAsync(new Exception("not found"));
            var result = await _controller.ApproveRegistration(ClubId, 1, Guid.NewGuid());
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region RejectRegistration

        [Fact]
        public async Task RejectRegistration_ReturnsOk_WhenSuccess()
        {
            SetupEventBelongsToClub(1);
            var userId = Guid.NewGuid();
            _mockAttendanceService.Setup(s => s.RejectRegistrationAsync(1, userId)).Returns(Task.CompletedTask);
            var result = await _controller.RejectRegistration(ClubId, 1, userId);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task RejectRegistration_ReturnsBadRequest_WhenWrongClub()
        {
            SetupEventBelongsToClub(1, clubId: 999);
            var result = await _controller.RejectRegistration(ClubId, 1, Guid.NewGuid());
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RejectRegistration_ReturnsBadRequest_WhenServiceThrows()
        {
            SetupEventBelongsToClub(1);
            var userId = Guid.NewGuid();
            _mockAttendanceService.Setup(s => s.RejectRegistrationAsync(1, userId)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.RejectRegistration(ClubId, 1, userId);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RejectRegistration_ReturnsBadRequest_WhenGetEventThrows()
        {
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ThrowsAsync(new Exception("err"));
            var result = await _controller.RejectRegistration(ClubId, 1, Guid.NewGuid());
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region BulkApproveRegistrations

        [Fact]
        public async Task BulkApproveRegistrations_ReturnsOk_WhenSuccess()
        {
            SetupEventBelongsToClub(1);
            var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            _mockAttendanceService.Setup(s => s.BulkApproveAsync(1, userIds)).ReturnsAsync(2);
            var result = await _controller.BulkApproveRegistrations(ClubId, 1, userIds);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task BulkApproveRegistrations_ReturnsBadRequest_WhenNullList()
        {
            var result = await _controller.BulkApproveRegistrations(ClubId, 1, null!);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task BulkApproveRegistrations_ReturnsBadRequest_WhenEmptyList()
        {
            var result = await _controller.BulkApproveRegistrations(ClubId, 1, new List<Guid>());
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task BulkApproveRegistrations_ReturnsBadRequest_WhenWrongClub()
        {
            SetupEventBelongsToClub(1, clubId: 999);
            var userIds = new List<Guid> { Guid.NewGuid() };
            var result = await _controller.BulkApproveRegistrations(ClubId, 1, userIds);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task BulkApproveRegistrations_Returns500_WhenServiceThrows()
        {
            SetupEventBelongsToClub(1);
            var userIds = new List<Guid> { Guid.NewGuid() };
            _mockAttendanceService.Setup(s => s.BulkApproveAsync(1, userIds)).ThrowsAsync(new Exception("err"));
            var result = await _controller.BulkApproveRegistrations(ClubId, 1, userIds);
            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task BulkApproveRegistrations_Returns500_WhenGetEventThrows()
        {
            var userIds = new List<Guid> { Guid.NewGuid() };
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ThrowsAsync(new Exception("err"));
            var result = await _controller.BulkApproveRegistrations(ClubId, 1, userIds);
            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        #endregion

        #region GenerateCheckInCode

        [Fact]
        public async Task GenerateCheckInCode_ReturnsOk_WhenSuccess()
        {
            SetupEventBelongsToClub(1);
            var response = new CheckInCodeResponse { EventId = 1, Code = "ABC123" };
            _mockAttendanceService.Setup(s => s.GenerateCheckInCodeAsync(1)).ReturnsAsync(response);
            var result = await _controller.GenerateCheckInCode(ClubId, 1);
            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GenerateCheckInCode_ReturnsBadRequest_WhenWrongClub()
        {
            SetupEventBelongsToClub(1, clubId: 999);
            var result = await _controller.GenerateCheckInCode(ClubId, 1);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GenerateCheckInCode_Returns404_WhenNotFound()
        {
            SetupEventBelongsToClub(1);
            _mockAttendanceService.Setup(s => s.GenerateCheckInCodeAsync(1)).ThrowsAsync(new NotFoundException("Event", 1));
            var result = await _controller.GenerateCheckInCode(ClubId, 1);
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GenerateCheckInCode_Returns500_WhenException()
        {
            SetupEventBelongsToClub(1);
            _mockAttendanceService.Setup(s => s.GenerateCheckInCodeAsync(1)).ThrowsAsync(new Exception("err"));
            var result = await _controller.GenerateCheckInCode(ClubId, 1);
            result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GenerateCheckInCode_Returns404_WhenGetEventThrowsNotFound()
        {
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ThrowsAsync(new NotFoundException("Event", 1));
            var result = await _controller.GenerateCheckInCode(ClubId, 1);
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GenerateCheckInCode_Returns500_WhenGetEventThrowsGeneric()
        {
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ThrowsAsync(new Exception("err"));
            var result = await _controller.GenerateCheckInCode(ClubId, 1);
            result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        #endregion

        #region CheckInByQr

        [Fact]
        public async Task CheckInByQr_ReturnsOk_WhenSuccess()
        {
            SetupEventBelongsToClub(1);
            var response = new CheckInByQrResponse { Success = true, MemberName = "Test" };
            _mockAttendanceService.Setup(s => s.CheckInByQrTokenAsync(1, "token123")).ReturnsAsync(response);
            var result = await _controller.CheckInByQr(ClubId, 1, new CheckInByQrRequest { Token = "token123" });
            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CheckInByQr_ReturnsBadRequest_WhenWrongClub()
        {
            SetupEventBelongsToClub(1, clubId: 999);
            var result = await _controller.CheckInByQr(ClubId, 1, new CheckInByQrRequest { Token = "abc" });
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CheckInByQr_ReturnsBadRequest_WhenNullToken()
        {
            SetupEventBelongsToClub(1);
            var result = await _controller.CheckInByQr(ClubId, 1, new CheckInByQrRequest { Token = null });
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CheckInByQr_ReturnsBadRequest_WhenEmptyToken()
        {
            SetupEventBelongsToClub(1);
            var result = await _controller.CheckInByQr(ClubId, 1, new CheckInByQrRequest { Token = "   " });
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CheckInByQr_ReturnsBadRequest_WhenNullRequest()
        {
            SetupEventBelongsToClub(1);
            var result = await _controller.CheckInByQr(ClubId, 1, null!);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CheckInByQr_Returns404_WhenNotFound()
        {
            SetupEventBelongsToClub(1);
            _mockAttendanceService.Setup(s => s.CheckInByQrTokenAsync(1, "invalid")).ThrowsAsync(new NotFoundException("Attendance", "invalid"));
            var result = await _controller.CheckInByQr(ClubId, 1, new CheckInByQrRequest { Token = "invalid" });
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CheckInByQr_ReturnsBadRequest_WhenDomainException()
        {
            SetupEventBelongsToClub(1);
            _mockAttendanceService.Setup(s => s.CheckInByQrTokenAsync(1, "abc")).ThrowsAsync(new DomainException("expired"));
            var result = await _controller.CheckInByQr(ClubId, 1, new CheckInByQrRequest { Token = "abc" });
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CheckInByQr_Returns500_WhenException()
        {
            SetupEventBelongsToClub(1);
            _mockAttendanceService.Setup(s => s.CheckInByQrTokenAsync(1, "abc")).ThrowsAsync(new Exception("err"));
            var result = await _controller.CheckInByQr(ClubId, 1, new CheckInByQrRequest { Token = "abc" });
            result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CheckInByQr_Returns404_WhenGetEventThrowsNotFound()
        {
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ThrowsAsync(new NotFoundException("Event", 1));
            var result = await _controller.CheckInByQr(ClubId, 1, new CheckInByQrRequest { Token = "abc" });
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CheckInByQr_Returns500_WhenGetEventThrowsGeneric()
        {
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ThrowsAsync(new Exception("err"));
            var result = await _controller.CheckInByQr(ClubId, 1, new CheckInByQrRequest { Token = "abc" });
            result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        #endregion

        #region EvaluateMember

        [Fact]
        public async Task EvaluateMember_ReturnsOk_WhenSuccess()
        {
            SetupEventBelongsToClub(1);
            var request = new EvaluateMemberRequest { EventId = 1, UserId = Guid.NewGuid(), Score = 85 };
            _mockAttendanceService.Setup(s => s.EvaluateMemberAsync(request)).Returns(Task.CompletedTask);
            var result = await _controller.EvaluateMember(ClubId, 1, request);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task EvaluateMember_ReturnsBadRequest_WhenIdMismatch()
        {
            var request = new EvaluateMemberRequest { EventId = 2, UserId = Guid.NewGuid(), Score = 85 };
            var result = await _controller.EvaluateMember(ClubId, 1, request);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task EvaluateMember_ReturnsBadRequest_WhenWrongClub()
        {
            SetupEventBelongsToClub(1, clubId: 999);
            var request = new EvaluateMemberRequest { EventId = 1, UserId = Guid.NewGuid(), Score = 85 };
            var result = await _controller.EvaluateMember(ClubId, 1, request);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task EvaluateMember_ReturnsBadRequest_WhenServiceThrows()
        {
            SetupEventBelongsToClub(1);
            var request = new EvaluateMemberRequest { EventId = 1, UserId = Guid.NewGuid(), Score = 85 };
            _mockAttendanceService.Setup(s => s.EvaluateMemberAsync(It.IsAny<EvaluateMemberRequest>())).ThrowsAsync(new Exception("err"));
            var result = await _controller.EvaluateMember(ClubId, 1, request);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task EvaluateMember_ReturnsBadRequest_WhenGetEventThrows()
        {
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ThrowsAsync(new Exception("err"));
            var request = new EvaluateMemberRequest { EventId = 1, UserId = Guid.NewGuid(), Score = 85 };
            var result = await _controller.EvaluateMember(ClubId, 1, request);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region GetEventAttendees

        [Fact]
        public async Task GetEventAttendees_ReturnsOk_WhenSuccess()
        {
            SetupEventBelongsToClub(1);
            var attendees = new List<AttendanceDetailDto>
            {
                new AttendanceDetailDto { AttendId = 1, MemberName = "User 1" }
            };
            _mockAttendanceService.Setup(s => s.GetEventAttendeesAsync(1)).ReturnsAsync(attendees);
            var result = await _controller.GetEventAttendees(ClubId, 1);
            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetEventAttendees_ReturnsBadRequest_WhenWrongClub()
        {
            SetupEventBelongsToClub(1, clubId: 999);
            var result = await _controller.GetEventAttendees(ClubId, 1);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetEventAttendees_Returns404_WhenNotFound()
        {
            SetupEventBelongsToClub(1);
            _mockAttendanceService.Setup(s => s.GetEventAttendeesAsync(1)).ThrowsAsync(new NotFoundException("Event", 1));
            var result = await _controller.GetEventAttendees(ClubId, 1);
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetEventAttendees_Returns500_WhenException()
        {
            SetupEventBelongsToClub(1);
            _mockAttendanceService.Setup(s => s.GetEventAttendeesAsync(1)).ThrowsAsync(new Exception("err"));
            var result = await _controller.GetEventAttendees(ClubId, 1);
            result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetEventAttendees_Returns404_WhenGetEventThrowsNotFound()
        {
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ThrowsAsync(new NotFoundException("Event", 1));
            var result = await _controller.GetEventAttendees(ClubId, 1);
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetEventAttendees_Returns500_WhenGetEventThrowsGeneric()
        {
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ThrowsAsync(new Exception("err"));
            var result = await _controller.GetEventAttendees(ClubId, 1);
            result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        #endregion
    }
}
