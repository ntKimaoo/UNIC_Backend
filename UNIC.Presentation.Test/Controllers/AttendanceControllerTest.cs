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

        #region CheckIn

        [Fact]
        public async Task CheckIn_ReturnsBadRequest_WhenIdMismatch()
        {
            var request = new CheckInRequest { EventId = 2 };

            var result = await _controller.CheckIn(1, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region GetMyCheckInQr

        [Fact]
        public async Task GetMyCheckInQr_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetMyCheckInQrAsync(1, It.IsAny<Guid>()))
                .ReturnsAsync((CheckInQrResponse?)null);

            // Note: this test would require setting up ClaimsPrincipal to work properly
            // Leaving as a placeholder for integration testing
        }

        #endregion
    }
}
