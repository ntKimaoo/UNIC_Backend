using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using DataAccess.Enums;
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
        private readonly IAttendanceService _attendanceService;

        public EventService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateEventRequest> createValidator,
            IValidator<UpdateEventRequest> updateValidator,
            IValidator<CreateSessionRequest> sessionValidator,
            IValidator<OpenRegistrationRequest> registrationValidator,
            IEmailService emailService,
            IAttendanceService attendanceService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _sessionValidator = sessionValidator;
            _registrationValidator = registrationValidator;
            _emailService = emailService;
            _attendanceService = attendanceService;
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
            eventEntity.CreatedAt = DateTime.UtcNow;
            eventEntity.Status = "PLANNED";
            eventEntity.CheckInCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            // Set ImageUrl from Cloudinary upload (null if no image provided)
            eventEntity.ImageUrl = imageUrl;

            if (!eventEntity.IsPublic)
            {
                // Generate a WebRTC room code for private events
                eventEntity.Location = $"/webrtc/{Guid.NewGuid().ToString("N").Substring(0, 10)}";
            }

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
            
            // Handle Type switch (Offline <-> Online)
            if (request.IsOnline)
            {
                // If switching to or staying Online, ensure we have a WebRTC link
                if (string.IsNullOrEmpty(existingEvent.Location) || !existingEvent.Location.StartsWith("/webrtc/"))
                {
                    existingEvent.Location = $"/webrtc/{Guid.NewGuid().ToString("N").Substring(0, 10)}";
                }
            }
            else
            {
                // If staying or switching to Offline, ensure they provided a physical location
                if (string.IsNullOrWhiteSpace(request.Location))
                {
                    throw new DomainException("Location is required for offline events.");
                }
                existingEvent.Location = request.Location;
            }
            
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

            // Validate session time is within event time (chỉ áp dụng cho type 'main')
            // Setup sessions có thể bắt đầu trước event, break sessions linh hoạt
            var sessionType = request.SessionType?.ToLower() ?? "main";
            if (sessionType == "main")
            {
                if (eventEntity.StartDate.HasValue && request.StartTime < eventEntity.StartDate.Value)
                {
                    throw new DomainException("Session start time cannot be before event start date");
                }

                if (eventEntity.EndDate.HasValue && request.EndTime > eventEntity.EndDate.Value)
                {
                    throw new DomainException("Session end time cannot be after event end date");
                }
            }

            // Map to entity
            var schedule = _mapper.Map<EventSchedule>(request);

            // Add to repository
            await _unitOfWork.EventSchedules.AddAsync(schedule);
            await _unitOfWork.SaveChangesAsync();

            // Return DTO
            return _mapper.Map<SessionDto>(schedule);
        }

        public async Task<SessionDto> UpdateSessionAsync(UpdateSessionRequest request)
        {
            var schedule = await _unitOfWork.EventSchedules.GetByIdAsync(request.ScheduleId);
            if (schedule == null)
                throw new NotFoundException("Session", request.ScheduleId);

            if (schedule.EventId != request.EventId)
                throw new DomainException("Session does not belong to this event.");

            schedule.ScheduleName = request.SessionName;
            schedule.StartTime    = request.StartTime;
            schedule.EndTime      = request.EndTime;
            schedule.Location     = request.Location;
            schedule.Description  = request.Description;
            if (request.SessionType != null)
                schedule.SessionType = request.SessionType;

            _unitOfWork.EventSchedules.Update(schedule);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SessionDto>(schedule);
        }

        public async Task DeleteSessionAsync(int scheduleId, int eventId)
        {
            var schedule = await _unitOfWork.EventSchedules.GetByIdAsync(scheduleId);
            if (schedule == null)
                throw new NotFoundException("Session", scheduleId);

            if (schedule.EventId != eventId)
                throw new DomainException("Session does not belong to this event.");

            _unitOfWork.EventSchedules.Delete(schedule);
            await _unitOfWork.SaveChangesAsync();
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

            // Validate MaxAttendees vs current registered count
            if (request.MaxAttendees.HasValue)
            {
                var allAttendances = await _unitOfWork.Attendances.GetAttendeesByEventAsync(request.EventId);
                // Option B: chỉ đếm status thật sự chiếm slot (PENDING/WAITLIST không chiếm)
                var slotOccupyingStatuses = new[] { "REGISTERED", "CHECKED_IN", "PRESENT", "ABSENT" };
                var occupiedSlots = allAttendances.Count(a => slotOccupyingStatuses.Contains(a.AttendanceStatus));

                // Validation: không cho set maxAttendees nhỏ hơn số đã chiếm slot
                if (request.MaxAttendees.Value < occupiedSlots)
                {
                    throw new DomainException(
                        $"Không thể đặt giới hạn {request.MaxAttendees.Value} người vì hiện đã có {occupiedSlots} người đã được duyệt.");
                }

                eventEntity.MaxAttendees = request.MaxAttendees;
                eventEntity.AvailableSlots = request.MaxAttendees.Value - occupiedSlots;
            }
            else
            {
                eventEntity.MaxAttendees = null;
                eventEntity.AvailableSlots = null;
            }

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

            // Lazily correct stale status based on current time
            await AutoSyncStatusAsync(eventEntity);

            return _mapper.Map<EventDetailDto>(eventEntity);
        }

        public async Task<IEnumerable<EventDetailDto>> GetAllEventsAsync(int pageNumber = 1, int pageSize = 10)
        {
            var events = await _unitOfWork.Events.GetAllAsync(pageNumber, pageSize);

            // Lazily correct stale statuses for all returned events
            bool anyChanged = false;
            foreach (var ev in events)
            {
                bool changed = await AutoSyncStatusAsync(ev, saveImmediately: false);
                if (changed) anyChanged = true;
            }
            if (anyChanged) await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<IEnumerable<EventDetailDto>>(events);
        }

        public async Task RegisterForEventAsync(int eventId, string userId, string? apiBaseUrl = null)
        {
            if (!Guid.TryParse(userId, out var userGuid))
                throw new DomainException("Invalid user ID format");

            await _attendanceService.RegisterMemberAsync(new EventRegistrationRequest 
            { 
                EventId = eventId, 
                UserId = userGuid 
            });
        }

        public async Task<(string checkInCode, DateTime expiresAt)> StartEventAsync(int eventId)
        {
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
            if (eventEntity == null)
                throw new NotFoundException("Event", eventId);

            var allowedStatuses = new[] { "REGISTRATION_OPEN", "OPEN_REGISTRATION", "REGISTRATION_CLOSED", "PLANNED" };
            if (!allowedStatuses.Contains(eventEntity.Status))
                throw new DomainException($"Không thể bắt đầu sự kiện từ trạng thái '{eventEntity.Status}'.");

            eventEntity.Status = "ONGOING";
            string generatedCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            eventEntity.CheckInCode = generatedCode;
            DateTime expiry = DateTime.UtcNow.AddHours(2);
            eventEntity.CodeExpiresAt = expiry;

            _unitOfWork.Events.Update(eventEntity);
            await _unitOfWork.SaveChangesAsync();

            var registeredAttendances = await _unitOfWork.Attendances.GetAttendeesByEventAsync(eventId);
            var usersToEmail = registeredAttendances.Where(a => a.AttendanceStatus == nameof(AttendanceStatus.REGISTERED)).ToList();

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

            if (eventEntity.CodeExpiresAt.HasValue && DateTime.UtcNow > eventEntity.CodeExpiresAt.Value)
                throw new DomainException("Check-in code has expired.");

            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, userGuid);
            if (attendance == null)
                throw new DomainException("User is not registered for this event.");

            if (attendance.AttendanceStatus == nameof(AttendanceStatus.PRESENT)
                || attendance.AttendanceStatus == nameof(AttendanceStatus.CHECKED_IN))
                throw new DomainException("User has already checked in.");

            attendance.AttendanceStatus = nameof(AttendanceStatus.PRESENT);
            attendance.CheckInTime = DateTime.UtcNow;

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
                if (attendance.AttendanceStatus == nameof(AttendanceStatus.REGISTERED))
                {
                    attendance.AttendanceStatus = nameof(AttendanceStatus.ABSENT);
                    _unitOfWork.Attendances.Update(attendance);
                }
            }

            _unitOfWork.Events.Update(eventEntity);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Lazily corrects stale event status based on current time.
        /// Rules:
        ///   0. PLANNED + no manual registration setup + within 2 days of start → auto REGISTRATION_OPEN
        ///   1. REGISTRATION_OPEN + registrationEndDate passed  → REGISTRATION_CLOSED
        ///   2. Any non-terminal status + endDate passed        → ENDED
        /// If user manually calls OpenRegistration, those dates take precedence (override).
        /// </summary>
        private async Task<bool> AutoSyncStatusAsync(Event eventEntity, bool saveImmediately = true)
        {
            var now = DateTime.UtcNow;
            var originalStatus = eventEntity.Status;

            // Rule 2 first (higher priority): if event's own end time has passed, mark ENDED
            var terminalStatuses = new[] { "ENDED", "CANCELED", "ONGOING" };
            if (!terminalStatuses.Contains(eventEntity.Status)
                && eventEntity.EndDate.HasValue
                && eventEntity.EndDate.Value <= now)
            {
                eventEntity.Status = "ENDED";
            }
            // Rule 1: registration period closed but event hasn't started yet
            else if (eventEntity.Status == "REGISTRATION_OPEN"
                && eventEntity.RegistrationEndDate.HasValue
                && eventEntity.RegistrationEndDate.Value <= now)
            {
                eventEntity.Status = "REGISTRATION_CLOSED";
            }
            // Rule 0: auto-open registration if no manual setup and within 2 days of start
            else if (eventEntity.Status == "PLANNED"
                && eventEntity.StartDate.HasValue
                && !eventEntity.RegistrationStartDate.HasValue  // user didn't manually set
                && eventEntity.StartDate.Value.AddDays(-2) <= now
                && eventEntity.StartDate.Value.AddHours(-12) > now) // still before auto-close
            {
                eventEntity.Status = "REGISTRATION_OPEN";
                eventEntity.RegistrationStartDate = eventEntity.StartDate.Value.AddDays(-2);
                eventEntity.RegistrationEndDate = eventEntity.StartDate.Value.AddHours(-12);
            }

            if (eventEntity.Status == originalStatus)
                return false; // nothing changed

            _unitOfWork.Events.Update(eventEntity);
            if (saveImmediately)
                await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
