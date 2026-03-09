using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UNIC.Presentation.Controllers;
using Xunit;

namespace UNIC.Presentation.Test.Controllers
{
    public class AttendanceControllerTest
    {
        private readonly Mock<IAttendanceService> _mockService;
        private readonly AttendanceController _controller;

        public AttendanceControllerTest()
        {
            _mockService = new Mock<IAttendanceService>();
            _controller = new AttendanceController(_mockService.Object);
        }

        private static IActionResult Unwrap<T>(ActionResult<T> result) => result.Result ?? (IActionResult)new OkObjectResult(result.Value);

        #region RegisterMember

        [Fact]
        public async Task RegisterMember_ReturnsOk_WhenSuccess()
        {
            var request = new EventRegistrationRequest { EventId = 1 };
            _mockService.Setup(s => s.RegisterMemberAsync(request)).Returns(Task.CompletedTask);

            var result = await _controller.RegisterMember(1, request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task RegisterMember_ReturnsBadRequest_WhenIdMismatch()
        {
            var request = new EventRegistrationRequest { EventId = 2 };

            var result = await _controller.RegisterMember(1, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RegisterMember_ReturnsNotFound_WhenNotFound()
        {
            var request = new EventRegistrationRequest { EventId = 1 };
            _mockService.Setup(s => s.RegisterMemberAsync(request))
                .ThrowsAsync(new NotFoundException("Event not found"));

            var result = await _controller.RegisterMember(1, request);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task RegisterMember_ReturnsConflict_WhenAlreadyRegistered()
        {
            var request = new EventRegistrationRequest { EventId = 1 };
            _mockService.Setup(s => s.RegisterMemberAsync(request))
                .ThrowsAsync(new ConflictException("Already registered"));

            var result = await _controller.RegisterMember(1, request);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task RegisterMember_Returns500_WhenUnexpected()
        {
            var request = new EventRegistrationRequest { EventId = 1 };
            _mockService.Setup(s => s.RegisterMemberAsync(request))
                .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.RegisterMember(1, request);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        #endregion

        #region GenerateCheckInCode

        [Fact]
        public async Task GenerateCheckInCode_ReturnsOk_WhenSuccess()
        {
            _mockService.Setup(s => s.GenerateCheckInCodeAsync(1))
                .ReturnsAsync(new CheckInCodeResponse { Code = "ABC123" });

            var result = await _controller.GenerateCheckInCode(1);

            Assert.IsType<OkObjectResult>(Unwrap(result));
        }

        [Fact]
        public async Task GenerateCheckInCode_ReturnsNotFound_WhenEventNotFound()
        {
            _mockService.Setup(s => s.GenerateCheckInCodeAsync(99))
                .ThrowsAsync(new NotFoundException("Event not found"));

            var result = await _controller.GenerateCheckInCode(99);

            Assert.IsType<NotFoundObjectResult>(Unwrap(result));
        }

        #endregion

        #region CheckIn

        [Fact]
        public async Task CheckIn_ReturnsOk_WhenSuccess()
        {
            var request = new CheckInRequest { EventId = 1, Code = "ABC" };
            _mockService.Setup(s => s.CheckInMemberAsync(request)).Returns(Task.CompletedTask);

            var result = await _controller.CheckIn(1, request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task CheckIn_ReturnsBadRequest_WhenIdMismatch()
        {
            var request = new CheckInRequest { EventId = 2 };

            var result = await _controller.CheckIn(1, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CheckIn_ReturnsNotFound_WhenNotFound()
        {
            var request = new CheckInRequest { EventId = 1 };
            _mockService.Setup(s => s.CheckInMemberAsync(request))
                .ThrowsAsync(new NotFoundException("Not found"));

            var result = await _controller.CheckIn(1, request);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region EvaluateMember

        [Fact]
        public async Task EvaluateMember_ReturnsOk_WhenSuccess()
        {
            var request = new EvaluateMemberRequest { EventId = 1 };
            _mockService.Setup(s => s.EvaluateMemberAsync(request)).Returns(Task.CompletedTask);

            var result = await _controller.EvaluateMember(1, request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task EvaluateMember_ReturnsBadRequest_WhenIdMismatch()
        {
            var request = new EvaluateMemberRequest { EventId = 2 };

            var result = await _controller.EvaluateMember(1, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region GetEventAttendees

        [Fact]
        public async Task GetEventAttendees_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetEventAttendeesAsync(1))
                .ReturnsAsync(new List<AttendanceDetailDto> { new() });

            var result = await _controller.GetEventAttendees(1);

            Assert.IsType<OkObjectResult>(Unwrap(result));
        }

        [Fact]
        public async Task GetEventAttendees_ReturnsNotFound_WhenNotFound()
        {
            _mockService.Setup(s => s.GetEventAttendeesAsync(99))
                .ThrowsAsync(new NotFoundException("Event not found"));

            var result = await _controller.GetEventAttendees(99);

            Assert.IsType<NotFoundObjectResult>(Unwrap(result));
        }

        #endregion
    }
}
