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
    public class ClubEventsControllerTest
    {
        private readonly Mock<IEventService> _mockEventService;
        private readonly Mock<IFileStorageService> _mockFileStorageService;
        private readonly Mock<IEventPermissionService> _mockEventPermService;
        private readonly ClubEventsController _controller;
        private const int ClubId = 1;

        public ClubEventsControllerTest()
        {
            _mockEventService = new Mock<IEventService>();
            _mockFileStorageService = new Mock<IFileStorageService>();
            _mockEventPermService = new Mock<IEventPermissionService>();
            _controller = new ClubEventsController(
                _mockEventService.Object,
                _mockFileStorageService.Object,
                _mockEventPermService.Object);
        }

        /// <summary>
        /// Sets up the controller with a ClaimsPrincipal that has Manager role for the given clubId.
        /// </summary>
        private void SetupManagerClaims(int clubId)
        {
            var clubRoles = JsonSerializer.Serialize(new[]
            {
                new { ClubId = clubId, RoleName = "Manager", Level = 1 }
            });
            var claims = new List<Claim>
            {
                new Claim("club_roles", clubRoles),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        /// <summary>
        /// Sets up the controller with NO club roles (non-manager user).
        /// </summary>
        private void SetupNonManagerClaims()
        {
            var identity = new ClaimsIdentity(new List<Claim>(), "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        #region CreateEvent

        [Fact]
        public async Task CreateEvent_ReturnsCreated_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            var request = new CreateEventRequest
            {
                EventName = "New Event",
                Description = "Desc",
                StartDate = DateTime.Now.AddDays(7),
                EndDate = DateTime.Now.AddDays(8)
            };
            var dto = new EventDetailDto { EventId = 1, EventName = "New Event" };

            _mockEventService.Setup(s => s.CreateEventAsync(It.IsAny<CreateEventRequest>(), null))
                .ReturnsAsync(dto);

            var result = await _controller.CreateEvent(ClubId, request, null);

            result.Result.Should().BeOfType<CreatedAtActionResult>();
        }

        [Fact]
        public async Task CreateEvent_Returns403_WhenNotManager()
        {
            SetupNonManagerClaims();
            var request = new CreateEventRequest { EventName = "Test", Description = "D" };

            var result = await _controller.CreateEvent(ClubId, request, null);

            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task CreateEvent_ReturnsBadRequest_WhenDomainException()
        {
            SetupManagerClaims(ClubId);
            var request = new CreateEventRequest { EventName = "Bad", Description = "D" };

            _mockEventService.Setup(s => s.CreateEventAsync(It.IsAny<CreateEventRequest>(), null))
                .ThrowsAsync(new DomainException("Validation failed"));

            var result = await _controller.CreateEvent(ClubId, request, null);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateEvent_Returns500_WhenUnexpectedException()
        {
            SetupManagerClaims(ClubId);
            var request = new CreateEventRequest { EventName = "Error", Description = "D" };

            _mockEventService.Setup(s => s.CreateEventAsync(It.IsAny<CreateEventRequest>(), null))
                .ThrowsAsync(new Exception("Database error"));

            var result = await _controller.CreateEvent(ClubId, request, null);

            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region UpdateEvent

        [Fact]
        public async Task UpdateEvent_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            var request = new UpdateEventRequest
            {
                EventId = 1,
                EventName = "Updated",
                Description = "Desc",
                Location = "Room A"
            };
            var existingDto = new EventDetailDto { EventId = 1, ClubId = ClubId, EventName = "Old" };
            var updatedDto = new EventDetailDto { EventId = 1, EventName = "Updated" };

            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(existingDto);
            _mockEventService.Setup(s => s.UpdateEventAsync(request)).ReturnsAsync(updatedDto);

            var result = await _controller.UpdateEvent(ClubId, 1, request, null);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateEvent_Returns403_WhenNotManager()
        {
            SetupNonManagerClaims();
            var request = new UpdateEventRequest { EventId = 1, EventName = "X", Description = "X" };

            var result = await _controller.UpdateEvent(ClubId, 1, request, null);

            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task UpdateEvent_ReturnsBadRequest_WhenIdMismatch()
        {
            SetupManagerClaims(ClubId);
            var request = new UpdateEventRequest { EventId = 2, EventName = "X", Description = "X" };

            var result = await _controller.UpdateEvent(ClubId, 1, request, null);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateEvent_ReturnsBadRequest_WhenWrongClub()
        {
            SetupManagerClaims(ClubId);
            var request = new UpdateEventRequest { EventId = 1, EventName = "X", Description = "X" };
            var existingDto = new EventDetailDto { EventId = 1, ClubId = 999 }; // wrong club

            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(existingDto);

            var result = await _controller.UpdateEvent(ClubId, 1, request, null);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region CreateSession

        [Fact]
        public async Task CreateSession_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            var request = new CreateSessionRequest
            {
                EventId = 1,
                SessionName = "Opening",
                StartTime = DateTime.Now.AddDays(7),
                EndTime = DateTime.Now.AddDays(7).AddHours(1)
            };
            var existingDto = new EventDetailDto { EventId = 1, ClubId = ClubId };
            var sessionDto = new SessionDto { ScheduleId = 10, ScheduleName = "Opening" };

            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(existingDto);
            _mockEventService.Setup(s => s.CreateSessionAsync(request)).ReturnsAsync(sessionDto);

            var result = await _controller.CreateSession(ClubId, 1, request);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CreateSession_Returns403_WhenNotManager()
        {
            SetupNonManagerClaims();
            var request = new CreateSessionRequest { EventId = 1, SessionName = "X" };

            var result = await _controller.CreateSession(ClubId, 1, request);

            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task CreateSession_ReturnsBadRequest_WhenIdMismatch()
        {
            SetupManagerClaims(ClubId);
            var request = new CreateSessionRequest { EventId = 2, SessionName = "X" };

            var result = await _controller.CreateSession(ClubId, 1, request);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region UpdateSession

        [Fact]
        public async Task UpdateSession_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            var request = new UpdateSessionRequest { SessionName = "Updated" };
            var existingDto = new EventDetailDto { EventId = 1, ClubId = ClubId };
            var sessionDto = new SessionDto { ScheduleId = 10, ScheduleName = "Updated" };

            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(existingDto);
            _mockEventService.Setup(s => s.UpdateSessionAsync(It.IsAny<UpdateSessionRequest>())).ReturnsAsync(sessionDto);

            var result = await _controller.UpdateSession(ClubId, 1, 10, request);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateSession_Returns404_WhenNotFoundException()
        {
            SetupManagerClaims(ClubId);
            var request = new UpdateSessionRequest { SessionName = "X" };
            var existingDto = new EventDetailDto { EventId = 1, ClubId = ClubId };

            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(existingDto);
            _mockEventService.Setup(s => s.UpdateSessionAsync(It.IsAny<UpdateSessionRequest>()))
                .ThrowsAsync(new NotFoundException("Session", 10));

            var result = await _controller.UpdateSession(ClubId, 1, 10, request);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateSession_Returns403_WhenNotManager()
        {
            SetupNonManagerClaims();
            var request = new UpdateSessionRequest { SessionName = "X" };

            var result = await _controller.UpdateSession(ClubId, 1, 10, request);

            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(403);
        }

        #endregion

        #region DeleteSession

        [Fact]
        public async Task DeleteSession_ReturnsNoContent_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            var existingDto = new EventDetailDto { EventId = 1, ClubId = ClubId };

            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(existingDto);
            _mockEventService.Setup(s => s.DeleteSessionAsync(10, 1)).Returns(Task.CompletedTask);

            var result = await _controller.DeleteSession(ClubId, 1, 10);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteSession_Returns404_WhenNotFoundException()
        {
            SetupManagerClaims(ClubId);
            var existingDto = new EventDetailDto { EventId = 1, ClubId = ClubId };

            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(existingDto);
            _mockEventService.Setup(s => s.DeleteSessionAsync(999, 1))
                .ThrowsAsync(new NotFoundException("Session", 999));

            var result = await _controller.DeleteSession(ClubId, 1, 999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteSession_Returns403_WhenNotManager()
        {
            SetupNonManagerClaims();

            var result = await _controller.DeleteSession(ClubId, 1, 10);

            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(403);
        }

        #endregion

        #region OpenRegistration

        [Fact]
        public async Task OpenRegistration_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            var request = new OpenRegistrationRequest
            {
                EventId = 1,
                RegistrationStartDate = DateTime.Now,
                RegistrationEndDate = DateTime.Now.AddDays(3)
            };
            var existingDto = new EventDetailDto { EventId = 1, ClubId = ClubId };
            var resultDto = new EventDetailDto { EventId = 1, Status = "REGISTRATION_OPEN" };

            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(existingDto);
            _mockEventService.Setup(s => s.OpenRegistrationAsync(request)).ReturnsAsync(resultDto);

            var result = await _controller.OpenRegistration(ClubId, 1, request);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task OpenRegistration_Returns403_WhenNotManager()
        {
            SetupNonManagerClaims();
            var request = new OpenRegistrationRequest { EventId = 1 };

            var result = await _controller.OpenRegistration(ClubId, 1, request);

            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task OpenRegistration_ReturnsBadRequest_WhenIdMismatch()
        {
            SetupManagerClaims(ClubId);
            var request = new OpenRegistrationRequest { EventId = 2 };

            var result = await _controller.OpenRegistration(ClubId, 1, request);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region StartEvent

        [Fact]
        public async Task StartEvent_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            var existingDto = new EventDetailDto { EventId = 1, ClubId = ClubId };

            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(existingDto);
            _mockEventService.Setup(s => s.StartEventAsync(1))
                .ReturnsAsync(("ABC123", DateTime.Now.AddHours(2)));

            var result = await _controller.StartEvent(ClubId, 1);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task StartEvent_Returns403_WhenNotManager()
        {
            SetupNonManagerClaims();

            var result = await _controller.StartEvent(ClubId, 1);

            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(403);
        }

        #endregion

        #region CompleteEvent

        [Fact]
        public async Task CompleteEvent_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            var existingDto = new EventDetailDto { EventId = 1, ClubId = ClubId };

            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(existingDto);
            _mockEventService.Setup(s => s.CompleteEventAsync(1)).Returns(Task.CompletedTask);

            var result = await _controller.CompleteEvent(ClubId, 1);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CompleteEvent_Returns403_WhenNotManager()
        {
            SetupNonManagerClaims();

            var result = await _controller.CompleteEvent(ClubId, 1);

            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(403);
        }

        #endregion

        #region GetSessions

        [Fact]
        public async Task GetSessions_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            var existingDto = new EventDetailDto
            {
                EventId = 1,
                ClubId = ClubId,
                Sessions = new List<SessionDto>
                {
                    new SessionDto { ScheduleId = 1, ScheduleName = "Session 1" }
                }
            };

            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(existingDto);

            var result = await _controller.GetSessions(ClubId, 1);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetSessions_Returns403_WhenNotManager()
        {
            SetupNonManagerClaims();

            var result = await _controller.GetSessions(ClubId, 1);

            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GetSessions_ReturnsBadRequest_WhenWrongClub()
        {
            SetupManagerClaims(ClubId);
            var existingDto = new EventDetailDto { EventId = 1, ClubId = 999 }; // wrong club

            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(existingDto);

            var result = await _controller.GetSessions(ClubId, 1);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion
    }
}
