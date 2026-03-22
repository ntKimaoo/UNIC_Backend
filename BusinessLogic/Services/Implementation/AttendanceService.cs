using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using DataAccess.Enums;
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
        private readonly IEmailService _emailService;

        public AttendanceService(IUnitOfWork unitOfWork, IMapper mapper, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _emailService = emailService;
        }

        public async Task RegisterMemberAsync(EventRegistrationRequest request)
        {
            // Check if event exists
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(request.EventId);
            if (eventEntity == null)
            {
                throw new NotFoundException("Event", request.EventId);
            }

            // ── Auto-sync stale status from dates (same logic as EventService.AutoSyncStatusAsync) ──
            var now = DateTime.Now;
            bool statusChanged = false;
            var terminalStatuses = new[] { "ENDED", "CANCELED", "ONGOING" };

            if (!terminalStatuses.Contains(eventEntity.Status)
                && eventEntity.EndDate.HasValue
                && eventEntity.EndDate.Value <= now)
            {
                eventEntity.Status = "ENDED";
                statusChanged = true;
            }
            else if (eventEntity.Status == "REGISTRATION_OPEN"
                && eventEntity.RegistrationEndDate.HasValue
                && eventEntity.RegistrationEndDate.Value <= now)
            {
                eventEntity.Status = "REGISTRATION_CLOSED";
                statusChanged = true;
            }

            if (statusChanged)
            {
                _unitOfWork.Events.Update(eventEntity);
                await _unitOfWork.SaveChangesAsync();
            }

            // ── Unified registration window validation ──
            if (eventEntity.Status == "ENDED" || eventEntity.Status == "CANCELED")
                throw new DomainException($"Sự kiện đã kết thúc hoặc bị huỷ.");

            if (eventEntity.Status == "ONGOING")
                throw new DomainException("Sự kiện đang diễn ra, không thể đăng ký.");

            if (eventEntity.Status == "REGISTRATION_CLOSED")
                throw new DomainException("Đăng ký đã kết thúc.");

            if (eventEntity.Status != "REGISTRATION_OPEN")
                throw new DomainException($"Đăng ký chưa được mở. Trạng thái hiện tại: {eventEntity.Status}");

            // Check registration start date
            if (eventEntity.RegistrationStartDate.HasValue && now < eventEntity.RegistrationStartDate.Value)
                throw new DomainException($"Đăng ký chưa bắt đầu. Mở lúc {eventEntity.RegistrationStartDate.Value:dd/MM/yyyy HH:mm}");

            // Check if user already registered (duplicate check)
            var isAlreadyRegistered = await _unitOfWork.Attendances.IsUserRegisteredAsync(request.EventId, request.UserId);
            if (isAlreadyRegistered)
            {
                throw new ConflictException("User is already registered for this event");
            }

            // Check if user exists
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
            if (user == null)
            {
                throw new NotFoundException("User", request.UserId);
            }

            string status = nameof(AttendanceStatus.REGISTERED);
            bool isWaitlist = false;

            var currentAttendeesCount = await _unitOfWork.Events.GetAttendeeCountAsync(request.EventId);

            // Optimistic Concurrency Check for Capacity
            if (eventEntity.MaxAttendees.HasValue)
            {
                if (currentAttendeesCount >= eventEntity.MaxAttendees.Value)
                {
                    status = nameof(AttendanceStatus.WAITLIST);
                    isWaitlist = true;
                }
                else
                {
                    status = nameof(AttendanceStatus.REGISTERED);
                }
            }
            else
            {
                // No limit, just check approval
                status = nameof(AttendanceStatus.REGISTERED);
            }

            // Create attendance record
            var attendance = new Attendance
            {
                EventId = request.EventId,
                UserId = request.UserId,
                RegistrationDate = DateTime.Now,
                AttendanceStatus = status,
                CheckInToken = Guid.NewGuid().ToString("N")
            };

            await _unitOfWork.Attendances.AddAsync(attendance);
            await _unitOfWork.SaveChangesAsync();

            // Send notification in background if registered directly
            if (status == nameof(AttendanceStatus.REGISTERED))
            {
                _ = Task.Run(() => _emailService.SendEventRegistrationSuccessAsync(user.Email, user.FullName, eventEntity.EventName, eventEntity.StartDate, attendance.CheckInToken));
            }
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
            attendance.AttendanceStatus = nameof(AttendanceStatus.PRESENT);
            attendance.CheckInTime = DateTime.Now;

            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<CheckInQrResponse?> GetMyCheckInQrAsync(int eventId, Guid userId)
        {
            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, userId);
            if (attendance == null)
                return null;

            if (string.IsNullOrEmpty(attendance.CheckInToken))
            {
                attendance.CheckInToken = Guid.NewGuid().ToString("N");
                _unitOfWork.Attendances.Update(attendance);
                await _unitOfWork.SaveChangesAsync();
            }

            return new CheckInQrResponse
            {
                EventId = eventId,
                QrContent = attendance.CheckInToken
            };
        }

        public async Task<CheckInByQrResponse> CheckInByQrTokenAsync(int eventId, string token)
        {
            var tokenTrimmed = NormalizeCheckInToken(token);
            if (string.IsNullOrWhiteSpace(tokenTrimmed))
                throw new DomainException("Mã QR không hợp lệ.");

            var attendance = await _unitOfWork.Attendances.GetByCheckInTokenAsync(tokenTrimmed);
            if (attendance == null)
                throw new NotFoundException("Attendance", tokenTrimmed);

            if (attendance.EventId != eventId)
                throw new DomainException("Mã QR không thuộc sự kiện này.");

            var alreadyCheckedIn = attendance.AttendanceStatus == nameof(AttendanceStatus.PRESENT)
                                || attendance.AttendanceStatus == nameof(AttendanceStatus.CHECKED_IN);
            if (!alreadyCheckedIn)
            {
                attendance.AttendanceStatus = nameof(AttendanceStatus.PRESENT);
                attendance.CheckInTime = DateTime.Now;
                _unitOfWork.Attendances.Update(attendance);
                await _unitOfWork.SaveChangesAsync();
            }

            return new CheckInByQrResponse
            {
                Success = true,
                Message = alreadyCheckedIn ? "Đã điểm danh trước đó." : "Đã điểm danh thành công.",
                MemberName = attendance.User?.FullName ?? "—",
                AlreadyCheckedIn = alreadyCheckedIn
            };
        }

        public async Task<VerifyByLinkResult> VerifyAttendanceByLinkAsync(string? email, string code)
        {
            var codeTrimmed = NormalizeVerifyCode(code);
            if (string.IsNullOrWhiteSpace(codeTrimmed))
                return new VerifyByLinkResult { Success = false, Message = "Mã xác nhận không hợp lệ." };

            var attendance = await _unitOfWork.Attendances.GetByCheckInTokenAsync(codeTrimmed);
            if (attendance == null)
                return new VerifyByLinkResult { Success = false, Message = "Mã xác nhận không hợp lệ hoặc đã hết hạn." };

            if (!string.IsNullOrWhiteSpace(email))
            {
                var userEmail = attendance.User?.Email?.Trim();
                if (string.IsNullOrEmpty(userEmail) || !string.Equals(userEmail, email.Trim(), StringComparison.OrdinalIgnoreCase))
                    return new VerifyByLinkResult { Success = false, Message = "Email không khớp với đăng ký." };
            }

            var alreadyCheckedIn = attendance.AttendanceStatus == "PRESENT";
            if (!alreadyCheckedIn)
            {
                attendance.AttendanceStatus = "PRESENT";
                attendance.CheckInTime = DateTime.Now;
                _unitOfWork.Attendances.Update(attendance);
                await _unitOfWork.SaveChangesAsync();
            }

            return new VerifyByLinkResult
            {
                Success = true,
                Message = alreadyCheckedIn ? "Bạn đã được điểm danh trước đó." : "Đã xác nhận điểm danh thành công.",
                AlreadyCheckedIn = alreadyCheckedIn,
                MemberName = attendance.User?.FullName,
                EventName = attendance.Event?.EventName
            };
        }

        private static string? NormalizeVerifyCode(string? code)
        {
            var s = code?.Trim();
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (s.Contains("code=", StringComparison.OrdinalIgnoreCase))
            {
                var idx = s.IndexOf("code=", StringComparison.OrdinalIgnoreCase) + 5;
                var end = s.IndexOf('&', idx);
                if (end < 0) end = s.Length;
                s = s.Substring(idx, end - idx).Trim();
            }
            return NormalizeCheckInToken(s) ?? (string.IsNullOrWhiteSpace(s) ? null : s);
        }

        private static string? NormalizeCheckInToken(string? token)
        {
            var s = token?.Trim();
            if (string.IsNullOrWhiteSpace(s)) return null;
            const string qrSegment = "/qr/";
            var idx = s.IndexOf(qrSegment, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                s = s.Substring(idx + qrSegment.Length);
                var query = s.IndexOf('?');
                if (query >= 0) s = s.Substring(0, query);
                s = s.Trim();
            }
            if (string.IsNullOrWhiteSpace(s))
                return null;
            if (s.Contains("code=", StringComparison.OrdinalIgnoreCase))
            {
                var codeIdx = s.IndexOf("code=", StringComparison.OrdinalIgnoreCase) + 5;
                var end = s.IndexOf('&', codeIdx);
                if (end < 0) end = s.Length;
                s = s.Substring(codeIdx, end - codeIdx).Trim();
                try { s = Uri.UnescapeDataString(s); } catch { }
            }
            return string.IsNullOrWhiteSpace(s) ? null : s;
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

            // Validate member status is PRESENT (or legacy CHECKED_IN)
            if (attendance.AttendanceStatus != nameof(AttendanceStatus.PRESENT)
                && attendance.AttendanceStatus != nameof(AttendanceStatus.CHECKED_IN))
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

        public async Task ApproveRegistrationAsync(int eventId, Guid userId)
        {
            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, userId);
            if (attendance == null) throw new NotFoundException("Attendance", userId);

            if (attendance.AttendanceStatus != nameof(AttendanceStatus.PENDING) && attendance.AttendanceStatus != nameof(AttendanceStatus.WAITLIST))
                throw new DomainException($"Cannot approve registration with status '{attendance.AttendanceStatus}'");

            attendance.AttendanceStatus = nameof(AttendanceStatus.REGISTERED);
            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();

            // Background email
            // _ = Task.Run(() => _emailService.SendRegistrationApprovedEmailAsync(attendance.User.Email, attendance.User.FullName, attendance.Event.EventName, attendance.Event.StartDate, attendance.CheckInToken));
        }

        public async Task RejectRegistrationAsync(int eventId, Guid userId)
        {
            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, userId);
            if (attendance == null) throw new NotFoundException("Attendance", userId);

            attendance.AttendanceStatus = nameof(AttendanceStatus.REJECTED);
            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CancelRegistrationAsync(int eventId, Guid userId)
        {
            try
            {
                var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, userId);
                if (attendance == null) throw new NotFoundException("Attendance", userId);

                var oldStatus = attendance.AttendanceStatus;
                attendance.AttendanceStatus = nameof(AttendanceStatus.CANCELLED);
                _unitOfWork.Attendances.Update(attendance);

                var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
                
                // If the user being cancelled was REGISTERED or PENDING (occupying a potential slot), we might promote someone from waitlist
                if (oldStatus == nameof(AttendanceStatus.REGISTERED) || oldStatus == nameof(AttendanceStatus.PENDING))
                {
                    // Try to promote the oldest waitlisted person
                    var oldestWaitlist = await _unitOfWork.Attendances.GetOldestWaitlistedAttendeeAsync(eventId);
                    if (oldestWaitlist != null && eventEntity != null)
                    {
                        oldestWaitlist.AttendanceStatus = nameof(AttendanceStatus.REGISTERED);
                        _unitOfWork.Attendances.Update(oldestWaitlist);
                        
                        await _unitOfWork.SaveChangesAsync();

                        // AFTER COMMIT: Send background email to the promoted user
                        // _ = Task.Run(() => _emailService.SendWaitlistPromotionEmailAsync(
                        //     oldestWaitlist.User.Email, 
                        //     oldestWaitlist.User.FullName, 
                        //     eventEntity.EventName, 
                        //     eventEntity.StartDate, 
                        //     false, 
                        //     oldestWaitlist.CheckInToken));
                        
                        return; // Early return
                    }
                }

                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                throw;
            }
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
