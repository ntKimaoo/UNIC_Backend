//using BusinessLogic.DTOs;
//using BusinessLogic.Exceptions;
//using BusinessLogic.Services.Interface;
//using FluentAssertions;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Moq;
//using System;
//using System.Collections.Generic;
//using System.Security.Claims;
//using System.Text.Json;
//using System.Threading.Tasks;
//using UNIC.Presentation.Controllers;
//using Xunit;

//namespace UNIC.ControllerTest.Controllers
//{
//    public class ClubAttendanceControllerTest
//    {
//        private readonly Mock<IAttendanceService> _mockAttendanceService;
//        private readonly Mock<IEventService> _mockEventService;
//        private readonly ClubAttendanceController _controller;
//        private const int ClubId = 1;

//        public ClubAttendanceControllerTest()
//        {
//            _mockAttendanceService = new Mock<IAttendanceService>();
//            _mockEventService = new Mock<IEventService>();
//            _controller = new ClubAttendanceController(_mockAttendanceService.Object, _mockEventService.Object);
//        }

//        private void SetupManagerClaims(int clubId)
//        {
//            var clubRoles = JsonSerializer.Serialize(new[]
//            {
//                new { ClubId = clubId, RoleName = "Manager", Level = 1 }
//            });
//            var claims = new List<Claim> { new Claim("club_roles", clubRoles) };
//            var identity = new ClaimsIdentity(claims, "TestAuth");
//            _controller.ControllerContext = new ControllerContext
//            {
//                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
//            };
//        }

//        private void SetupNonManagerClaims()
//        {
//            var identity = new ClaimsIdentity(new List<Claim>(), "TestAuth");
//            _controller.ControllerContext = new ControllerContext
//            {
//                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
//            };
//        }

//        /// <summary>
//        /// Helper: sets up GetEventByIdAsync to return an event belonging to ClubId.
//        /// </summary>
//        private void SetupEventBelongsToClub(int eventId, int clubId = ClubId)
//        {
//            _mockEventService.Setup(s => s.GetEventByIdAsync(eventId))
//                .ReturnsAsync(new EventDetailDto { EventId = eventId, ClubId = clubId });
//        }

//        #region ApproveRegistration

//        [Fact]
//        public async Task ApproveRegistration_ReturnsOk_WhenSuccess()
//        {
//            SetupManagerClaims(ClubId);
//            SetupEventBelongsToClub(1);
//            var userId = Guid.NewGuid();
//            _mockAttendanceService.Setup(s => s.ApproveRegistrationAsync(1, userId)).Returns(Task.CompletedTask);

//            var result = await _controller.ApproveRegistration(ClubId, 1, userId);

//            result.Should().BeOfType<OkObjectResult>();
//        }

//        [Fact]
//        public async Task ApproveRegistration_Returns403_WhenNotManager()
//        {
//            SetupNonManagerClaims();

//            var result = await _controller.ApproveRegistration(ClubId, 1, Guid.NewGuid());

//            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
//            statusResult.StatusCode.Should().Be(403);
//        }

//        [Fact]
//        public async Task ApproveRegistration_ReturnsBadRequest_WhenWrongClub()
//        {
//            SetupManagerClaims(ClubId);
//            SetupEventBelongsToClub(1, clubId: 999); // wrong club

//            var result = await _controller.ApproveRegistration(ClubId, 1, Guid.NewGuid());

//            result.Should().BeOfType<BadRequestObjectResult>();
//        }

//        #endregion

//        #region RejectRegistration

//        [Fact]
//        public async Task RejectRegistration_ReturnsOk_WhenSuccess()
//        {
//            SetupManagerClaims(ClubId);
//            SetupEventBelongsToClub(1);
//            var userId = Guid.NewGuid();
//            _mockAttendanceService.Setup(s => s.RejectRegistrationAsync(1, userId)).Returns(Task.CompletedTask);

//            var result = await _controller.RejectRegistration(ClubId, 1, userId);

//            result.Should().BeOfType<OkObjectResult>();
//        }

//        [Fact]
//        public async Task RejectRegistration_Returns403_WhenNotManager()
//        {
//            SetupNonManagerClaims();

//            var result = await _controller.RejectRegistration(ClubId, 1, Guid.NewGuid());

//            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
//            statusResult.StatusCode.Should().Be(403);
//        }

//        #endregion

//        #region BulkApproveRegistrations

//        [Fact]
//        public async Task BulkApproveRegistrations_ReturnsOk_WhenSuccess()
//        {
//            SetupManagerClaims(ClubId);
//            SetupEventBelongsToClub(1);
//            var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

//            _mockAttendanceService.Setup(s => s.BulkApproveAsync(1, userIds)).ReturnsAsync(2);

//            var result = await _controller.BulkApproveRegistrations(ClubId, 1, userIds);

//            result.Should().BeOfType<OkObjectResult>();
//        }

//        [Fact]
//        public async Task BulkApproveRegistrations_Returns403_WhenNotManager()
//        {
//            SetupNonManagerClaims();

//            var result = await _controller.BulkApproveRegistrations(ClubId, 1, new List<Guid>());

//            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
//            statusResult.StatusCode.Should().Be(403);
//        }

//        [Fact]
//        public async Task BulkApproveRegistrations_ReturnsBadRequest_WhenEmptyList()
//        {
//            SetupManagerClaims(ClubId);

//            var result = await _controller.BulkApproveRegistrations(ClubId, 1, new List<Guid>());

//            result.Should().BeOfType<BadRequestObjectResult>();
//        }

//        [Fact]
//        public async Task BulkApproveRegistrations_ReturnsBadRequest_WhenNullList()
//        {
//            SetupManagerClaims(ClubId);

//            var result = await _controller.BulkApproveRegistrations(ClubId, 1, null!);

//            result.Should().BeOfType<BadRequestObjectResult>();
//        }

//        #endregion

//        #region GenerateCheckInCode

//        [Fact]
//        public async Task GenerateCheckInCode_ReturnsOk_WhenSuccess()
//        {
//            SetupManagerClaims(ClubId);
//            SetupEventBelongsToClub(1);
//            var response = new CheckInCodeResponse { EventId = 1, Code = "ABC123" };

//            _mockAttendanceService.Setup(s => s.GenerateCheckInCodeAsync(1)).ReturnsAsync(response);

//            var result = await _controller.GenerateCheckInCode(ClubId, 1);

//            result.Result.Should().BeOfType<OkObjectResult>();
//        }

//        [Fact]
//        public async Task GenerateCheckInCode_Returns403_WhenNotManager()
//        {
//            SetupNonManagerClaims();

//            var result = await _controller.GenerateCheckInCode(ClubId, 1);

//            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
//            statusResult.StatusCode.Should().Be(403);
//        }

//        [Fact]
//        public async Task GenerateCheckInCode_Returns404_WhenNotFoundException()
//        {
//            SetupManagerClaims(ClubId);
//            SetupEventBelongsToClub(1);

//            _mockAttendanceService.Setup(s => s.GenerateCheckInCodeAsync(1))
//                .ThrowsAsync(new NotFoundException("Event", 1));

//            var result = await _controller.GenerateCheckInCode(ClubId, 1);

//            result.Result.Should().BeOfType<NotFoundObjectResult>();
//        }

//        #endregion

//        #region CheckInByQr

//        [Fact]
//        public async Task CheckInByQr_ReturnsOk_WhenSuccess()
//        {
//            SetupManagerClaims(ClubId);
//            SetupEventBelongsToClub(1);
//            var request = new CheckInByQrRequest { Token = "token123" };
//            var response = new CheckInByQrResponse { Success = true, MemberName = "Test" };

//            _mockAttendanceService.Setup(s => s.CheckInByQrTokenAsync(1, "token123")).ReturnsAsync(response);

//            var result = await _controller.CheckInByQr(ClubId, 1, request);

//            result.Result.Should().BeOfType<OkObjectResult>();
//        }

//        [Fact]
//        public async Task CheckInByQr_Returns403_WhenNotManager()
//        {
//            SetupNonManagerClaims();
//            var request = new CheckInByQrRequest { Token = "token123" };

//            var result = await _controller.CheckInByQr(ClubId, 1, request);

//            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
//            statusResult.StatusCode.Should().Be(403);
//        }

//        [Fact]
//        public async Task CheckInByQr_ReturnsBadRequest_WhenEmptyToken()
//        {
//            SetupManagerClaims(ClubId);
//            SetupEventBelongsToClub(1);
//            var request = new CheckInByQrRequest { Token = "  " };

//            var result = await _controller.CheckInByQr(ClubId, 1, request);

//            result.Result.Should().BeOfType<BadRequestObjectResult>();
//        }

//        [Fact]
//        public async Task CheckInByQr_ReturnsNotFound_WhenNotFoundException()
//        {
//            SetupManagerClaims(ClubId);
//            SetupEventBelongsToClub(1);
//            var request = new CheckInByQrRequest { Token = "invalid" };

//            _mockAttendanceService.Setup(s => s.CheckInByQrTokenAsync(1, "invalid"))
//                .ThrowsAsync(new NotFoundException("Attendance", "invalid"));

//            var result = await _controller.CheckInByQr(ClubId, 1, request);

//            result.Result.Should().BeOfType<NotFoundObjectResult>();
//        }

//        #endregion

//        #region EvaluateMember

//        [Fact]
//        public async Task EvaluateMember_ReturnsOk_WhenSuccess()
//        {
//            SetupManagerClaims(ClubId);
//            SetupEventBelongsToClub(1);
//            var request = new EvaluateMemberRequest { EventId = 1, UserId = Guid.NewGuid(), Score = 85 };

//            _mockAttendanceService.Setup(s => s.EvaluateMemberAsync(request)).Returns(Task.CompletedTask);

//            var result = await _controller.EvaluateMember(ClubId, 1, request);

//            result.Should().BeOfType<OkObjectResult>();
//        }

//        [Fact]
//        public async Task EvaluateMember_Returns403_WhenNotManager()
//        {
//            SetupNonManagerClaims();
//            var request = new EvaluateMemberRequest { EventId = 1, UserId = Guid.NewGuid(), Score = 85 };

//            var result = await _controller.EvaluateMember(ClubId, 1, request);

//            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
//            statusResult.StatusCode.Should().Be(403);
//        }

//        [Fact]
//        public async Task EvaluateMember_ReturnsBadRequest_WhenIdMismatch()
//        {
//            SetupManagerClaims(ClubId);
//            var request = new EvaluateMemberRequest { EventId = 2, UserId = Guid.NewGuid(), Score = 85 };

//            var result = await _controller.EvaluateMember(ClubId, 1, request);

//            result.Should().BeOfType<BadRequestObjectResult>();
//        }

//        #endregion

//        #region GetEventAttendees

//        [Fact]
//        public async Task GetEventAttendees_ReturnsOk_WhenSuccess()
//        {
//            SetupManagerClaims(ClubId);
//            SetupEventBelongsToClub(1);
//            var attendees = new List<AttendanceDetailDto>
//            {
//                new AttendanceDetailDto { AttendId = 1, MemberName = "User 1" }
//            };

//            _mockAttendanceService.Setup(s => s.GetEventAttendeesAsync(1)).ReturnsAsync(attendees);

//            var result = await _controller.GetEventAttendees(ClubId, 1);

//            result.Result.Should().BeOfType<OkObjectResult>();
//        }

//        [Fact]
//        public async Task GetEventAttendees_Returns403_WhenNotManager()
//        {
//            SetupNonManagerClaims();

//            var result = await _controller.GetEventAttendees(ClubId, 1);

//            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
//            statusResult.StatusCode.Should().Be(403);
//        }

//        [Fact]
//        public async Task GetEventAttendees_Returns404_WhenNotFoundException()
//        {
//            SetupManagerClaims(ClubId);
//            SetupEventBelongsToClub(1);

//            _mockAttendanceService.Setup(s => s.GetEventAttendeesAsync(1))
//                .ThrowsAsync(new NotFoundException("Event", 1));

//            var result = await _controller.GetEventAttendees(ClubId, 1);

//            result.Result.Should().BeOfType<NotFoundObjectResult>();
//        }

//        #endregion
//    }
//}
