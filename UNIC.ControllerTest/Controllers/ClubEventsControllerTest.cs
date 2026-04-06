using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IEventRoleRepository> _mockEventRoles;
        private readonly Mock<IUserEventRoleRepository> _mockEventMembers;
        private readonly ClubEventsController _controller;
        private const int ClubId = 1;

        public ClubEventsControllerTest()
        {
            _mockEventService = new Mock<IEventService>();
            _mockFileStorageService = new Mock<IFileStorageService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockEventRoles = new Mock<IEventRoleRepository>();
            _mockEventMembers = new Mock<IUserEventRoleRepository>();

            _mockUnitOfWork.Setup(u => u.EventRoles).Returns(_mockEventRoles.Object);
            _mockUnitOfWork.Setup(u => u.EventMembers).Returns(_mockEventMembers.Object);

            _controller = new ClubEventsController(_mockEventService.Object, _mockFileStorageService.Object, _mockUnitOfWork.Object);
        }

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
            var request = new CreateEventRequest { EventName = "New Event", Description = "Desc", StartDate = DateTime.Now.AddDays(7), EndDate = DateTime.Now.AddDays(8) };
            var createdEvent = new EventDetailDto { EventId = 1, EventName = "New Event" };
            _mockEventService.Setup(s => s.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<string>(), It.IsAny<Guid?>())).ReturnsAsync(createdEvent);
            var result = await _controller.CreateEvent(ClubId, request, null);
            result.Result.Should().BeOfType<CreatedAtActionResult>();
        }

        [Fact]
        public async Task CreateEvent_Returns403_WhenNotManager()
        {
            SetupNonManagerClaims();
            var result = await _controller.CreateEvent(ClubId, new CreateEventRequest { EventName = "T", Description = "D" }, null);
            result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task CreateEvent_ReturnsBadRequest_WhenDomainException()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<string>(), It.IsAny<Guid?>())).ThrowsAsync(new DomainException("err"));
            var result = await _controller.CreateEvent(ClubId, new CreateEventRequest { EventName = "B", Description = "D" }, null);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateEvent_Returns500_WhenUnexpectedException()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<string>(), It.IsAny<Guid?>())).ThrowsAsync(new Exception("DB error"));
            var result = await _controller.CreateEvent(ClubId, new CreateEventRequest { EventName = "E", Description = "D" }, null);
            result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        #endregion

        #region UpdateEvent

        [Fact]
        public async Task UpdateEvent_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            var request = new UpdateEventRequest { EventId = 1, EventName = "Updated", Description = "Desc", Location = "A" };
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId });
            _mockEventService.Setup(s => s.UpdateEventAsync(It.IsAny<UpdateEventRequest>())).ReturnsAsync(new EventDetailDto { EventId = 1, EventName = "Updated" });
            var result = await _controller.UpdateEvent(ClubId, 1, request, null);
            result.Result.Should().BeOfType<OkObjectResult>();
        }


        [Fact]
        public async Task UpdateEvent_ReturnsBadRequest_WhenIdMismatch()
        {
            SetupManagerClaims(ClubId);
            var result = await _controller.UpdateEvent(ClubId, 1, new UpdateEventRequest { EventId = 2, EventName = "X", Description = "X" }, null);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateEvent_ReturnsBadRequest_WhenWrongClub()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = 999 });
            var result = await _controller.UpdateEvent(ClubId, 1, new UpdateEventRequest { EventId = 1, EventName = "X", Description = "X" }, null);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region UploadEventImage

        [Fact]
        public async Task UploadEventImage_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            var existingDto = new EventDetailDto { EventId = 1, ClubId = ClubId, EventName = "E", Description = "D" };
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(existingDto);
            _mockFileStorageService.Setup(f => f.SaveFileAsync(It.IsAny<IFormFile>(), "uniclub/events")).ReturnsAsync("https://img.jpg");
            _mockEventService.Setup(s => s.UpdateEventAsync(It.IsAny<UpdateEventRequest>())).ReturnsAsync(new EventDetailDto { EventId = 1 });
            var result = await _controller.UploadEventImage(ClubId, 1, mockFile.Object);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UploadEventImage_ReturnsBadRequest_WhenNoFile()
        {
            SetupManagerClaims(ClubId);
            var result = await _controller.UploadEventImage(ClubId, 1, null!);
            result.Should().BeOfType<BadRequestObjectResult>();
        }


        #endregion

        #region CreateSession

        [Fact]
        public async Task CreateSession_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            var request = new CreateSessionRequest { EventId = 1, SessionName = "Opening", StartTime = DateTime.Now.AddDays(7), EndTime = DateTime.Now.AddDays(7).AddHours(1) };
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId });
            _mockEventService.Setup(s => s.CreateSessionAsync(request)).ReturnsAsync(new SessionDto { ScheduleId = 10, ScheduleName = "Opening" });
            var result = await _controller.CreateSession(ClubId, 1, request);
            result.Result.Should().BeOfType<OkObjectResult>();
        }


        [Fact]
        public async Task CreateSession_ReturnsBadRequest_WhenIdMismatch()
        {
            SetupManagerClaims(ClubId);
            var result = await _controller.CreateSession(ClubId, 1, new CreateSessionRequest { EventId = 2, SessionName = "X" });
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region UpdateSession

        [Fact]
        public async Task UpdateSession_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId });
            _mockEventService.Setup(s => s.UpdateSessionAsync(It.IsAny<UpdateSessionRequest>())).ReturnsAsync(new SessionDto { ScheduleId = 10, ScheduleName = "Updated" });
            var result = await _controller.UpdateSession(ClubId, 1, 10, new UpdateSessionRequest { SessionName = "Updated" });
            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateSession_Returns404_WhenNotFoundException()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId });
            _mockEventService.Setup(s => s.UpdateSessionAsync(It.IsAny<UpdateSessionRequest>())).ThrowsAsync(new NotFoundException("Session", 10));
            var result = await _controller.UpdateSession(ClubId, 1, 10, new UpdateSessionRequest { SessionName = "X" });
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }


        #endregion

        #region DeleteSession

        [Fact]
        public async Task DeleteSession_ReturnsNoContent_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId });
            _mockEventService.Setup(s => s.DeleteSessionAsync(10, 1)).Returns(Task.CompletedTask);
            var result = await _controller.DeleteSession(ClubId, 1, 10);
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteSession_Returns404_WhenNotFoundException()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId });
            _mockEventService.Setup(s => s.DeleteSessionAsync(999, 1)).ThrowsAsync(new NotFoundException("Session", 999));
            var result = await _controller.DeleteSession(ClubId, 1, 999);
            result.Should().BeOfType<NotFoundObjectResult>();
        }


        #endregion

        #region OpenRegistration

        [Fact]
        public async Task OpenRegistration_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            var request = new OpenRegistrationRequest { EventId = 1, RegistrationStartDate = DateTime.Now, RegistrationEndDate = DateTime.Now.AddDays(3) };
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId });
            _mockEventService.Setup(s => s.OpenRegistrationAsync(request)).ReturnsAsync(new EventDetailDto { EventId = 1, Status = "REGISTRATION_OPEN" });
            var result = await _controller.OpenRegistration(ClubId, 1, request);
            result.Result.Should().BeOfType<OkObjectResult>();
        }


        [Fact]
        public async Task OpenRegistration_ReturnsBadRequest_WhenIdMismatch()
        {
            SetupManagerClaims(ClubId);
            var result = await _controller.OpenRegistration(ClubId, 1, new OpenRegistrationRequest { EventId = 2 });
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region StartEvent

        [Fact]
        public async Task StartEvent_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId });
            _mockEventService.Setup(s => s.StartEventAsync(1)).ReturnsAsync(("ABC123", DateTime.Now.AddHours(2)));
            var result = await _controller.StartEvent(ClubId, 1);
            result.Should().BeOfType<OkObjectResult>();
        }


        #endregion

        #region CompleteEvent

        [Fact]
        public async Task CompleteEvent_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId });
            _mockEventService.Setup(s => s.CompleteEventAsync(1)).Returns(Task.CompletedTask);
            var result = await _controller.CompleteEvent(ClubId, 1);
            result.Should().BeOfType<OkObjectResult>();
        }


        #endregion

        #region GetSessions

        [Fact]
        public async Task GetSessions_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto
            {
                EventId = 1, ClubId = ClubId, Sessions = new List<SessionDto> { new SessionDto { ScheduleId = 1, ScheduleName = "Session 1" } }
            });
            var result = await _controller.GetSessions(ClubId, 1);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetSessions_Returns403_WhenNotManager()
        {
            SetupNonManagerClaims();
            var result = await _controller.GetSessions(ClubId, 1);
            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GetSessions_ReturnsBadRequest_WhenWrongClub()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = 999 });
            var result = await _controller.GetSessions(ClubId, 1);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region GetMyRole

        [Fact]
        public async Task GetMyRole_ReturnsAdmin_WhenClubManager()
        {
            SetupManagerClaims(ClubId);
            _mockEventRoles.Setup(r => r.GetEventPolicyNamesAsync()).ReturnsAsync(new List<string> { "editevent", "managecollaborator" });
            var result = await _controller.GetMyRole(ClubId, 1);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetMyRole_ReturnsUnauthorized_WhenNoUser()
        {
            SetupNonManagerClaims();
            var result = await _controller.GetMyRole(ClubId, 1);
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task GetMyRole_ReturnsCollabRole_WhenCollaborator()
        {
            var userId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new Claim("club_roles", JsonSerializer.Serialize(new[] { new { ClubId = 99, RoleName = "Member", Level = 3 } })),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) } };

            var collab = new UserEventRole
            {
                EventMemberId = 1, EventId = 1, UserId = userId,
                EventRole = new EventRole { RoleName = "Coordinator", EventRolePolicies = new List<EventRolePolicy>() },
                EventMemberPolicies = new List<EventMemberPolicy>()
            };
            _mockEventMembers.Setup(m => m.GetByEventAndUserAsync(1, userId)).ReturnsAsync(collab);
            _mockEventRoles.Setup(r => r.GetEventPolicyNamesAsync()).ReturnsAsync(new List<string> { "editevent" });
            var result = await _controller.GetMyRole(ClubId, 1);
            result.Should().BeOfType<OkObjectResult>();
        }

        #endregion

        #region GetEventRoles

        [Fact]
        public async Task GetEventRoles_ReturnsOk_WhenManager()
        {
            SetupManagerClaims(ClubId);
            _mockEventRoles.Setup(r => r.GetAllAsync(1)).ReturnsAsync(new List<EventRole>
            { new EventRole { EventRoleId = 1, RoleName = "Creator", Level = 1, EventRolePolicies = new List<EventRolePolicy>() } });
            var result = await _controller.GetEventRoles(ClubId, 1);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetEventRoles_Returns403_WhenNotCollaborator()
        {
            SetupNonManagerClaims();
            var result = await _controller.GetEventRoles(ClubId, 1);
            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        }

        #endregion

        #region CreateEventRole

        [Fact]
        public async Task CreateEventRole_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            _mockEventRoles.Setup(r => r.RoleNameExistsAsync("Checker", 1)).ReturnsAsync(false);
            _mockEventRoles.Setup(r => r.CreateAsync(It.IsAny<EventRole>())).ReturnsAsync(new EventRole { EventRoleId = 10, RoleName = "Checker" });
            var result = await _controller.CreateEventRole(ClubId, 1, new ClubEventsController.EventRoleDto { RoleName = "Checker", Description = "Check-in" });
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CreateEventRole_ReturnsBadRequest_WhenDuplicate()
        {
            SetupManagerClaims(ClubId);
            _mockEventRoles.Setup(r => r.RoleNameExistsAsync("Creator", 1)).ReturnsAsync(true);
            var result = await _controller.CreateEventRole(ClubId, 1, new ClubEventsController.EventRoleDto { RoleName = "Creator" });
            result.Should().BeOfType<BadRequestObjectResult>();
        }


        #endregion

        #region UpdateEventRole

        [Fact]
        public async Task UpdateEventRole_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            _mockEventRoles.Setup(r => r.GetByIdAsync(2, 1)).ReturnsAsync(new EventRole { EventRoleId = 2, RoleName = "Old", Level = 2 });
            _mockEventRoles.Setup(r => r.UpdateAsync(It.IsAny<EventRole>())).ReturnsAsync(true);
            var result = await _controller.UpdateEventRole(ClubId, 1, 2, new ClubEventsController.EventRoleDto { RoleName = "New" });
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateEventRole_Returns404_WhenNotFound()
        {
            SetupManagerClaims(ClubId);
            _mockEventRoles.Setup(r => r.GetByIdAsync(999, 1)).ReturnsAsync((EventRole?)null);
            var result = await _controller.UpdateEventRole(ClubId, 1, 999, new ClubEventsController.EventRoleDto { RoleName = "X" });
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateEventRole_ReturnsBadRequest_WhenCreatorRole()
        {
            SetupManagerClaims(ClubId);
            _mockEventRoles.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(new EventRole { EventRoleId = 1, Level = 1 });
            var result = await _controller.UpdateEventRole(ClubId, 1, 1, new ClubEventsController.EventRoleDto { RoleName = "Hacked" });
            result.Should().BeOfType<BadRequestObjectResult>();
        }


        #endregion

        #region DeleteEventRole

        [Fact]
        public async Task DeleteEventRole_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            _mockEventRoles.Setup(r => r.GetByIdAsync(2, 1)).ReturnsAsync(new EventRole { EventRoleId = 2, Level = 2 });
            _mockEventRoles.Setup(r => r.DeleteAsync(2)).ReturnsAsync(true);
            var result = await _controller.DeleteEventRole(ClubId, 1, 2);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task DeleteEventRole_Returns404_WhenNotFound()
        {
            SetupManagerClaims(ClubId);
            _mockEventRoles.Setup(r => r.GetByIdAsync(999, 1)).ReturnsAsync((EventRole?)null);
            var result = await _controller.DeleteEventRole(ClubId, 1, 999);
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteEventRole_ReturnsBadRequest_WhenCreatorRole()
        {
            SetupManagerClaims(ClubId);
            _mockEventRoles.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(new EventRole { EventRoleId = 1, Level = 1 });
            var result = await _controller.DeleteEventRole(ClubId, 1, 1);
            result.Should().BeOfType<BadRequestObjectResult>();
        }


        #endregion

        #region SetEventRolePolicies

        [Fact]
        public async Task SetEventRolePolicies_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            _mockEventRoles.Setup(r => r.GetByIdAsync(2, 1)).ReturnsAsync(new EventRole { EventRoleId = 2, Level = 2 });
            _mockEventRoles.Setup(r => r.SetPoliciesAsync(2, It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);
            var result = await _controller.SetEventRolePolicies(ClubId, 1, 2, new List<string> { "editevent" });
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task SetEventRolePolicies_Returns404_WhenNotFound()
        {
            SetupManagerClaims(ClubId);
            _mockEventRoles.Setup(r => r.GetByIdAsync(999, 1)).ReturnsAsync((EventRole?)null);
            var result = await _controller.SetEventRolePolicies(ClubId, 1, 999, new List<string>());
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task SetEventRolePolicies_ReturnsBadRequest_WhenCreatorRole()
        {
            SetupManagerClaims(ClubId);
            _mockEventRoles.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(new EventRole { EventRoleId = 1, Level = 1 });
            var result = await _controller.SetEventRolePolicies(ClubId, 1, 1, new List<string> { "x" });
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region GetEventMembers

        [Fact]
        public async Task GetEventMembers_ReturnsOk_WhenManager()
        {
            SetupManagerClaims(ClubId);
            _mockEventMembers.Setup(m => m.GetByEventIdAsync(1)).ReturnsAsync(new List<UserEventRole>
            {
                new UserEventRole { EventMemberId = 1, UserId = Guid.NewGuid(), User = new User { FullName = "Test" },
                    EventRole = new EventRole { RoleName = "Creator", EventRolePolicies = new List<EventRolePolicy>() },
                    EventMemberPolicies = new List<EventMemberPolicy>() }
            });
            var result = await _controller.GetEventMembers(ClubId, 1);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetEventMembers_Returns403_WhenNotCollaborator()
        {
            SetupNonManagerClaims();
            var result = await _controller.GetEventMembers(ClubId, 1);
            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        }

        #endregion

        #region AddEventMember

        [Fact]
        public async Task AddEventMember_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            var userId = Guid.NewGuid();
            _mockEventMembers.Setup(m => m.GetByEventAndUserAsync(1, userId)).ReturnsAsync((UserEventRole?)null);
            _mockEventMembers.Setup(m => m.AddAsync(It.IsAny<UserEventRole>())).ReturnsAsync(new UserEventRole { EventMemberId = 5 });
            var result = await _controller.AddEventMember(ClubId, 1, new ClubEventsController.AddEventMemberRequest { UserId = userId, EventRoleId = 2 });
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task AddEventMember_ReturnsBadRequest_WhenAlreadyMember()
        {
            SetupManagerClaims(ClubId);
            var userId = Guid.NewGuid();
            _mockEventMembers.Setup(m => m.GetByEventAndUserAsync(1, userId)).ReturnsAsync(new UserEventRole { EventMemberId = 1 });
            var result = await _controller.AddEventMember(ClubId, 1, new ClubEventsController.AddEventMemberRequest { UserId = userId });
            result.Should().BeOfType<BadRequestObjectResult>();
        }


        #endregion

        #region UpdateEventMemberRole

        [Fact]
        public async Task UpdateEventMemberRole_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            _mockEventMembers.Setup(m => m.GetByIdAsync(5)).ReturnsAsync(new UserEventRole { EventMemberId = 5, EventId = 1, EventRole = new EventRole { Level = 2 } });
            _mockEventMembers.Setup(m => m.UpdateAsync(It.IsAny<UserEventRole>())).ReturnsAsync(true);
            var result = await _controller.UpdateEventMemberRole(ClubId, 1, 5, 3);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateEventMemberRole_Returns404_WhenNotFound()
        {
            SetupManagerClaims(ClubId);
            _mockEventMembers.Setup(m => m.GetByIdAsync(999)).ReturnsAsync((UserEventRole?)null);
            var result = await _controller.UpdateEventMemberRole(ClubId, 1, 999, 2);
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateEventMemberRole_ReturnsBadRequest_WhenCreator()
        {
            SetupManagerClaims(ClubId);
            _mockEventMembers.Setup(m => m.GetByIdAsync(1)).ReturnsAsync(new UserEventRole { EventMemberId = 1, EventId = 1, EventRole = new EventRole { Level = 1 } });
            var result = await _controller.UpdateEventMemberRole(ClubId, 1, 1, 3);
            result.Should().BeOfType<BadRequestObjectResult>();
        }


        #endregion

        #region RemoveEventMember

        [Fact]
        public async Task RemoveEventMember_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            _mockEventMembers.Setup(m => m.GetByIdAsync(5)).ReturnsAsync(new UserEventRole { EventMemberId = 5, EventId = 1, EventRole = new EventRole { Level = 2 } });
            _mockEventMembers.Setup(m => m.DeleteAsync(5)).ReturnsAsync(true);
            var result = await _controller.RemoveEventMember(ClubId, 1, 5);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task RemoveEventMember_Returns404_WhenNotFound()
        {
            SetupManagerClaims(ClubId);
            _mockEventMembers.Setup(m => m.GetByIdAsync(999)).ReturnsAsync((UserEventRole?)null);
            var result = await _controller.RemoveEventMember(ClubId, 1, 999);
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task RemoveEventMember_ReturnsBadRequest_WhenCreator()
        {
            SetupManagerClaims(ClubId);
            _mockEventMembers.Setup(m => m.GetByIdAsync(1)).ReturnsAsync(new UserEventRole { EventMemberId = 1, EventId = 1, EventRole = new EventRole { Level = 1 } });
            var result = await _controller.RemoveEventMember(ClubId, 1, 1);
            result.Should().BeOfType<BadRequestObjectResult>();
        }


        #endregion

        #region SetEventMemberPolicies

        [Fact]
        public async Task SetEventMemberPolicies_ReturnsOk_WhenSuccess()
        {
            SetupManagerClaims(ClubId);
            _mockEventMembers.Setup(m => m.GetByIdAsync(5)).ReturnsAsync(new UserEventRole { EventMemberId = 5, EventId = 1, EventRole = new EventRole { Level = 2 } });
            _mockEventMembers.Setup(m => m.SetMemberPoliciesAsync(5, It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);
            var result = await _controller.SetEventMemberPolicies(ClubId, 1, 5, new List<string> { "editevent" });
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task SetEventMemberPolicies_Returns404_WhenNotFound()
        {
            SetupManagerClaims(ClubId);
            _mockEventMembers.Setup(m => m.GetByIdAsync(999)).ReturnsAsync((UserEventRole?)null);
            var result = await _controller.SetEventMemberPolicies(ClubId, 1, 999, new List<string>());
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task SetEventMemberPolicies_ReturnsBadRequest_WhenCreator()
        {
            SetupManagerClaims(ClubId);
            _mockEventMembers.Setup(m => m.GetByIdAsync(1)).ReturnsAsync(new UserEventRole { EventMemberId = 1, EventId = 1, EventRole = new EventRole { Level = 1 } });
            var result = await _controller.SetEventMemberPolicies(ClubId, 1, 1, new List<string> { "x" });
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region Additional Branch Coverage Tests

        [Fact]
        public async Task CreateEvent_WithImage_ReturnsCreated()
        {
            SetupManagerClaims(ClubId);
            var request = new CreateEventRequest { EventName = "Ev", Description = "D", StartDate = DateTime.Now.AddDays(7), EndDate = DateTime.Now.AddDays(8) };
            var mockFile = new Mock<IFormFile>(); mockFile.Setup(f => f.Length).Returns(1024);
            _mockFileStorageService.Setup(f => f.SaveFileAsync(It.IsAny<IFormFile>(), "uniclub/events")).ReturnsAsync("https://img.jpg");
            _mockEventService.Setup(s => s.CreateEventAsync(It.IsAny<CreateEventRequest>(), "https://img.jpg", It.IsAny<Guid?>())).ReturnsAsync(new EventDetailDto { EventId = 1 });
            var result = await _controller.CreateEvent(ClubId, request, mockFile.Object);
            result.Result.Should().BeOfType<CreatedAtActionResult>();
        }

        [Fact]
        public async Task CreateEvent_ReturnsBadRequest_WhenInvalidOperationException()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<string>(), It.IsAny<Guid?>())).ThrowsAsync(new InvalidOperationException("invalid"));
            var result = await _controller.CreateEvent(ClubId, new CreateEventRequest { EventName = "E", Description = "D" }, null);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateEvent_WithImage_ReturnsOk()
        {
            SetupManagerClaims(ClubId);
            var request = new UpdateEventRequest { EventId = 1, EventName = "U", Description = "D", Location = "L" };
            var mockFile = new Mock<IFormFile>(); mockFile.Setup(f => f.Length).Returns(512);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId });
            _mockFileStorageService.Setup(f => f.SaveFileAsync(It.IsAny<IFormFile>(), "uniclub/events")).ReturnsAsync("https://new.jpg");
            _mockEventService.Setup(s => s.UpdateEventAsync(It.IsAny<UpdateEventRequest>())).ReturnsAsync(new EventDetailDto { EventId = 1 });
            var result = await _controller.UpdateEvent(ClubId, 1, request, mockFile.Object);
            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UploadEventImage_ReturnsBadRequest_WhenWrongClub()
        {
            SetupManagerClaims(ClubId);
            var mockFile = new Mock<IFormFile>(); mockFile.Setup(f => f.Length).Returns(1024);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = 999 });
            var result = await _controller.UploadEventImage(ClubId, 1, mockFile.Object);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateSession_ReturnsBadRequest_WhenWrongClub()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = 999 });
            var result = await _controller.CreateSession(ClubId, 1, new CreateSessionRequest { EventId = 1, SessionName = "S" });
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateSession_ReturnsBadRequest_WhenDomainException()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId });
            _mockEventService.Setup(s => s.CreateSessionAsync(It.IsAny<CreateSessionRequest>())).ThrowsAsync(new DomainException("dup"));
            var result = await _controller.CreateSession(ClubId, 1, new CreateSessionRequest { EventId = 1, SessionName = "S" });
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateSession_Returns500_WhenException()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId });
            _mockEventService.Setup(s => s.CreateSessionAsync(It.IsAny<CreateSessionRequest>())).ThrowsAsync(new Exception("DB error"));
            var result = await _controller.CreateSession(ClubId, 1, new CreateSessionRequest { EventId = 1, SessionName = "S" });
            result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task UpdateSession_ReturnsBadRequest_WhenWrongClub()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = 999 });
            var result = await _controller.UpdateSession(ClubId, 1, 10, new UpdateSessionRequest { SessionName = "X" });
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateSession_ReturnsBadRequest_WhenDomainException()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId });
            _mockEventService.Setup(s => s.UpdateSessionAsync(It.IsAny<UpdateSessionRequest>())).ThrowsAsync(new DomainException("err"));
            var result = await _controller.UpdateSession(ClubId, 1, 10, new UpdateSessionRequest { SessionName = "X" });
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateSession_Returns500_WhenException()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId });
            _mockEventService.Setup(s => s.UpdateSessionAsync(It.IsAny<UpdateSessionRequest>())).ThrowsAsync(new Exception("err"));
            var result = await _controller.UpdateSession(ClubId, 1, 10, new UpdateSessionRequest { SessionName = "X" });
            result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task DeleteSession_ReturnsBadRequest_WhenWrongClub()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = 999 });
            var result = await _controller.DeleteSession(ClubId, 1, 10);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DeleteSession_Returns500_WhenException()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId });
            _mockEventService.Setup(s => s.DeleteSessionAsync(10, 1)).ThrowsAsync(new Exception("err"));
            var result = await _controller.DeleteSession(ClubId, 1, 10);
            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task OpenRegistration_ReturnsBadRequest_WhenWrongClub()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = 999 });
            var result = await _controller.OpenRegistration(ClubId, 1, new OpenRegistrationRequest { EventId = 1 });
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task StartEvent_ReturnsBadRequest_WhenWrongClub()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = 999 });
            var result = await _controller.StartEvent(ClubId, 1);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CompleteEvent_ReturnsBadRequest_WhenWrongClub()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = 999 });
            var result = await _controller.CompleteEvent(ClubId, 1);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetMyRole_ReturnsNullRole_WhenNotCollaborator()
        {
            var userId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new Claim("club_roles", JsonSerializer.Serialize(new[] { new { ClubId = 99, RoleName = "Member", Level = 3 } })),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) } };
            _mockEventMembers.Setup(m => m.GetByEventAndUserAsync(1, userId)).ReturnsAsync((UserEventRole?)null);
            _mockEventRoles.Setup(r => r.GetEventPolicyNamesAsync()).ReturnsAsync(new List<string>());
            var result = await _controller.GetMyRole(ClubId, 1);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetSessions_ReturnsEmptyList_WhenSessionsNull()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(new EventDetailDto { EventId = 1, ClubId = ClubId, Sessions = null });
            var result = await _controller.GetSessions(ClubId, 1);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetSessions_ReturnsBadRequest_WhenException()
        {
            SetupManagerClaims(ClubId);
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ThrowsAsync(new Exception("err"));
            var result = await _controller.GetSessions(ClubId, 1);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion
    }
}
