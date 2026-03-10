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
        private readonly Mock<IFileStorageService> _mockFileStorage;
        private readonly EventsController _controller;

        public EventsControllerTest()
        {
            _mockEventService = new Mock<IEventService>();
            _mockFileStorage = new Mock<IFileStorageService>();
            _controller = new EventsController(_mockEventService.Object, _mockFileStorage.Object);
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

        #region CreateEvent

        [Fact]
        public async Task CreateEvent_ReturnsCreated_WithNoImage()
        {
            var request = new CreateEventRequest { EventName = "Test", ClubId = 1, Description = "desc", StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            _mockEventService.Setup(s => s.CreateEventAsync(request, null))
                .ReturnsAsync(new EventDetailDto { EventId = 5 });

            var result = await _controller.CreateEvent(request, null);

            Assert.IsType<CreatedAtActionResult>(Unwrap(result));
        }

        [Fact]
        public async Task CreateEvent_ReturnsCreated_WithImage()
        {
            var request = new CreateEventRequest { EventName = "Test", ClubId = 1, Description = "desc", StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(100);
            _mockFileStorage.Setup(s => s.SaveFileAsync(mockFile.Object, "uniclub/events"))
                .ReturnsAsync("https://cdn.example.com/img.jpg");
            _mockEventService.Setup(s => s.CreateEventAsync(request, "https://cdn.example.com/img.jpg"))
                .ReturnsAsync(new EventDetailDto { EventId = 5 });

            var result = await _controller.CreateEvent(request, mockFile.Object);

            Assert.IsType<CreatedAtActionResult>(Unwrap(result));
        }

        [Fact]
        public async Task CreateEvent_ReturnsBadRequest_WhenInvalidOperation()
        {
            var request = new CreateEventRequest { Description = "x", EventName = "x", StartDate = DateTime.Now, EndDate = DateTime.Now };
            _mockEventService.Setup(s => s.CreateEventAsync(request, null))
                .ThrowsAsync(new InvalidOperationException("Club not found"));

            var result = await _controller.CreateEvent(request, null);

            Assert.IsType<BadRequestObjectResult>(Unwrap(result));
        }

        [Fact]
        public async Task CreateEvent_Returns500_WhenUnexpected()
        {
            var request = new CreateEventRequest { Description = "x", EventName = "x", StartDate = DateTime.Now, EndDate = DateTime.Now };
            _mockEventService.Setup(s => s.CreateEventAsync(request, null))
                .ThrowsAsync(new Exception("Unexpected"));

            var result = await _controller.CreateEvent(request, null);

            var obj = Assert.IsType<ObjectResult>(Unwrap(result));
            Assert.Equal(500, obj.StatusCode);
        }

        #endregion

        #region UpdateEvent

        [Fact]
        public async Task UpdateEvent_ReturnsOk_WhenSuccess()
        {
            var request = new UpdateEventRequest { EventId = 1, EventName = "Updated", Description = "d" };
            _mockEventService.Setup(s => s.UpdateEventAsync(request))
                .ReturnsAsync(new EventDetailDto { EventId = 1 });

            var result = await _controller.UpdateEvent(1, request, null);

            Assert.IsType<OkObjectResult>(Unwrap(result));
        }

        [Fact]
        public async Task UpdateEvent_ReturnsBadRequest_WhenIdMismatch()
        {
            var request = new UpdateEventRequest { EventId = 2, EventName = "x", Description = "x" };

            var result = await _controller.UpdateEvent(1, request, null);

            Assert.IsType<BadRequestObjectResult>(Unwrap(result));
        }

        [Fact]
        public async Task UpdateEvent_ReturnsNotFound_WhenNotFound()
        {
            var request = new UpdateEventRequest { EventId = 1, EventName = "x", Description = "x" };
            _mockEventService.Setup(s => s.UpdateEventAsync(request))
                .ThrowsAsync(new NotFoundException("Event not found"));

            var result = await _controller.UpdateEvent(1, request, null);

            Assert.IsType<NotFoundObjectResult>(Unwrap(result));
        }

        #endregion

        #region CreateSession

        [Fact]
        public async Task CreateSession_ReturnsCreated_WhenSuccess()
        {
            var request = new CreateSessionRequest { EventId = 1, SessionName = "S1", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(2) };
            _mockEventService.Setup(s => s.CreateSessionAsync(request))
                .ReturnsAsync(new SessionDto { ScheduleId = 1 });

            var result = await _controller.CreateSession(1, request);

            Assert.IsType<CreatedAtActionResult>(Unwrap(result));
        }

        [Fact]
        public async Task CreateSession_ReturnsBadRequest_WhenIdMismatch()
        {
            var request = new CreateSessionRequest { EventId = 2, SessionName = "x", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(1) };

            var result = await _controller.CreateSession(1, request);

            Assert.IsType<BadRequestObjectResult>(Unwrap(result));
        }

        [Fact]
        public async Task CreateSession_ReturnsNotFound_WhenEventNotFound()
        {
            var request = new CreateSessionRequest { EventId = 1, SessionName = "x", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(1) };
            _mockEventService.Setup(s => s.CreateSessionAsync(request))
                .ThrowsAsync(new NotFoundException("Event not found"));

            var result = await _controller.CreateSession(1, request);

            Assert.IsType<NotFoundObjectResult>(Unwrap(result));
        }

        #endregion

        #region OpenRegistration

        [Fact]
        public async Task OpenRegistration_ReturnsOk_WhenSuccess()
        {
            var request = new OpenRegistrationRequest { EventId = 1, RegistrationStartDate = DateTime.Now, RegistrationEndDate = DateTime.Now.AddDays(7) };
            _mockEventService.Setup(s => s.OpenRegistrationAsync(request))
                .ReturnsAsync(new EventDetailDto { EventId = 1 });

            var result = await _controller.OpenRegistration(1, request);

            Assert.IsType<OkObjectResult>(Unwrap(result));
        }

        [Fact]
        public async Task OpenRegistration_ReturnsBadRequest_WhenIdMismatch()
        {
            var request = new OpenRegistrationRequest { EventId = 2, RegistrationStartDate = DateTime.Now, RegistrationEndDate = DateTime.Now.AddDays(7) };

            var result = await _controller.OpenRegistration(1, request);

            Assert.IsType<BadRequestObjectResult>(Unwrap(result));
        }

        #endregion
    }
}
