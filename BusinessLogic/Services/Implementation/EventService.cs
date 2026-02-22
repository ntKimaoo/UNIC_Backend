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

        public EventService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateEventRequest> createValidator,
            IValidator<UpdateEventRequest> updateValidator,
            IValidator<CreateSessionRequest> sessionValidator,
            IValidator<OpenRegistrationRequest> registrationValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _sessionValidator = sessionValidator;
            _registrationValidator = registrationValidator;
        }

        public async Task<EventDetailDto> CreateEventAsync(CreateEventRequest request)
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

            // Add to repository
            await _unitOfWork.Events.AddAsync(eventEntity);
            await _unitOfWork.SaveChangesAsync();

            // Return DTO
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
            existingEvent.ImageUrl = request.ImageUrl;

            // Update
            _unitOfWork.Events.Update(existingEvent);
            await _unitOfWork.SaveChangesAsync();

            // Return updated DTO
            var updatedEvent = await _unitOfWork.Events.GetByIdWithDetailsAsync(request.EventId);
            return _mapper.Map<EventDetailDto>(updatedEvent);
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

            // Check event status is PLANNED
            if (eventEntity.Status != "PLANNED")
            {
                throw new DomainException($"Cannot open registration for event with status '{eventEntity.Status}'. Event must be in PLANNED status.");
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
    }
}
