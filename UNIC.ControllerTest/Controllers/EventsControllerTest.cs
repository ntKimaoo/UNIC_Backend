//using BusinessLogic.DTOs;
//using BusinessLogic.Exceptions;
//using BusinessLogic.Services.Interface;
//using FluentAssertions;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Moq;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using UNIC.Presentation.Controllers;
//using Xunit;

//namespace UNIC.ControllerTest.Controllers
//{
//    public class EventsControllerTest
//    {
//        private readonly Mock<IEventService> _mockEventService;
//        private readonly Mock<IQRCodeGeneratorService> _mockQrService;
//        private readonly EventsController _controller;

//        public EventsControllerTest()
//        {
//            _mockEventService = new Mock<IEventService>();
//            _mockQrService = new Mock<IQRCodeGeneratorService>();
//            _controller = new EventsController(_mockEventService.Object, _mockQrService.Object);

//            _controller.ControllerContext = new ControllerContext
//            {
//                HttpContext = new DefaultHttpContext()
//            };
//        }

//        #region GetEventById

//        [Fact]
//        public async Task GetEventById_ReturnsOk_WhenFound()
//        {
//            var dto = new EventDetailDto { EventId = 1, EventName = "Test Event" };
//            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(dto);

//            var result = await _controller.GetEventById(1);

//            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
//            var value = okResult.Value.Should().BeOfType<EventDetailDto>().Subject;
//            value.EventId.Should().Be(1);
//        }

//        [Fact]
//        public async Task GetEventById_ReturnsNotFound_WhenNotFoundException()
//        {
//            _mockEventService.Setup(s => s.GetEventByIdAsync(999))
//                .ThrowsAsync(new NotFoundException("Event", 999));

//            var result = await _controller.GetEventById(999);

//            result.Result.Should().BeOfType<NotFoundObjectResult>();
//        }

//        [Fact]
//        public async Task GetEventById_Returns500_WhenUnexpectedException()
//        {
//            _mockEventService.Setup(s => s.GetEventByIdAsync(1))
//                .ThrowsAsync(new Exception("Database error"));

//            var result = await _controller.GetEventById(1);

//            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
//            statusResult.StatusCode.Should().Be(500);
//        }

//        #endregion

//        #region GetAllEvents

//        [Fact]
//        public async Task GetAllEvents_ReturnsOk_WithPagination()
//        {
//            var events = new List<EventDetailDto>
//            {
//                new EventDetailDto { EventId = 1, EventName = "Event 1" }
//            };
//            _mockEventService.Setup(s => s.GetAllEventsAsync(1, 10)).ReturnsAsync(events);

//            var result = await _controller.GetAllEvents(1, 10);

//            result.Result.Should().BeOfType<OkObjectResult>();
//        }

//        [Fact]
//        public async Task GetAllEvents_ReturnsBadRequest_WhenPageNumberLessThan1()
//        {
//            var result = await _controller.GetAllEvents(0, 10);
//            result.Result.Should().BeOfType<BadRequestObjectResult>();
//        }

//        [Fact]
//        public async Task GetAllEvents_ReturnsBadRequest_WhenPageSizeGreaterThan100()
//        {
//            var result = await _controller.GetAllEvents(1, 101);
//            result.Result.Should().BeOfType<BadRequestObjectResult>();
//        }

//        [Fact]
//        public async Task GetAllEvents_ReturnsBadRequest_WhenPageSizeLessThan1()
//        {
//            var result = await _controller.GetAllEvents(1, 0);
//            result.Result.Should().BeOfType<BadRequestObjectResult>();
//        }

//        #endregion

//        #region GetQrCodeImage

//        [Fact]
//        public async Task GetQrCodeImage_ReturnsFile_WhenValid()
//        {
//            var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
//            _mockQrService.Setup(s => s.GetQrCodePngBytes("valid-token")).Returns(pngBytes);

//            var result = _controller.GetQrCodeImage("valid-token");

//            result.Should().BeOfType<FileContentResult>();
//            var fileResult = (FileContentResult)result;
//            fileResult.ContentType.Should().Be("image/png");
//        }

//        [Fact]
//        public async Task GetQrCodeImage_ReturnsBadRequest_WhenEmptyToken()
//        {
//            var result = _controller.GetQrCodeImage("  ");
//            result.Should().BeOfType<BadRequestResult>();
//        }

//        [Fact]
//        public async Task GetQrCodeImage_ReturnsNotFound_WhenNullBytes()
//        {
//            _mockQrService.Setup(s => s.GetQrCodePngBytes("unknown")).Returns((byte[]?)null);

//            var result = _controller.GetQrCodeImage("unknown");
//            result.Should().BeOfType<NotFoundResult>();
//        }

//        #endregion
//    }
//}
