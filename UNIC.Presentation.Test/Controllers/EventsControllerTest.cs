using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UNIC.Presentation.Controllers;
using Xunit;

namespace UNIC.Presentation.Test.Controllers
{
    public class EventsControllerTest
    {
        private readonly Mock<IEventService> _mockEventService;
        private readonly Mock<IQRCodeGeneratorService> _mockQrService;
        private readonly EventsController _controller;

        public EventsControllerTest()
        {
            _mockEventService = new Mock<IEventService>();
            _mockQrService = new Mock<IQRCodeGeneratorService>();
            _controller = new EventsController(_mockEventService.Object, _mockQrService.Object);
        }

        // Helper to unwrap ActionResult<T> → the inner IActionResult
        private static IActionResult Unwrap<T>(ActionResult<T> result) => result.Result ?? result.Value as IActionResult ?? new OkObjectResult(result.Value);

        #region GetAllEvents

        [Fact]
        public async Task GetAllEvents_ReturnsOk_WithValidPagination()
        {
            _mockEventService.Setup(s => s.GetAllEventsAsync(1, 10))
                .ReturnsAsync(new List<EventDetailDto> { new() });

            var result = await _controller.GetAllEvents(1, 10);

            Assert.IsType<OkObjectResult>(Unwrap(result));
        }

        [Fact]
        public async Task GetAllEvents_ReturnsBadRequest_WhenInvalidPageNumber()
        {
            var result = await _controller.GetAllEvents(0, 10);

            Assert.IsType<BadRequestObjectResult>(Unwrap(result));
        }

        [Fact]
        public async Task GetAllEvents_ReturnsBadRequest_WhenPageSizeTooLarge()
        {
            var result = await _controller.GetAllEvents(1, 200);

            Assert.IsType<BadRequestObjectResult>(Unwrap(result));
        }

        #endregion

        #region GetEventById

        [Fact]
        public async Task GetEventById_ReturnsOk_WhenFound()
        {
            _mockEventService.Setup(s => s.GetEventByIdAsync(1))
                .ReturnsAsync(new EventDetailDto { EventId = 1 });

            var result = await _controller.GetEventById(1);

            Assert.IsType<OkObjectResult>(Unwrap(result));
        }

        [Fact]
        public async Task GetEventById_ReturnsNotFound_WhenNotFound()
        {
            _mockEventService.Setup(s => s.GetEventByIdAsync(99))
                .ThrowsAsync(new NotFoundException("Event not found"));

            var result = await _controller.GetEventById(99);

            Assert.IsType<NotFoundObjectResult>(Unwrap(result));
        }

        [Fact]
        public async Task GetEventById_Returns500_WhenUnexpected()
        {
            _mockEventService.Setup(s => s.GetEventByIdAsync(1))
                .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.GetEventById(1);

            var obj = Assert.IsType<ObjectResult>(Unwrap(result));
            Assert.Equal(500, obj.StatusCode);
        }

        #endregion

        #region GetQrCodeImage

        [Fact]
        public void GetQrCodeImage_ReturnsBadRequest_WhenTokenEmpty()
        {
            var result = _controller.GetQrCodeImage("");

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public void GetQrCodeImage_ReturnsNotFound_WhenNoPngBytes()
        {
            _mockQrService.Setup(s => s.GetQrCodePngBytes("abc"))
                .Returns(Array.Empty<byte>());

            var result = _controller.GetQrCodeImage("abc");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void GetQrCodeImage_ReturnsFile_WhenSuccess()
        {
            _mockQrService.Setup(s => s.GetQrCodePngBytes("abc"))
                .Returns(new byte[] { 1, 2, 3 });

            var result = _controller.GetQrCodeImage("abc");

            Assert.IsType<FileContentResult>(result);
        }

        #endregion
    }
}
