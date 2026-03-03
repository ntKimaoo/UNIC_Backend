using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementation
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AttendanceService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task RegisterMemberAsync(EventRegistrationRequest request)
        {
            // Check if event exists
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(request.EventId);
            if (eventEntity == null)
            {
                throw new NotFoundException("Event", request.EventId);
            }

            // Check event status is REGISTRATION_OPEN
            if (eventEntity.Status != "REGISTRATION_OPEN")
            {
                throw new DomainException($"Event registration is not open. Current status: {eventEntity.Status}");
            }

            // Check if current time is within registration dates
            var now = DateTime.Now;
            if (eventEntity.RegistrationStartDate.HasValue && now < eventEntity.RegistrationStartDate.Value)
            {
                throw new DomainException("Registration has not started yet");
            }

            if (eventEntity.RegistrationEndDate.HasValue && now > eventEntity.RegistrationEndDate.Value)
            {
                throw new DomainException("Registration has ended");
            }

            // Check if user already registered (duplicate check)
            var isAlreadyRegistered = await _unitOfWork.Attendances.IsUserRegisteredAsync(request.EventId, request.UserId);
            if (isAlreadyRegistered)
            {
                throw new ConflictException("User is already registered for this event");
            }

            // Check max capacity if set
            if (eventEntity.MaxAttendees.HasValue)
            {
                var currentCount = await _unitOfWork.Events.GetAttendeeCountAsync(request.EventId);
                if (currentCount >= eventEntity.MaxAttendees.Value)
                {
                    throw new DomainException("Event has reached maximum capacity");
                }
            }

            // Check if user exists
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
            if (user == null)
            {
                throw new NotFoundException("User", request.UserId);
            }

            // Create attendance record
            var attendance = new Attendance
            {
                EventId = request.EventId,
                UserId = request.UserId,
                RegistrationDate = DateTime.Now,
                AttendanceStatus = "REGISTERED"
            };

            await _unitOfWork.Attendances.AddAsync(attendance);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<CheckInCodeResponse> GenerateCheckInCodeAsync(int eventId)
        {
            // Check if event exists
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
            if (eventEntity == null)
            {
                throw new NotFoundException("Event", eventId);
            }

            // Generate 6-character random uppercase code
            var code = GenerateRandomCode(6);

            // Set expiration (e.g., 15 minutes from now)
            var expiresAt = DateTime.Now.AddMinutes(15);

            // Update event
            eventEntity.CheckInCode = code;
            eventEntity.CodeExpiresAt = expiresAt;

            _unitOfWork.Events.Update(eventEntity);
            await _unitOfWork.SaveChangesAsync();

            // Return DTO with QR content
            return _mapper.Map<CheckInCodeResponse>(eventEntity);
        }

        public async Task CheckInMemberAsync(CheckInRequest request)
        {
            // Check if event exists
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(request.EventId);
            if (eventEntity == null)
            {
                throw new NotFoundException("Event", request.EventId);
            }

            // Validate check-in code
            if (string.IsNullOrEmpty(eventEntity.CheckInCode) || eventEntity.CheckInCode != request.Code)
            {
                throw new DomainException("Invalid check-in code");
            }

            // Validate code expiration
            if (!eventEntity.CodeExpiresAt.HasValue || eventEntity.CodeExpiresAt.Value < DateTime.Now)
            {
                throw new DomainException("Check-in code has expired");
            }

            // Find attendance record
            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(request.EventId, request.UserId);
            if (attendance == null)
            {
                throw new NotFoundException("Attendance record not found. User must register for the event first.");
            }

            // Update attendance status
            attendance.AttendanceStatus = "PRESENT";
            attendance.CheckInTime = DateTime.Now;

            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<CheckInByBarcodeResponse> CheckInByBarcodeAsync(int eventId, string barcode)
        {
            var barcodeTrimmed = barcode?.Trim();
            if (string.IsNullOrWhiteSpace(barcodeTrimmed))
            {
                throw new DomainException("Mã barcode không hợp lệ.");
            }

            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
            if (eventEntity == null)
            {
                throw new NotFoundException("Event", eventId);
            }

            var user = await _unitOfWork.Users.GetByStudentIdAsync(barcodeTrimmed);
            if (user == null)
            {
                throw new NotFoundException("User", barcodeTrimmed);
            }

            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, user.UserId);
            if (attendance == null)
            {
                throw new DomainException("Chưa đăng ký sự kiện. Sinh viên cần đăng ký trước khi điểm danh.");
            }

            var alreadyCheckedIn = attendance.AttendanceStatus == "PRESENT";
            if (!alreadyCheckedIn)
            {
                attendance.AttendanceStatus = "PRESENT";
                attendance.CheckInTime = DateTime.Now;
                _unitOfWork.Attendances.Update(attendance);
                await _unitOfWork.SaveChangesAsync();
            }

            return new CheckInByBarcodeResponse
            {
                Message = alreadyCheckedIn ? "Đã điểm danh trước đó." : "Đã điểm danh thành công.",
                MemberName = user.FullName,
                StudentId = user.StudentId
            };
        }

        public async Task EvaluateMemberAsync(EvaluateMemberRequest request)
        {
            // Check if event exists
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(request.EventId);
            if (eventEntity == null)
            {
                throw new NotFoundException("Event", request.EventId);
            }

            // Validate event has ended
            if (!eventEntity.EndDate.HasValue || eventEntity.EndDate.Value > DateTime.Now)
            {
                throw new DomainException("Cannot evaluate members before event has ended");
            }

            // Find attendance record
            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(request.EventId, request.UserId);
            if (attendance == null)
            {
                throw new NotFoundException("Attendance record not found");
            }

            // Validate member status is PRESENT
            if (attendance.AttendanceStatus != "PRESENT")
            {
                throw new DomainException($"Cannot evaluate member with status '{attendance.AttendanceStatus}'. Member must have attended the event.");
            }

            // Update score and comment
            attendance.Score = request.Score;
            attendance.Comment = request.Comment;

            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<AttendanceDetailDto>> GetEventAttendeesAsync(int eventId)
        {
            // Check if event exists
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
            if (eventEntity == null)
            {
                throw new NotFoundException("Event", eventId);
            }

            // Get attendees with user information
            var attendances = await _unitOfWork.Attendances.GetAttendeesByEventAsync(eventId);

            // Map to DTOs
            return _mapper.Map<IEnumerable<AttendanceDetailDto>>(attendances);
        }

        private string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
