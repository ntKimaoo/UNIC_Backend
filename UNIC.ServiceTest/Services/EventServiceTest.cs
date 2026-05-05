using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Implementation;
using BusinessLogic.Services.Interface;
using DataAccess.Enums;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class EventServiceTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IValidator<CreateEventRequest>> _mockCreateValidator;
        private readonly Mock<IValidator<UpdateEventRequest>> _mockUpdateValidator;
        private readonly Mock<IValidator<CreateSessionRequest>> _mockSessionValidator;
        private readonly Mock<IValidator<OpenRegistrationRequest>> _mockRegistrationValidator;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IAttendanceService> _mockAttendanceService;
        private readonly EventService _eventService;

        public EventServiceTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCreateValidator = new Mock<IValidator<CreateEventRequest>>();
            _mockUpdateValidator = new Mock<IValidator<UpdateEventRequest>>();
            _mockSessionValidator = new Mock<IValidator<CreateSessionRequest>>();
            _mockRegistrationValidator = new Mock<IValidator<OpenRegistrationRequest>>();
            _mockEmailService = new Mock<IEmailService>();
            _mockAttendanceService = new Mock<IAttendanceService>();

            _eventService = new EventService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockCreateValidator.Object,
                _mockUpdateValidator.Object,
                _mockSessionValidator.Object,
                _mockRegistrationValidator.Object,
                _mockEmailService.Object,
                _mockAttendanceService.Object
            );
        }

        private static ValidationResult ValidResult() => new ValidationResult();
        private static ValidationResult InvalidResult(string msg) =>
            new ValidationResult(new[] { new ValidationFailure("Field", msg) });

        #region CreateEventAsync

        [Fact]
        public async Task CreateEventAsync_Success_PublicEvent_ReturnsDto()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                EventName = "Tech Talk",
                Description = "A tech event",
                IsPublic = true,
                StartDate = DateTime.Now.AddDays(7),
                EndDate = DateTime.Now.AddDays(8)
            };
            var eventEntity = new Event { EventId = 1, EventName = "Tech Talk", IsPublic = true };
            var expectedDto = new EventDetailDto { EventId = 1, EventName = "Tech Talk" };

            _mockCreateValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockMapper.Setup(m => m.Map<Event>(request)).Returns(eventEntity);
            _mockMapper.Setup(m => m.Map<EventDetailDto>(eventEntity)).Returns(expectedDto);
            _mockUnitOfWork.Setup(u => u.Events.AddAsync(eventEntity)).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _eventService.CreateEventAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.EventId.Should().Be(1);
            result.EventName.Should().Be("Tech Talk");
            _mockUnitOfWork.Verify(u => u.Events.AddAsync(It.IsAny<Event>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateEventAsync_PrivateEvent_GeneratesWebRTCLink()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                EventName = "Private Meeting",
                Description = "Internal",
                IsPublic = false,
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(2)
            };
            Event capturedEvent = null!;
            _mockCreateValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockMapper.Setup(m => m.Map<Event>(request)).Returns(new Event { IsPublic = false });
            _mockUnitOfWork.Setup(u => u.Events.AddAsync(It.IsAny<Event>()))
                .Callback<Event>(e => capturedEvent = e)
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<EventDetailDto>(It.IsAny<Event>()))
                .Returns(new EventDetailDto());

            // Act
            await _eventService.CreateEventAsync(request);

            // Assert
            capturedEvent.Location.Should().StartWith("/webrtc/");
            capturedEvent.Status.Should().Be("PLANNED");
        }

        [Fact]
        public async Task CreateEventAsync_WithImageUrl_SetsImageUrl()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                EventName = "Event with Image",
                Description = "Test",
                IsPublic = true,
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(2)
            };
            Event capturedEvent = null!;
            _mockCreateValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockMapper.Setup(m => m.Map<Event>(request)).Returns(new Event { IsPublic = true });
            _mockUnitOfWork.Setup(u => u.Events.AddAsync(It.IsAny<Event>()))
                .Callback<Event>(e => capturedEvent = e)
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<EventDetailDto>(It.IsAny<Event>()))
                .Returns(new EventDetailDto());

            // Act
            await _eventService.CreateEventAsync(request, "https://cdn.example.com/image.png");

            // Assert
            capturedEvent.ImageUrl.Should().Be("https://cdn.example.com/image.png");
        }

        [Fact]
        public async Task CreateEventAsync_ValidationFails_ThrowsDomainException()
        {
            // Arrange
            var request = new CreateEventRequest { EventName = "" };
            _mockCreateValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(InvalidResult("Event name is required"));

            // Act & Assert
            var act = () => _eventService.CreateEventAsync(request);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Event name is required*");
        }

        #endregion

        #region UpdateEventAsync

        [Fact]
        public async Task UpdateEventAsync_Success_OfflineEvent_ReturnsDto()
        {
            // Arrange
            var request = new UpdateEventRequest
            {
                EventId = 1,
                EventName = "Updated Event",
                Description = "Updated",
                Location = "Room 101",
                IsOnline = false,
                StartDate = DateTime.Now.AddDays(7),
                EndDate = DateTime.Now.AddDays(8)
            };
            var existingEvent = new Event { EventId = 1, Status = "PLANNED", EventName = "Old" };
            var expectedDto = new EventDetailDto { EventId = 1, EventName = "Updated Event" };

            _mockUpdateValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(existingEvent);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<EventDetailDto>(existingEvent)).Returns(expectedDto);

            // Act
            var result = await _eventService.UpdateEventAsync(request);

            // Assert
            result.EventName.Should().Be("Updated Event");
            existingEvent.Location.Should().Be("Room 101");
            _mockUnitOfWork.Verify(u => u.Events.Update(existingEvent), Times.Once);
        }

        [Fact]
        public async Task UpdateEventAsync_SwitchToOnline_GeneratesWebRTCLink()
        {
            // Arrange
            var request = new UpdateEventRequest
            {
                EventId = 1,
                EventName = "Online Event",
                Description = "Switching",
                IsOnline = true,
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(2)
            };
            var existingEvent = new Event { EventId = 1, Status = "PLANNED", Location = "Room 101" };

            _mockUpdateValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(existingEvent);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<EventDetailDto>(existingEvent))
                .Returns(new EventDetailDto());

            // Act
            await _eventService.UpdateEventAsync(request);

            // Assert
            existingEvent.Location.Should().StartWith("/webrtc/");
        }

        [Fact]
        public async Task UpdateEventAsync_EventNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var request = new UpdateEventRequest { EventId = 999, EventName = "X", Description = "X" };
            _mockUpdateValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(999)).ReturnsAsync((Event?)null);

            // Act & Assert
            var act = () => _eventService.UpdateEventAsync(request);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateEventAsync_CanceledEvent_ThrowsDomainException()
        {
            // Arrange
            var request = new UpdateEventRequest { EventId = 1, EventName = "X", Description = "X" };
            var existingEvent = new Event { EventId = 1, Status = "CANCELED" };

            _mockUpdateValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(existingEvent);

            // Act & Assert
            var act = () => _eventService.UpdateEventAsync(request);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Cannot update event*CANCELED*");
        }

        [Fact]
        public async Task UpdateEventAsync_ClosedEvent_ThrowsDomainException()
        {
            // Arrange
            var request = new UpdateEventRequest { EventId = 1, EventName = "X", Description = "X" };
            var existingEvent = new Event { EventId = 1, Status = "CLOSED" };

            _mockUpdateValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(existingEvent);

            // Act & Assert
            var act = () => _eventService.UpdateEventAsync(request);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Cannot update event*CLOSED*");
        }

        [Fact]
        public async Task UpdateEventAsync_OfflineMissingLocation_ThrowsDomainException()
        {
            // Arrange
            var request = new UpdateEventRequest
            {
                EventId = 1,
                EventName = "X",
                Description = "X",
                IsOnline = false,
                Location = null
            };
            var existingEvent = new Event { EventId = 1, Status = "PLANNED" };

            _mockUpdateValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(existingEvent);

            // Act & Assert
            var act = () => _eventService.UpdateEventAsync(request);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Location is required*");
        }

        [Fact]
        public async Task UpdateEventAsync_ValidationFails_ThrowsDomainException()
        {
            // Arrange
            var request = new UpdateEventRequest { EventId = 1 };
            _mockUpdateValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(InvalidResult("Name required"));

            // Act & Assert
            var act = () => _eventService.UpdateEventAsync(request);
            await act.Should().ThrowAsync<DomainException>().WithMessage("*Name required*");
        }

        #endregion

        #region CreateSessionAsync

        [Fact]
        public async Task CreateSessionAsync_Success_ReturnsSessionDto()
        {
            // Arrange
            var request = new CreateSessionRequest
            {
                EventId = 1,
                SessionName = "Opening",
                StartTime = new DateTime(2026, 4, 1, 9, 0, 0),
                EndTime = new DateTime(2026, 4, 1, 10, 0, 0)
            };
            var eventEntity = new Event
            {
                EventId = 1,
                StartDate = new DateTime(2026, 4, 1, 8, 0, 0),
                EndDate = new DateTime(2026, 4, 1, 18, 0, 0)
            };
            var schedule = new EventSchedule { ScheduleId = 10, EventId = 1 };
            var expectedDto = new SessionDto { ScheduleId = 10, ScheduleName = "Opening" };

            _mockSessionValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockMapper.Setup(m => m.Map<EventSchedule>(request)).Returns(schedule);
            _mockUnitOfWork.Setup(u => u.EventSchedules.AddAsync(schedule)).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<SessionDto>(schedule)).Returns(expectedDto);

            // Act
            var result = await _eventService.CreateSessionAsync(request);

            // Assert
            result.ScheduleId.Should().Be(10);
            result.ScheduleName.Should().Be("Opening");
        }

        [Fact]
        public async Task CreateSessionAsync_EventNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var request = new CreateSessionRequest { EventId = 999, SessionName = "X" };
            _mockSessionValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(999)).ReturnsAsync((Event?)null);

            // Act & Assert
            var act = () => _eventService.CreateSessionAsync(request);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateSessionAsync_StartBeforeEvent_ThrowsDomainException()
        {
            // Arrange
            var request = new CreateSessionRequest
            {
                EventId = 1,
                SessionName = "Early Session",
                StartTime = new DateTime(2026, 3, 31, 7, 0, 0),
                EndTime = new DateTime(2026, 4, 1, 10, 0, 0)
            };
            var eventEntity = new Event
            {
                EventId = 1,
                StartDate = new DateTime(2026, 4, 1, 8, 0, 0),
                EndDate = new DateTime(2026, 4, 1, 18, 0, 0)
            };

            _mockSessionValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);

            // Act & Assert
            var act = () => _eventService.CreateSessionAsync(request);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Session start time cannot be before*");
        }

        [Fact]
        public async Task CreateSessionAsync_EndAfterEvent_ThrowsDomainException()
        {
            // Arrange
            var request = new CreateSessionRequest
            {
                EventId = 1,
                SessionName = "Late Session",
                StartTime = new DateTime(2026, 4, 1, 9, 0, 0),
                EndTime = new DateTime(2026, 4, 2, 20, 0, 0)
            };
            var eventEntity = new Event
            {
                EventId = 1,
                StartDate = new DateTime(2026, 4, 1, 8, 0, 0),
                EndDate = new DateTime(2026, 4, 1, 18, 0, 0)
            };

            _mockSessionValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);

            // Act & Assert
            var act = () => _eventService.CreateSessionAsync(request);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Session end time cannot be after*");
        }

        [Fact]
        public async Task CreateSessionAsync_ValidationFails_ThrowsDomainException()
        {
            // Arrange
            var request = new CreateSessionRequest { EventId = 1 };
            _mockSessionValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(InvalidResult("Session name is required"));

            // Act & Assert
            var act = () => _eventService.CreateSessionAsync(request);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Session name is required*");
        }

        #endregion

        #region UpdateSessionAsync

        [Fact]
        public async Task UpdateSessionAsync_Success_ReturnsDto()
        {
            // Arrange
            var request = new UpdateSessionRequest
            {
                ScheduleId = 10,
                EventId = 1,
                SessionName = "Updated Session",
                StartTime = DateTime.Now.AddHours(1),
                EndTime = DateTime.Now.AddHours(2)
            };
            var schedule = new EventSchedule { ScheduleId = 10, EventId = 1 };
            var expectedDto = new SessionDto { ScheduleId = 10, ScheduleName = "Updated Session" };

            _mockUnitOfWork.Setup(u => u.EventSchedules.GetByIdAsync(10)).ReturnsAsync(schedule);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<SessionDto>(schedule)).Returns(expectedDto);

            // Act
            var result = await _eventService.UpdateSessionAsync(request);

            // Assert
            result.ScheduleName.Should().Be("Updated Session");
            schedule.ScheduleName.Should().Be("Updated Session");
            _mockUnitOfWork.Verify(u => u.EventSchedules.Update(schedule), Times.Once);
        }

        [Fact]
        public async Task UpdateSessionAsync_NotFound_ThrowsNotFoundException()
        {
            // Arrange
            var request = new UpdateSessionRequest { ScheduleId = 999, EventId = 1, SessionName = "X" };
            _mockUnitOfWork.Setup(u => u.EventSchedules.GetByIdAsync(999)).ReturnsAsync((EventSchedule?)null);

            // Act & Assert
            var act = () => _eventService.UpdateSessionAsync(request);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateSessionAsync_WrongEvent_ThrowsDomainException()
        {
            // Arrange
            var request = new UpdateSessionRequest { ScheduleId = 10, EventId = 2, SessionName = "X" };
            var schedule = new EventSchedule { ScheduleId = 10, EventId = 1 };
            _mockUnitOfWork.Setup(u => u.EventSchedules.GetByIdAsync(10)).ReturnsAsync(schedule);

            // Act & Assert
            var act = () => _eventService.UpdateSessionAsync(request);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*does not belong*");
        }

        #endregion

        #region DeleteSessionAsync

        [Fact]
        public async Task DeleteSessionAsync_Success_DeletesSession()
        {
            // Arrange
            var schedule = new EventSchedule { ScheduleId = 10, EventId = 1 };
            _mockUnitOfWork.Setup(u => u.EventSchedules.GetByIdAsync(10)).ReturnsAsync(schedule);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _eventService.DeleteSessionAsync(10, 1);

            // Assert
            _mockUnitOfWork.Verify(u => u.EventSchedules.Delete(schedule), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteSessionAsync_NotFound_ThrowsNotFoundException()
        {
            _mockUnitOfWork.Setup(u => u.EventSchedules.GetByIdAsync(999))
                .ReturnsAsync((EventSchedule?)null);

            var act = () => _eventService.DeleteSessionAsync(999, 1);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteSessionAsync_WrongEvent_ThrowsDomainException()
        {
            var schedule = new EventSchedule { ScheduleId = 10, EventId = 1 };
            _mockUnitOfWork.Setup(u => u.EventSchedules.GetByIdAsync(10)).ReturnsAsync(schedule);

            var act = () => _eventService.DeleteSessionAsync(10, 2);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*does not belong*");
        }

        #endregion

        #region OpenRegistrationAsync

        [Fact]
        public async Task OpenRegistrationAsync_Success_ReturnsDto()
        {
            // Arrange
            var request = new OpenRegistrationRequest
            {
                EventId = 1,
                RegistrationStartDate = DateTime.Now,
                RegistrationEndDate = DateTime.Now.AddDays(3),
                MaxAttendees = 50
            };
            var eventEntity = new Event
            {
                EventId = 1,
                Status = "PLANNED",
                StartDate = DateTime.Now.AddDays(5)
            };
            var updatedEvent = new Event { EventId = 1, Status = "REGISTRATION_OPEN" };
            var expectedDto = new EventDetailDto { EventId = 1, Status = "REGISTRATION_OPEN" };

            _mockRegistrationValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.Events.GetByIdWithDetailsAsync(1)).ReturnsAsync(updatedEvent);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<EventDetailDto>(updatedEvent)).Returns(expectedDto);

            // Act
            var result = await _eventService.OpenRegistrationAsync(request);

            // Assert
            result.Status.Should().Be("REGISTRATION_OPEN");
            eventEntity.Status.Should().Be("REGISTRATION_OPEN");
            eventEntity.AvailableSlots.Should().Be(50);
        }

        [Fact]
        public async Task OpenRegistrationAsync_EventNotFound_ThrowsNotFoundException()
        {
            var request = new OpenRegistrationRequest { EventId = 999 };
            _mockRegistrationValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(999)).ReturnsAsync((Event?)null);

            var act = () => _eventService.OpenRegistrationAsync(request);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task OpenRegistrationAsync_InvalidStatus_ThrowsDomainException()
        {
            var request = new OpenRegistrationRequest { EventId = 1 };
            var eventEntity = new Event { EventId = 1, Status = "ONGOING" };

            _mockRegistrationValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);

            var act = () => _eventService.OpenRegistrationAsync(request);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Cannot open*registration*");
        }

        [Fact]
        public async Task OpenRegistrationAsync_RegEndAfterEventStart_ThrowsDomainException()
        {
            var request = new OpenRegistrationRequest
            {
                EventId = 1,
                RegistrationEndDate = new DateTime(2026, 4, 5, 12, 0, 0)
            };
            var eventEntity = new Event
            {
                EventId = 1,
                Status = "PLANNED",
                StartDate = new DateTime(2026, 4, 5, 10, 0, 0)
            };

            _mockRegistrationValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidResult());
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);

            var act = () => _eventService.OpenRegistrationAsync(request);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Registration end date must be before*");
        }

        #endregion

        #region GetEventByIdAsync

        [Fact]
        public async Task GetEventByIdAsync_Success_ReturnsDto()
        {
            var eventEntity = new Event
            {
                EventId = 1,
                Status = "PLANNED",
                EndDate = DateTime.Now.AddDays(7)
            };
            var expectedDto = new EventDetailDto { EventId = 1, EventName = "Test" };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdWithDetailsAsync(1)).ReturnsAsync(eventEntity);
            _mockMapper.Setup(m => m.Map<EventDetailDto>(eventEntity)).Returns(expectedDto);

            var result = await _eventService.GetEventByIdAsync(1);

            result.Should().NotBeNull();
            result.EventId.Should().Be(1);
        }

        [Fact]
        public async Task GetEventByIdAsync_NotFound_ThrowsNotFoundException()
        {
            _mockUnitOfWork.Setup(u => u.Events.GetByIdWithDetailsAsync(999))
                .ReturnsAsync((Event?)null);

            var act = () => _eventService.GetEventByIdAsync(999);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region GetAllEventsAsync

        [Fact]
        public async Task GetAllEventsAsync_ReturnsMappedList()
        {
            var events = new List<Event>
            {
                new Event { EventId = 1, Status = "PLANNED", EndDate = DateTime.Now.AddDays(7) },
                new Event { EventId = 2, Status = "PLANNED", EndDate = DateTime.Now.AddDays(14) }
            };
            var expectedDtos = new List<EventDetailDto>
            {
                new EventDetailDto { EventId = 1 },
                new EventDetailDto { EventId = 2 }
            };

            _mockUnitOfWork.Setup(u => u.Events.GetAllAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<Guid?>())).ReturnsAsync(events);
            _mockMapper.Setup(m => m.Map<IEnumerable<EventDetailDto>>(events)).Returns(expectedDtos);

            var result = await _eventService.GetAllEventsAsync(1, 10);

            result.Should().HaveCount(2);
        }

        #endregion

        #region RegisterForEventAsync

        [Fact]
        public async Task RegisterForEventAsync_Success_DelegatesToAttendanceService()
        {
            var userId = Guid.NewGuid().ToString();

            await _eventService.RegisterForEventAsync(1, userId);

            _mockAttendanceService.Verify(s => s.RegisterMemberAsync(
                It.Is<EventRegistrationRequest>(r => r.EventId == 1)), Times.Once);
        }

        [Fact]
        public async Task RegisterForEventAsync_InvalidGuid_ThrowsDomainException()
        {
            var act = () => _eventService.RegisterForEventAsync(1, "not-a-guid");
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Invalid user ID*");
        }

        #endregion

        #region StartEventAsync

        [Fact]
        public async Task StartEventAsync_Success_ReturnsCodeAndExpiry()
        {
            var eventEntity = new Event
            {
                EventId = 1,
                Status = "REGISTRATION_OPEN"
            };
            var attendances = new List<Attendance>
            {
                new Attendance
                {
                    EventId = 1,
                    UserId = Guid.NewGuid(),
                    AttendanceStatus = nameof(AttendanceStatus.REGISTERED)
                }
            };
            var user = new User { Email = "test@uni.edu", FullName = "Test User" };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.Attendances.GetAttendeesByEventAsync(1))
                .ReturnsAsync(attendances);
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);
            _mockEmailService.Setup(e => e.SendEventCheckInCodeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var result = await _eventService.StartEventAsync(1);

            result.checkInCode.Should().NotBeNullOrEmpty();
            result.checkInCode.Should().HaveLength(6);
            result.expiresAt.Should().BeAfter(DateTime.Now);
            eventEntity.Status.Should().Be("ONGOING");
        }

        [Fact]
        public async Task StartEventAsync_NotFound_ThrowsNotFoundException()
        {
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(999)).ReturnsAsync((Event?)null);

            var act = () => _eventService.StartEventAsync(999);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task StartEventAsync_WrongStatus_ThrowsDomainException()
        {
            var eventEntity = new Event { EventId = 1, Status = "PLANNED" };
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);

            var act = () => _eventService.StartEventAsync(1);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Cannot start event*");
        }

        #endregion

        #region CheckInEventAsync

        [Fact]
        public async Task CheckInEventAsync_Success_UpdatesAttendance()
        {
            var userId = Guid.NewGuid();
            var eventEntity = new Event
            {
                EventId = 1,
                Status = "ONGOING",
                CheckInCode = "ABC123",
                CodeExpiresAt = DateTime.Now.AddHours(1)
            };
            var attendance = new Attendance
            {
                EventId = 1,
                UserId = userId,
                AttendanceStatus = nameof(AttendanceStatus.REGISTERED)
            };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId)).ReturnsAsync(attendance);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            await _eventService.CheckInEventAsync(1, userId.ToString(), "ABC123");

            attendance.AttendanceStatus.Should().Be(nameof(AttendanceStatus.PRESENT));
            attendance.CheckInTime.Should().NotBeNull();
        }

        [Fact]
        public async Task CheckInEventAsync_InvalidGuid_ThrowsDomainException()
        {
            var act = () => _eventService.CheckInEventAsync(1, "bad-guid", "ABC123");
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Invalid user ID*");
        }

        [Fact]
        public async Task CheckInEventAsync_EventNotFound_ThrowsNotFoundException()
        {
            var userId = Guid.NewGuid();
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync((Event?)null);

            var act = () => _eventService.CheckInEventAsync(1, userId.ToString(), "X");
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CheckInEventAsync_NotOngoing_ThrowsDomainException()
        {
            var userId = Guid.NewGuid();
            var eventEntity = new Event { EventId = 1, Status = "PLANNED" };
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);

            var act = () => _eventService.CheckInEventAsync(1, userId.ToString(), "X");
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*not currently ongoing*");
        }

        [Fact]
        public async Task CheckInEventAsync_WrongCode_ThrowsDomainException()
        {
            var userId = Guid.NewGuid();
            var eventEntity = new Event
            {
                EventId = 1,
                Status = "ONGOING",
                CheckInCode = "ABC123",
                CodeExpiresAt = DateTime.Now.AddHours(1)
            };
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);

            var act = () => _eventService.CheckInEventAsync(1, userId.ToString(), "WRONG");
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Invalid check-in code*");
        }

        [Fact]
        public async Task CheckInEventAsync_ExpiredCode_ThrowsDomainException()
        {
            var userId = Guid.NewGuid();
            var eventEntity = new Event
            {
                EventId = 1,
                Status = "ONGOING",
                CheckInCode = "ABC123",
                CodeExpiresAt = DateTime.Now.AddHours(-1) // expired
            };
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);

            var act = () => _eventService.CheckInEventAsync(1, userId.ToString(), "ABC123");
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*expired*");
        }

        [Fact]
        public async Task CheckInEventAsync_NotRegistered_ThrowsDomainException()
        {
            var userId = Guid.NewGuid();
            var eventEntity = new Event
            {
                EventId = 1,
                Status = "ONGOING",
                CheckInCode = "ABC123",
                CodeExpiresAt = DateTime.Now.AddHours(1)
            };
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId))
                .ReturnsAsync((Attendance?)null);

            var act = () => _eventService.CheckInEventAsync(1, userId.ToString(), "ABC123");
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*not registered*");
        }

        [Fact]
        public async Task CheckInEventAsync_AlreadyCheckedIn_ThrowsDomainException()
        {
            var userId = Guid.NewGuid();
            var eventEntity = new Event
            {
                EventId = 1,
                Status = "ONGOING",
                CheckInCode = "ABC123",
                CodeExpiresAt = DateTime.Now.AddHours(1)
            };
            var attendance = new Attendance
            {
                EventId = 1,
                UserId = userId,
                AttendanceStatus = nameof(AttendanceStatus.PRESENT)
            };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.Attendances.GetByEventAndUserAsync(1, userId))
                .ReturnsAsync(attendance);

            var act = () => _eventService.CheckInEventAsync(1, userId.ToString(), "ABC123");
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*already checked in*");
        }

        #endregion

        #region CompleteEventAsync

        [Fact]
        public async Task CompleteEventAsync_Success_MarksAbsent()
        {
            var eventEntity = new Event { EventId = 1, Status = "ONGOING" };
            var attendances = new List<Attendance>
            {
                new Attendance { AttendanceStatus = nameof(AttendanceStatus.REGISTERED) },
                new Attendance { AttendanceStatus = nameof(AttendanceStatus.PRESENT) }
            };

            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);
            _mockUnitOfWork.Setup(u => u.Attendances.GetAttendeesByEventAsync(1)).ReturnsAsync(attendances);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            await _eventService.CompleteEventAsync(1);

            eventEntity.Status.Should().Be("COMPLETED");
            attendances[0].AttendanceStatus.Should().Be(nameof(AttendanceStatus.ABSENT));
            attendances[1].AttendanceStatus.Should().Be(nameof(AttendanceStatus.PRESENT)); // unchanged
        }

        [Fact]
        public async Task CompleteEventAsync_NotFound_ThrowsNotFoundException()
        {
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(999)).ReturnsAsync((Event?)null);

            var act = () => _eventService.CompleteEventAsync(999);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CompleteEventAsync_WrongStatus_ThrowsDomainException()
        {
            var eventEntity = new Event { EventId = 1, Status = "PLANNED" };
            _mockUnitOfWork.Setup(u => u.Events.GetByIdAsync(1)).ReturnsAsync(eventEntity);

            var act = () => _eventService.CompleteEventAsync(1);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Cannot complete event*");
        }

        #endregion
    }
}
