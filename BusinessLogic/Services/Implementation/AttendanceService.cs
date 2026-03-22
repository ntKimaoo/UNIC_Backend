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

            string status;

            // Giai đoạn 2: Atomic slot reservation — dùng transaction + ExecuteUpdateAsync
            await using var txn = await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (eventEntity.MaxAttendees.HasValue)
                {
                    // Atomic: trừ 1 slot nếu còn AvailableSlots > 0 (DB row-level lock)
                    bool gotSlot = await _unitOfWork.Events.TryDecrementSlotAsync(request.EventId);
                    if (!gotSlot)
                    {
                        // Slot hết → vào WAITLIST (không cần trừ slot)
                        status = nameof(AttendanceStatus.WAITLIST);
                    }
                    else
                    {
                        // Có slot → PENDING nếu cần duyệt, REGISTERED nếu không
                        status = eventEntity.RequiresApproval
                            ? nameof(AttendanceStatus.PENDING)
                            : nameof(AttendanceStatus.REGISTERED);
                    }
                }
                else
                {
                    // Không giới hạn chỗ → không cần trừ slot
                    status = eventEntity.RequiresApproval
                        ? nameof(AttendanceStatus.PENDING)
                        : nameof(AttendanceStatus.REGISTERED);
                }

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
                await txn.CommitAsync();

                // Email ngoài transaction — lỗi email KHÔNG rollback DB
                if (status == nameof(AttendanceStatus.REGISTERED))
                {
                    _ = Task.Run(async () =>
                    {
                        try { await _emailService.SendEventRegistrationSuccessAsync(user.Email, user.FullName, eventEntity.EventName, eventEntity.StartDate, attendance.CheckInToken); }
                        catch (Exception ex) { Console.WriteLine($"[Email] SendRegistration failed: {ex.Message}"); }
                    });
                }
            }
            catch
            {
                await txn.RollbackAsync();
                throw;
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
                Message = alreadyCheckedIn ? "Đã điểm danh trước đó." : "Đã điểm danh thành công.",
                MemberName = attendance.User?.FullName ?? "—",
                AlreadyCheckedIn = alreadyCheckedIn
            };
        }

        /// <summary>
        /// Trims token and, if it looks like a URL (e.g. .../qr/TOKEN), extracts the token part so scanning a QR that encodes the image URL still works.
        /// </summary>
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

            // Gửi email riêng, lỗi email KHÔNG rollback DB
            _ = Task.Run(async () =>
            {
                try
                {
                    var user = attendance.User;
                    var ev = attendance.Event;
                    if (user != null && ev != null)
                        await _emailService.SendEventRegistrationSuccessAsync(
                            user.Email, user.FullName, ev.EventName, ev.StartDate, attendance.CheckInToken);
                }
                catch (Exception ex) { Console.WriteLine($"[Email] ApproveRegistration email failed: {ex.Message}"); }
            });
        }

        public async Task<int> BulkApproveAsync(int eventId, List<Guid> userIds)
        {
            int approved = 0;
            foreach (var userId in userIds)
            {
                var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, userId);
                if (attendance == null) continue;
                if (attendance.AttendanceStatus != nameof(AttendanceStatus.PENDING)
                    && attendance.AttendanceStatus != nameof(AttendanceStatus.WAITLIST))
                    continue;

                attendance.AttendanceStatus = nameof(AttendanceStatus.REGISTERED);
                _unitOfWork.Attendances.Update(attendance);
                approved++;
            }
            if (approved > 0) await _unitOfWork.SaveChangesAsync();
            return approved;
        }

        public async Task RejectRegistrationAsync(int eventId, Guid userId)
        {
            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, userId);
            if (attendance == null) throw new NotFoundException("Attendance", userId);

            var oldStatus = attendance.AttendanceStatus;
            if (oldStatus != nameof(AttendanceStatus.PENDING) && oldStatus != nameof(AttendanceStatus.WAITLIST)
                && oldStatus != nameof(AttendanceStatus.REGISTERED))
                throw new DomainException($"Không thể từ chối đăng ký có trạng thái '{oldStatus}'.");

            attendance.AttendanceStatus = nameof(AttendanceStatus.REJECTED);
            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();

            // PENDING và REGISTERED đã chiếm slot → nhả slot với Atomic Direct-Promote
            bool wasOccupying = oldStatus == nameof(AttendanceStatus.PENDING)
                             || oldStatus == nameof(AttendanceStatus.REGISTERED);
            if (wasOccupying)
            {
                var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
                if (eventEntity != null)
                    await HandleSlotReleaseAsync(eventId, eventEntity.RequiresApproval);
            }
        }

        public async Task CancelRegistrationAsync(int eventId, Guid userId)
        {
            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, userId);
            if (attendance == null) throw new NotFoundException("Attendance", userId);

            var oldStatus = attendance.AttendanceStatus;
            attendance.AttendanceStatus = nameof(AttendanceStatus.CANCELLED);
            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();

            // Chỉ nhả slot khi status cũ thực sự chiếm slot
            var slotOccupying = new[]
            {
                nameof(AttendanceStatus.REGISTERED),
                nameof(AttendanceStatus.PENDING),
                nameof(AttendanceStatus.PRESENT),
                nameof(AttendanceStatus.CHECKED_IN),
                nameof(AttendanceStatus.ABSENT),
            };

            if (slotOccupying.Contains(oldStatus))
            {
                var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
                if (eventEntity != null)
                    await HandleSlotReleaseAsync(eventId, eventEntity.RequiresApproval);
            }
        }

        // Giai đoạn 3: Atomic Direct-Promote — không bao giờ nhả slot vào free pool
        private async Task HandleSlotReleaseAsync(int eventId, bool requiresApproval)
        {
            // Kiểm tra event có giới hạn không (AvailableSlots null = không giới hạn)
            var ev = await _unitOfWork.Events.GetByIdAsync(eventId);
            if (ev?.MaxAttendees == null) return;

            var promoteToStatus = requiresApproval
                ? nameof(AttendanceStatus.PENDING)     // vẫn cần manager duyệt
                : nameof(AttendanceStatus.REGISTERED);

            // Thử promote trực tiếp (WAITLIST → target) không qua pool
            // Guard condition 'AND Status=WAITLIST' chống Double Cancel
            bool promoted = await _unitOfWork.Events
                .TryDirectPromoteOldestWaitlistAsync(eventId, promoteToStatus);

            // Chỉ cộng slot lại khi không có ai để promote — không rải slot cho người ngoài cướp
            if (!promoted)
                await _unitOfWork.Events.IncrementSlotAsync(eventId);
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
