using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Implementation;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.BusinessLogic.Test.Services
{
    public class EventServiceTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IValidator<CreateEventRequest>> _mockCreateVal;
        private readonly Mock<IValidator<UpdateEventRequest>> _mockUpdateVal;
        private readonly Mock<IValidator<CreateSessionRequest>> _mockSessionVal;
        private readonly Mock<IValidator<OpenRegistrationRequest>> _mockRegistrationVal;
        private readonly Mock<IEventRepository> _mockEventRepo;
        private readonly Mock<IEventScheduleRepository> _mockEventScheduleRepo;

        private readonly EventService _eventService;

        public EventServiceTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCreateVal = new Mock<IValidator<CreateEventRequest>>();
            _mockUpdateVal = new Mock<IValidator<UpdateEventRequest>>();
            _mockSessionVal = new Mock<IValidator<CreateSessionRequest>>();
            _mockRegistrationVal = new Mock<IValidator<OpenRegistrationRequest>>();

            _mockEventRepo = new Mock<IEventRepository>();
            _mockEventScheduleRepo = new Mock<IEventScheduleRepository>();

            _mockUnitOfWork.Setup(u => u.Events).Returns(_mockEventRepo.Object);
            _mockUnitOfWork.Setup(u => u.EventSchedules).Returns(_mockEventScheduleRepo.Object);

            _eventService = new EventService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockCreateVal.Object,
                _mockUpdateVal.Object,
                _mockSessionVal.Object,
                _mockRegistrationVal.Object
            );
        }

        #region CreateEventAsync

        [Fact]
        public async Task CreateEventAsync_ShouldThrowDomainException_WhenValidationFails()
        {
            var request = new CreateEventRequest();
            var valResult = new ValidationResult(new[] { new ValidationFailure("Prop", "Error") });
            _mockCreateVal.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(valResult);

            var ex = await Assert.ThrowsAsync<DomainException>(() => _eventService.CreateEventAsync(request, null));
            Assert.Contains("Error", ex.Message);
        }

        [Fact]
        public async Task CreateEventAsync_ShouldCreateAndSaveEvent_WhenValid()
        {
            var request = new CreateEventRequest();
            _mockCreateVal.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
            
            var ev = new Event();
            _mockMapper.Setup(m => m.Map<Event>(request)).Returns(ev);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var expectedDto = new EventDetailDto();
            _mockMapper.Setup(m => m.Map<EventDetailDto>(ev)).Returns(expectedDto);

            var result = await _eventService.CreateEventAsync(request, "image.jpg");

            Assert.Equal(expectedDto, result);
            Assert.Equal("PLANNED", ev.Status);
            Assert.Equal("image.jpg", ev.ImageUrl);
            Assert.NotNull(ev.CheckInCode);
            _mockEventRepo.Verify(r => r.AddAsync(ev), Times.Once);
        }

        #endregion

        #region UpdateEventAsync

        [Fact]
        public async Task UpdateEventAsync_ShouldThrowDomainException_WhenValidationFails()
        {
            var request = new UpdateEventRequest();
            var valResult = new ValidationResult(new[] { new ValidationFailure("Prop", "Error") });
            _mockUpdateVal.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(valResult);

            var ex = await Assert.ThrowsAsync<DomainException>(() => _eventService.UpdateEventAsync(request));
            Assert.Contains("Error", ex.Message);
        }

        [Fact]
        public async Task UpdateEventAsync_ShouldThrowNotFoundException_WhenEventNotFound()
        {
            var request = new UpdateEventRequest { EventId = 1 };
            _mockUpdateVal.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Event?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _eventService.UpdateEventAsync(request));
        }

        [Fact]
        public async Task UpdateEventAsync_ShouldThrowDomainException_WhenEventStatusInvalid()
        {
            var request = new UpdateEventRequest { EventId = 1 };
            _mockUpdateVal.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Event { Status = "CANCELED" });

            var ex = await Assert.ThrowsAsync<DomainException>(() => _eventService.UpdateEventAsync(request));
            Assert.Contains("Cannot update event", ex.Message);
        }

        [Fact]
        public async Task UpdateEventAsync_ShouldUpdate_WhenValid()
        {
            var request = new UpdateEventRequest { EventId = 1, EventName = "New Name", ImageUrl = "new.jpg" };
            var existingEv = new Event { Status = "PLANNED", ImageUrl = "old.jpg" };
            
            _mockUpdateVal.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingEv);

            await _eventService.UpdateEventAsync(request);

            Assert.Equal("New Name", existingEv.EventName);
            Assert.Equal("new.jpg", existingEv.ImageUrl);
            _mockEventRepo.Verify(r => r.Update(existingEv), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region CreateSessionAsync

        [Fact]
        public async Task CreateSessionAsync_ShouldThrow_WhenValidationFails()
        {
            var request = new CreateSessionRequest();
            var valResult = new ValidationResult(new[] { new ValidationFailure("Prop", "Error") });
            _mockSessionVal.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(valResult);

            await Assert.ThrowsAsync<DomainException>(() => _eventService.CreateSessionAsync(request));
        }

        [Fact]
        public async Task CreateSessionAsync_ShouldThrowNotFound_WhenEventNotFound()
        {
            var request = new CreateSessionRequest { EventId = 1 };
            _mockSessionVal.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Event?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _eventService.CreateSessionAsync(request));
        }

        [Fact]
        public async Task CreateSessionAsync_ShouldThrow_WhenTimeInvalid()
        {
            var request = new CreateSessionRequest { EventId = 1, StartTime = DateTime.Now.AddDays(-1) }; // Before event start
            var ev = new Event { StartDate = DateTime.Now };
            
            _mockSessionVal.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var ex = await Assert.ThrowsAsync<DomainException>(() => _eventService.CreateSessionAsync(request));
            Assert.Contains("before event start", ex.Message);
        }

        [Fact]
        public async Task CreateSessionAsync_ShouldAddAndSave_WhenValid()
        {
            var request = new CreateSessionRequest { EventId = 1 };
            var ev = new Event(); // No dates
            var schedule = new EventSchedule();

            _mockSessionVal.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _mockMapper.Setup(m => m.Map<EventSchedule>(request)).Returns(schedule);

            await _eventService.CreateSessionAsync(request);

            _mockEventScheduleRepo.Verify(r => r.AddAsync(schedule), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region OpenRegistrationAsync

        [Fact]
        public async Task OpenRegistrationAsync_ShouldThrow_WhenEventStatusNotPlanned()
        {
            var request = new OpenRegistrationRequest { EventId = 1 };
            var ev = new Event { Status = "ONGOING" };
            
            _mockRegistrationVal.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var ex = await Assert.ThrowsAsync<DomainException>(() => _eventService.OpenRegistrationAsync(request));
            Assert.Contains("must be in PLANNED status", ex.Message);
        }

        [Fact]
        public async Task OpenRegistrationAsync_ShouldThrow_WhenEndDateAfterEventStart()
        {
            var request = new OpenRegistrationRequest { EventId = 1, RegistrationEndDate = DateTime.Now.AddDays(2) };
            var ev = new Event { Status = "PLANNED", StartDate = DateTime.Now.AddDays(1) };
            
            _mockRegistrationVal.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var ex = await Assert.ThrowsAsync<DomainException>(() => _eventService.OpenRegistrationAsync(request));
            Assert.Contains("must be before event start", ex.Message);
        }

        [Fact]
        public async Task OpenRegistrationAsync_ShouldUpdateStatusAndSave_WhenValid()
        {
            var request = new OpenRegistrationRequest { EventId = 1, RegistrationStartDate = DateTime.Now, MaxAttendees = 100 };
            var ev = new Event { Status = "PLANNED" }; // No start date
            
            _mockRegistrationVal.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
            _mockEventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _mockEventRepo.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(ev);

            await _eventService.OpenRegistrationAsync(request);

            Assert.Equal("REGISTRATION_OPEN", ev.Status);
            Assert.Equal(100, ev.MaxAttendees);
            _mockEventRepo.Verify(r => r.Update(ev), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region GetMethods

        [Fact]
        public async Task GetEventByIdAsync_ShouldThrowNotFound()
        {
            _mockEventRepo.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync((Event?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _eventService.GetEventByIdAsync(1));
        }

        [Fact]
        public async Task GetEventByIdAsync_ShouldReturnDto()
        {
            var ev = new Event();
            _mockEventRepo.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(ev);
            _mockMapper.Setup(m => m.Map<EventDetailDto>(ev)).Returns(new EventDetailDto());

            var result = await _eventService.GetEventByIdAsync(1);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetAllEventsAsync_ShouldReturnDtos()
        {
            var events = new List<Event> { new Event() };
            _mockEventRepo.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync(events);
            _mockMapper.Setup(m => m.Map<IEnumerable<EventDetailDto>>(events)).Returns(new List<EventDetailDto>());

            var result = await _eventService.GetAllEventsAsync();
            Assert.NotNull(result);
        }

        #endregion
    }
}
