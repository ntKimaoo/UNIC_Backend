using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementation
{
    public class EventService : IEventService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateEventRequest> _createValidator;
        private readonly IValidator<UpdateEventRequest> _updateValidator;
        private readonly IValidator<CreateSessionRequest> _sessionValidator;
        private readonly IValidator<OpenRegistrationRequest> _registrationValidator;
        private readonly IEmailService _emailService;

        public EventService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateEventRequest> createValidator,
            IValidator<UpdateEventRequest> updateValidator,
            IValidator<CreateSessionRequest> sessionValidator,
            IValidator<OpenRegistrationRequest> registrationValidator,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _sessionValidator = sessionValidator;
            _registrationValidator = registrationValidator;
            _emailService = emailService;
        }

        public async Task<EventDetailDto> CreateEventAsync(CreateEventRequest request, string? imageUrl = null)
        {
            // Validate input
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new DomainException(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            }

            // Map to entity
            var eventEntity = _mapper.Map<Event>(request);
            eventEntity.CreatedAt = DateTime.Now;
            eventEntity.Status = "PLANNED";
            eventEntity.CheckInCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            // Set ImageUrl from Cloudinary upload (null if no image provided)
            eventEntity.ImageUrl = imageUrl;

            // Add to repository
            await _unitOfWork.Events.AddAsync(eventEntity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<EventDetailDto>(eventEntity);
        }

        public async Task<EventDetailDto> UpdateEventAsync(UpdateEventRequest request)
        {
            // Validate input
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new DomainException(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            }

            // Check if event exists
            var existingEvent = await _unitOfWork.Events.GetByIdAsync(request.EventId);
            if (existingEvent == null)
            {
                throw new NotFoundException("Event", request.EventId);
            }

            // Check status - cannot update if canceled or closed
            if (existingEvent.Status == "CANCELED" || existingEvent.Status == "CLOSED")
            {
                throw new DomainException($"Cannot update event with status '{existingEvent.Status}'");
            }

            // Map updates
            existingEvent.EventName = request.EventName;
            existingEvent.Description = request.Description;
            existingEvent.Location = request.Location;
            existingEvent.StartDate = request.StartDate;
            existingEvent.EndDate = request.EndDate;
            // Only update ImageUrl if a new one was provided (preserve existing if no new image)
            if (request.ImageUrl != null)
                existingEvent.ImageUrl = request.ImageUrl;

            _unitOfWork.Events.Update(existingEvent);
            await _unitOfWork.SaveChangesAsync();

            // Return from already-tracked entity — no extra DB round-trip needed
            return _mapper.Map<EventDetailDto>(existingEvent);
        }

        public async Task<SessionDto> CreateSessionAsync(CreateSessionRequest request)
        {
            // Validate input
            var validationResult = await _sessionValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new DomainException(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            }

            // Check if event exists
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(request.EventId);
            if (eventEntity == null)
            {
                throw new NotFoundException("Event", request.EventId);
            }

            // Validate session time is within event time
            if (eventEntity.StartDate.HasValue && request.StartTime < eventEntity.StartDate.Value)
            {
                throw new DomainException("Session start time cannot be before event start date");
            }

            if (eventEntity.EndDate.HasValue && request.EndTime > eventEntity.EndDate.Value)
            {
                throw new DomainException("Session end time cannot be after event end date");
            }

            // Map to entity
            var schedule = _mapper.Map<EventSchedule>(request);

            // Add to repository
            await _unitOfWork.EventSchedules.AddAsync(schedule);
            await _unitOfWork.SaveChangesAsync();

            // Return DTO
            return _mapper.Map<SessionDto>(schedule);
        }

        public async Task<EventDetailDto> OpenRegistrationAsync(OpenRegistrationRequest request)
        {
            // Validate input
            var validationResult = await _registrationValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new DomainException(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            }

            // Check if event exists
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(request.EventId);
            if (eventEntity == null)
            {
                throw new NotFoundException("Event", request.EventId);
            }

            // Check event status is PLANNED or REGISTRATION_OPEN (allow editing dates)
            if (eventEntity.Status != "PLANNED" && eventEntity.Status != "REGISTRATION_OPEN")
            {
                throw new DomainException($"Cannot open/update registration for event with status '{eventEntity.Status}'. Event must be in PLANNED or REGISTRATION_OPEN status.");
            }

            // Validate registration end date is before event start date
            if (eventEntity.StartDate.HasValue && request.RegistrationEndDate >= eventEntity.StartDate.Value)
            {
                throw new DomainException("Registration end date must be before event start date");
            }

            // Update event
            eventEntity.Status = "REGISTRATION_OPEN";
            eventEntity.RegistrationStartDate = request.RegistrationStartDate;
            eventEntity.RegistrationEndDate = request.RegistrationEndDate;
            eventEntity.MaxAttendees = request.MaxAttendees;

            _unitOfWork.Events.Update(eventEntity);
            await _unitOfWork.SaveChangesAsync();

            // Return updated DTO
            var updatedEvent = await _unitOfWork.Events.GetByIdWithDetailsAsync(request.EventId);
            return _mapper.Map<EventDetailDto>(updatedEvent);
        }

        public async Task<EventDetailDto> GetEventByIdAsync(int eventId)
        {
            var eventEntity = await _unitOfWork.Events.GetByIdWithDetailsAsync(eventId);
            if (eventEntity == null)
            {
                throw new NotFoundException("Event", eventId);
            }

            return _mapper.Map<EventDetailDto>(eventEntity);
        }

        public async Task<IEnumerable<EventDetailDto>> GetAllEventsAsync(int pageNumber = 1, int pageSize = 10)
        {
            var events = await _unitOfWork.Events.GetAllAsync(pageNumber, pageSize);
            return _mapper.Map<IEnumerable<EventDetailDto>>(events);
        }

        public async Task RegisterForEventAsync(int eventId, string userId, string? apiBaseUrl = null)
        {
            if (!Guid.TryParse(userId, out var userGuid))
                throw new DomainException("Invalid user ID format");

            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
            if (eventEntity == null)
                throw new NotFoundException("Event", eventId);

            if (eventEntity.Status != "REGISTRATION_OPEN" && eventEntity.Status != "OPEN_REGISTRATION")
                throw new DomainException($"Cannot register. Event status is '{eventEntity.Status}'. It must be REGISTRATION_OPEN.");

            if (eventEntity.RegistrationStartDate.HasValue && DateTime.Now < eventEntity.RegistrationStartDate.Value)
                throw new DomainException("Registration has not started yet.");

            if (eventEntity.RegistrationEndDate.HasValue && DateTime.Now > eventEntity.RegistrationEndDate.Value)
            {
                bool eventEnded = eventEntity.EndDate.HasValue && DateTime.Now > eventEntity.EndDate.Value;
                if (eventEnded)
                    throw new DomainException("Registration has ended.");
            }

            if (eventEntity.MaxAttendees.HasValue)
            {
                var currentAttendees = await _unitOfWork.Attendances.GetAttendeesByEventAsync(eventId);
                if (currentAttendees.Count() >= eventEntity.MaxAttendees.Value)
                {
                    throw new DomainException("Event has reached maximum capacity.");
                }
            }

            var isRegistered = await _unitOfWork.Attendances.IsUserRegisteredAsync(eventId, userGuid);
            if (isRegistered)
                throw new DomainException("User is already registered for this event.");

            var attendance = new Attendance
            {
                EventId = eventId,
                UserId = userGuid,
                RegistrationDate = DateTime.Now,
                AttendanceStatus = "REGISTERED",
                CheckInToken = Guid.NewGuid().ToString("N")
            };

            await _unitOfWork.Attendances.AddAsync(attendance);
            await _unitOfWork.SaveChangesAsync();

            var user = await _unitOfWork.Users.GetByIdAsync(userGuid);
            if (user != null)
            {
                _ = _emailService.SendEventRegistrationSuccessAsync(user.Email, user.FullName, eventEntity.EventName, eventEntity.StartDate, attendance.CheckInToken, apiBaseUrl);
            }
        }

        public async Task<(string checkInCode, DateTime expiresAt)> StartEventAsync(int eventId)
        {
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
            if (eventEntity == null)
                throw new NotFoundException("Event", eventId);

            if (eventEntity.Status != "REGISTRATION_OPEN" && eventEntity.Status != "OPEN_REGISTRATION")
                throw new DomainException($"Cannot start event from status '{eventEntity.Status}'.");

            eventEntity.Status = "ONGOING";
            string generatedCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            eventEntity.CheckInCode = generatedCode;
            DateTime expiry = DateTime.Now.AddHours(2);
            eventEntity.CodeExpiresAt = expiry;

            _unitOfWork.Events.Update(eventEntity);
            await _unitOfWork.SaveChangesAsync();

            var registeredAttendances = await _unitOfWork.Attendances.GetAttendeesByEventAsync(eventId);
            var usersToEmail = registeredAttendances.Where(a => a.AttendanceStatus == "REGISTERED").ToList();

            foreach (var att in usersToEmail)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(att.UserId);
                if (user != null)
                {
                    _ = _emailService.SendEventCheckInCodeAsync(user.Email, user.FullName, eventEntity.EventName, generatedCode);
                }
            }

            return (generatedCode, expiry);
        }

        public async Task CheckInEventAsync(int eventId, string userId, string checkInCode)
        {
            if (!Guid.TryParse(userId, out var userGuid))
                throw new DomainException("Invalid user ID format");

            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
            if (eventEntity == null)
                throw new NotFoundException("Event", eventId);

            if (eventEntity.Status != "ONGOING")
                throw new DomainException("Event is not currently ongoing.");

            if (string.IsNullOrEmpty(eventEntity.CheckInCode) || !eventEntity.CheckInCode.Equals(checkInCode, StringComparison.OrdinalIgnoreCase))
                throw new DomainException("Invalid check-in code.");

            if (eventEntity.CodeExpiresAt.HasValue && DateTime.Now > eventEntity.CodeExpiresAt.Value)
                throw new DomainException("Check-in code has expired.");

            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, userGuid);
            if (attendance == null)
                throw new DomainException("User is not registered for this event.");

            if (attendance.AttendanceStatus == "CHECKED_IN" || attendance.AttendanceStatus == "PRESENT")
                throw new DomainException("User has already checked in.");

            attendance.AttendanceStatus = "PRESENT";
            attendance.CheckInTime = DateTime.Now;

            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CompleteEventAsync(int eventId)
        {
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
            if (eventEntity == null)
                throw new NotFoundException("Event", eventId);

            if (eventEntity.Status != "ONGOING")
                throw new DomainException($"Cannot complete event from status '{eventEntity.Status}'.");

            eventEntity.Status = "COMPLETED";

            var allAttendances = await _unitOfWork.Attendances.GetAttendeesByEventAsync(eventId);
            foreach (var attendance in allAttendances)
            {
                if (attendance.AttendanceStatus == "REGISTERED")
                {
                    attendance.AttendanceStatus = "ABSENT";
                    _unitOfWork.Attendances.Update(attendance);
                }
            }

            _unitOfWork.Events.Update(eventEntity);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
