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
            var now = DateTime.UtcNow;
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
                throw new ConflictException("Người dùng đã đăng ký sự kiện này rồi");
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
                    // Có giới hạn slot → kiểm tra còn chỗ không
                    bool hasSlot = (eventEntity.AvailableSlots ?? 0) > 0;

                    if (!hasSlot)
                    {
                        // Hết slot → WAITLIST (bất kể RequiresApproval)
                        status = nameof(AttendanceStatus.WAITLIST);
                    }
                    else if (eventEntity.RequiresApproval)
                    {
                        // Còn slot + cần duyệt → PENDING (không trừ slot, chờ Approve)
                        status = nameof(AttendanceStatus.PENDING);
                    }
                    else
                    {
                        // Còn slot + không cần duyệt → trừ slot ngay
                        bool gotSlot = await _unitOfWork.Events.TryDecrementSlotAsync(request.EventId);
                        status = gotSlot
                            ? nameof(AttendanceStatus.REGISTERED)
                            : nameof(AttendanceStatus.WAITLIST); // race condition fallback
                    }
                }
                else if (eventEntity.RequiresApproval)
                {
                    // Không giới hạn chỗ + cần duyệt → PENDING
                    status = nameof(AttendanceStatus.PENDING);
                }
                else
                {
                    // Không giới hạn chỗ + không cần duyệt → REGISTERED ngay
                    status = nameof(AttendanceStatus.REGISTERED);
                }
                // Check nếu đã có record cũ (REJECTED/CANCELLED) → update thay vì tạo mới
                var existingAttendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(request.EventId, request.UserId);
                if (existingAttendance != null
                    && (existingAttendance.AttendanceStatus == nameof(AttendanceStatus.REJECTED)
                        || existingAttendance.AttendanceStatus == nameof(AttendanceStatus.CANCELLED)))
                {
                    // Reuse record cũ — update status + reset fields
                    existingAttendance.AttendanceStatus = status;
                    existingAttendance.RegistrationDate = DateTime.UtcNow;
                    existingAttendance.CheckInToken = Guid.NewGuid().ToString("N");
                    existingAttendance.CheckInTime = null;
                    existingAttendance.Score = null;
                    existingAttendance.Comment = null;
                    _unitOfWork.Attendances.Update(existingAttendance);
                }
                else
                {
                    var attendance = new Attendance
                    {
                        EventId = request.EventId,
                        UserId = request.UserId,
                        RegistrationDate = DateTime.UtcNow,
                        AttendanceStatus = status,
                        CheckInToken = Guid.NewGuid().ToString("N")
                    };
                    await _unitOfWork.Attendances.AddAsync(attendance);
                }

                await _unitOfWork.SaveChangesAsync();
                await txn.CommitAsync();

                // Email ngoài transaction — lỗi email KHÔNG rollback DB
                var checkInToken = existingAttendance?.CheckInToken
                    ?? Guid.NewGuid().ToString("N"); // fallback nhưng đã set ở trên
                if (status == nameof(AttendanceStatus.REGISTERED))
                {
                    _ = Task.Run(async () =>
                    {
                        try { await _emailService.SendEventRegistrationSuccessAsync(user.Email, user.FullName, eventEntity.EventName, eventEntity.StartDate, checkInToken); }
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
            var expiresAt = DateTime.UtcNow.AddMinutes(15);

            // Update event
            eventEntity.CheckInCode = code;
            eventEntity.CodeExpiresAt = expiresAt;

            _unitOfWork.Events.Update(eventEntity);
            await _unitOfWork.SaveChangesAsync();

            // Return DTO with QR content
            return _mapper.Map<CheckInCodeResponse>(eventEntity);
        }

        public async Task<CheckInResult> CheckInMemberAsync(CheckInRequest request)
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
                throw new DomainException("Mã điểm danh không hợp lệ hoặc đã hết hạn");
            }

            // Validate code expiration
            if (!eventEntity.CodeExpiresAt.HasValue || eventEntity.CodeExpiresAt.Value < DateTime.UtcNow)
            {
                throw new DomainException("Mã điểm danh đã hết hạn");
            }

            // Find attendance record
            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(request.EventId, request.UserId);
            if (attendance == null)
            {
                throw new NotFoundException("Không tìm thấy bản ghi điểm danh. Người dùng phải đăng ký sự kiện trước.");
            }

            // Idempotent: if already PRESENT, return without updating
            if (attendance.AttendanceStatus == nameof(AttendanceStatus.PRESENT)
                || attendance.AttendanceStatus == nameof(AttendanceStatus.CHECKED_IN))
            {
                return new CheckInResult
                {
                    Success = true,
                    Message = "Đã điểm danh trước đó.",
                    AlreadyCheckedIn = true
                };
            }

            // Update attendance status
            attendance.AttendanceStatus = nameof(AttendanceStatus.PRESENT);
            attendance.CheckInTime = DateTime.UtcNow;

            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();

            return new CheckInResult
            {
                Success = true,
                Message = "Điểm danh thành công!",
                AlreadyCheckedIn = false
            };
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
                attendance.CheckInTime = DateTime.UtcNow;
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
                attendance.CheckInTime = DateTime.UtcNow;
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
            if (!eventEntity.EndDate.HasValue || eventEntity.EndDate.Value > DateTime.UtcNow)
            {
                throw new DomainException("Chỉ có thể đánh giá sau khi sự kiện kết thúc");
            }

            // Find attendance record
            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(request.EventId, request.UserId);
            if (attendance == null)
            {
                throw new NotFoundException("Không tìm thấy bản ghi điểm danh");
            }

            // Validate member status is PRESENT (or legacy CHECKED_IN)
            if (attendance.AttendanceStatus != nameof(AttendanceStatus.PRESENT)
                && attendance.AttendanceStatus != nameof(AttendanceStatus.CHECKED_IN))
            {
                throw new DomainException($"Không thể đánh giá thành viên có trạng thái '{attendance.AttendanceStatus}'. Thành viên phải đã tham gia sự kiện.");
            }

            // Update score and comment
            attendance.Score = request.Score;
            attendance.Comment = request.Comment;

            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<AttendanceDetailDto>> GetEventAttendeesAsync(int eventId)
        {
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
            if (eventEntity == null)
                throw new NotFoundException("Event", eventId);

            var attendances = await _unitOfWork.Attendances.GetAttendeesByEventAsync(eventId);
            return _mapper.Map<IEnumerable<AttendanceDetailDto>>(attendances);
        }

        public async Task<AttendeePagedResult> GetEventAttendeesAsync(int eventId, string? statusFilter, int page = 1, int pageSize = 50)
        {
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
            if (eventEntity == null)
                throw new NotFoundException("Event", eventId);

            var (items, totalCount) = await _unitOfWork.Attendances.GetAttendeesByEventAsync(eventId, statusFilter, page, pageSize);
            return new AttendeePagedResult
            {
                Items = _mapper.Map<IEnumerable<AttendanceDetailDto>>(items),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task ApproveRegistrationAsync(int eventId, Guid userId)
        {
            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, userId);
            if (attendance == null) throw new NotFoundException("Attendance", userId);

            if (attendance.AttendanceStatus != nameof(AttendanceStatus.PENDING) && attendance.AttendanceStatus != nameof(AttendanceStatus.WAITLIST))
                throw new DomainException($"Cannot approve registration with status '{attendance.AttendanceStatus}'");

            // Option B: trừ slot khi Approve, không phải khi Register
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
            if (eventEntity?.MaxAttendees.HasValue == true)
            {
                // Recalc AvailableSlots trước — đảm bảo DB luôn đúng
                await RecalcAvailableSlotsAsync(eventId, eventEntity);
                bool gotSlot = await _unitOfWork.Events.TryDecrementSlotAsync(eventId);
                if (!gotSlot)
                    throw new DomainException("Không còn slot trống. Không thể duyệt thêm.");
            }

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

        public async Task<BulkApproveResult> BulkApproveAsync(int eventId, List<Guid> userIds)
        {
            await using var txn = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
                int approved = 0;
                int skippedDueToSlot = 0;

                // Recalc AvailableSlots trước khi duyệt — đảm bảo DB luôn đúng
                if (eventEntity?.MaxAttendees.HasValue == true)
                {
                    await RecalcAvailableSlotsAsync(eventId, eventEntity);
                }

                foreach (var userId in userIds)
                {
                    var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, userId);
                    if (attendance == null) continue;
                    if (attendance.AttendanceStatus != nameof(AttendanceStatus.PENDING)
                        && attendance.AttendanceStatus != nameof(AttendanceStatus.WAITLIST))
                        continue;

                    // Option B: trừ slot khi Approve
                    if (eventEntity?.MaxAttendees.HasValue == true)
                    {
                        bool gotSlot = await _unitOfWork.Events.TryDecrementSlotAsync(eventId);
                        if (!gotSlot)
                        {
                            skippedDueToSlot++;
                            continue; // skip thay vì break, để đếm tổng
                        }
                    }

                    attendance.AttendanceStatus = nameof(AttendanceStatus.REGISTERED);
                    _unitOfWork.Attendances.Update(attendance);
                    approved++;

                    // Gửi email (fire-and-forget, ngoài txn)
                    var user = attendance.User;
                    var ev = attendance.Event;
                    var token = attendance.CheckInToken;
                    if (user != null && ev != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            try { await _emailService.SendEventRegistrationSuccessAsync(user.Email, user.FullName, ev.EventName, ev.StartDate, token); }
                            catch (Exception ex) { Console.WriteLine($"[Email] BulkApprove email failed: {ex.Message}"); }
                        });
                    }
                }

                if (approved > 0) await _unitOfWork.SaveChangesAsync();
                await txn.CommitAsync();

                // Re-fetch remaining slots
                var updatedEvent = await _unitOfWork.Events.GetByIdAsync(eventId);
                var remaining = updatedEvent?.AvailableSlots ?? 0;

                var message = skippedDueToSlot > 0
                    ? $"Đã duyệt {approved}/{userIds.Count} đăng ký. {skippedDueToSlot} bị bỏ qua do hết slot."
                    : $"Đã duyệt {approved}/{userIds.Count} đăng ký.";

                return new BulkApproveResult
                {
                    Approved = approved,
                    Total = userIds.Count,
                    SkippedDueToSlot = skippedDueToSlot,
                    RemainingSlots = remaining,
                    Message = message,
                };
            }
            catch
            {
                await txn.RollbackAsync();
                throw;
            }
        }

        public async Task RejectRegistrationAsync(int eventId, Guid userId)
        {
            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, userId);
            if (attendance == null) throw new NotFoundException("Attendance", userId);

            var oldStatus = attendance.AttendanceStatus;
            Console.WriteLine($"[RejectRegistration] Event {eventId}, User {userId}, OldStatus={oldStatus}");

            if (oldStatus != nameof(AttendanceStatus.PENDING) && oldStatus != nameof(AttendanceStatus.WAITLIST)
                && oldStatus != nameof(AttendanceStatus.REGISTERED))
                throw new DomainException($"Không thể từ chối đăng ký có trạng thái '{oldStatus}'.");

            attendance.AttendanceStatus = nameof(AttendanceStatus.REJECTED);
            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();
            Console.WriteLine($"[RejectRegistration] Saved REJECTED status for User {userId}");

            // Gửi email thông báo từ chối
            _ = Task.Run(async () =>
            {
                try
                {
                    var user = attendance.User;
                    var ev = attendance.Event;
                    if (user != null && ev != null)
                        await _emailService.SendRegistrationRejectedAsync(user.Email, user.FullName, ev.EventName);
                }
                catch (Exception ex) { Console.WriteLine($"[Email] RejectRegistration email failed: {ex.Message}"); }
            });

            // Chỉ nhả slot nếu đã REGISTERED (PENDING/WAITLIST không chiếm slot)
            bool wasOccupying = oldStatus == nameof(AttendanceStatus.REGISTERED);
            Console.WriteLine($"[RejectRegistration] wasOccupying={wasOccupying}");
            if (wasOccupying)
            {
                var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
                Console.WriteLine($"[RejectRegistration] Event MaxAttendees={eventEntity?.MaxAttendees}, AvailableSlots={eventEntity?.AvailableSlots}");
                if (eventEntity != null)
                    await HandleSlotReleaseAsync(eventId, eventEntity.RequiresApproval);
            }
        }

        // CancelRegistrationAsync — user tự huỷ
        public async Task CancelRegistrationAsync(int eventId, Guid userId)
        {
            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, userId);
            if (attendance == null) throw new NotFoundException("Attendance", userId);

            var oldStatus = attendance.AttendanceStatus;
            if (oldStatus == nameof(AttendanceStatus.CANCELLED) || oldStatus == nameof(AttendanceStatus.REJECTED))
                throw new DomainException("Đã huỷ trước đó.");

            attendance.AttendanceStatus = nameof(AttendanceStatus.CANCELLED);
            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();

            bool wasOccupying = oldStatus == nameof(AttendanceStatus.REGISTERED)
                             || oldStatus == nameof(AttendanceStatus.CHECKED_IN)
                             || oldStatus == nameof(AttendanceStatus.PRESENT);
            if (wasOccupying)
            {
                var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
                if (eventEntity != null)
                    await HandleSlotReleaseAsync(eventId, eventEntity.RequiresApproval);
            }
        }

        // Giai đoạn 3: Atomic Direct-Promote — không bao giờ nhả slot vào free pool
        // Khi slot được nhả (cancel/reject), luôn promote WAITLIST → REGISTERED
        // (bất kể RequiresApproval — vì Manager đã chủ động tạo chỗ trống)
        private async Task HandleSlotReleaseAsync(int eventId, bool requiresApproval)
        {
            // Kiểm tra event có giới hạn không (AvailableSlots null = không giới hạn)
            var ev = await _unitOfWork.Events.GetByIdAsync(eventId);
            Console.WriteLine($"[HandleSlotRelease] Event {eventId}, MaxAttendees={ev?.MaxAttendees}, AvailableSlots={ev?.AvailableSlots}");
            if (ev?.MaxAttendees == null) return;

            // Luôn promote thẳng → REGISTERED (không cần duyệt lại)
            bool promoted = await _unitOfWork.Events
                .TryDirectPromoteOldestWaitlistAsync(eventId, nameof(AttendanceStatus.REGISTERED));
            Console.WriteLine($"[HandleSlotRelease] TryDirectPromote result: {promoted}");

            if (!promoted)
            {
                // Không có ai trong WAITLIST → trả slot về pool
                await _unitOfWork.Events.IncrementSlotAsync(eventId);
            }
        }

        /// <summary>
        /// Manual status transition — only allows safe transitions that don't involve slot logic.
        /// For slot-affecting transitions (→REGISTERED), use Approve/Reject endpoints.
        /// Allowed: PENDING → WAITLIST, WAITLIST → PENDING
        /// </summary>
        public async Task UpdateAttendeeStatusAsync(int eventId, Guid userId, string newStatus)
        {
            var attendance = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, userId);
            if (attendance == null)
                throw new NotFoundException("Attendance", userId);

            var current = attendance.AttendanceStatus;
            var normalizedNew = newStatus?.Trim().ToUpperInvariant() ?? "";

            // Define allowed manual transitions (no slot side-effects)
            var allowed = new Dictionary<string, string[]>
            {
                { nameof(AttendanceStatus.PENDING), new[] { nameof(AttendanceStatus.WAITLIST) } },
                { nameof(AttendanceStatus.WAITLIST), new[] { nameof(AttendanceStatus.PENDING) } },
            };

            if (!allowed.ContainsKey(current) || !allowed[current].Contains(normalizedNew))
            {
                throw new DomainException(
                    $"Không thể chuyển trạng thái từ '{current}' sang '{normalizedNew}'. "
                    + "Để duyệt (→REGISTERED) hoặc từ chối (→REJECTED), sử dụng endpoint Approve/Reject.");
            }

            attendance.AttendanceStatus = normalizedNew;
            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Recalculate AvailableSlots from ground truth (MaxAttendees - actual occupied count).
        /// Prevents stale DB values from causing incorrect slot management.
        /// Only counts statuses that truly occupy a slot: REGISTERED, CHECKED_IN, PRESENT, ABSENT.
        /// </summary>
        private async Task RecalcAvailableSlotsAsync(int eventId, DataAccess.Models.Event eventEntity)
        {
            if (!eventEntity.MaxAttendees.HasValue) return;

            var allAttendances = await _unitOfWork.Attendances.GetAttendeesByEventAsync(eventId);
            var slotOccupyingStatuses = new[] { "REGISTERED", "CHECKED_IN", "PRESENT", "ABSENT" };
            var occupiedSlots = allAttendances.Count(a => slotOccupyingStatuses.Contains(a.AttendanceStatus));
            var correctAvailable = eventEntity.MaxAttendees.Value - occupiedSlots;

            if (correctAvailable < 0) correctAvailable = 0;

            // Chỉ update nếu giá trị DB sai
            if (eventEntity.AvailableSlots != correctAvailable)
            {
                Console.WriteLine($"[RecalcSlots] Event {eventId}: DB AvailableSlots={eventEntity.AvailableSlots} → corrected to {correctAvailable} (max={eventEntity.MaxAttendees}, occupied={occupiedSlots})");
                // Dùng ExecuteUpdate để atomic update, tránh conflict
                await _unitOfWork.Events.SetAvailableSlotsAsync(eventId, correctAvailable);
            }
        }

        private string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        /// <summary>
        /// Manager thêm thành viên CLB vào sự kiện — status REGISTERED ngay, gửi email thông báo.
        /// </summary>
        public async Task<int> AddAttendeesAsync(int eventId, List<Guid> userIds)
        {
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
            if (eventEntity == null) throw new NotFoundException("Event", eventId);

            int addedCount = 0;
            // Tính trước số người thực sự cần add (không trùng, không đã active)
            var toAdd = new List<(Guid userId, Attendance? existing)>();

            foreach (var userId in userIds)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null) continue;

                var existing = await _unitOfWork.Attendances.GetByEventAndUserAsync(eventId, userId);
                if (existing != null)
                {
                    var activeStatuses = new[] { "REGISTERED", "PRESENT", "CHECKED_IN", "ABSENT" };
                    if (activeStatuses.Contains(existing.AttendanceStatus))
                        continue; // đã active → skip
                }
                toAdd.Add((userId, existing));
            }

            if (toAdd.Count == 0) return 0;

            // Manager bypass: nếu có giới hạn slot và không đủ chỗ → tự tăng MaxAttendees
            if (eventEntity.MaxAttendees.HasValue)
            {
                var currentSlots = eventEntity.AvailableSlots ?? 0;
                if (currentSlots < toAdd.Count)
                {
                    var shortage = toAdd.Count - currentSlots;
                    eventEntity.MaxAttendees += shortage;
                    eventEntity.AvailableSlots = (eventEntity.AvailableSlots ?? 0) + shortage;
                    _unitOfWork.Events.Update(eventEntity);
                }
            }

            foreach (var (userId, existing) in toAdd)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null) continue;

                string checkInToken;
                if (existing != null)
                {
                    existing.AttendanceStatus = nameof(AttendanceStatus.REGISTERED);
                    existing.RegistrationDate = DateTime.UtcNow;
                    existing.CheckInToken = Guid.NewGuid().ToString("N");
                    existing.CheckInTime = null;
                    checkInToken = existing.CheckInToken;
                    _unitOfWork.Attendances.Update(existing);
                }
                else
                {
                    checkInToken = Guid.NewGuid().ToString("N");
                    var attendance = new Attendance
                    {
                        EventId = eventId,
                        UserId = userId,
                        RegistrationDate = DateTime.UtcNow,
                        AttendanceStatus = nameof(AttendanceStatus.REGISTERED),
                        CheckInToken = checkInToken
                    };
                    await _unitOfWork.Attendances.AddAsync(attendance);
                }

                // Trừ slot
                if (eventEntity.MaxAttendees.HasValue)
                {
                    await _unitOfWork.Events.TryDecrementSlotAsync(eventId);
                }

                addedCount++;

                // Gửi email (fire-and-forget)
                var u = user;
                var ev = eventEntity;
                var token = checkInToken;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendEventRegistrationSuccessAsync(
                            u.Email, u.FullName, ev.EventName, ev.StartDate, token);
                    }
                    catch (Exception ex) { Console.WriteLine($"[Email] AddAttendee email failed: {ex.Message}"); }
                });
            }

            await _unitOfWork.SaveChangesAsync();
            return addedCount;
        }
    }
}
