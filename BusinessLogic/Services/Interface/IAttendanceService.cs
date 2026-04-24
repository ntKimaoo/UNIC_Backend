using BusinessLogic.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IAttendanceService
    {
        Task RegisterMemberAsync(EventRegistrationRequest request);
        Task<CheckInCodeResponse> GenerateCheckInCodeAsync(int eventId);
        Task<CheckInResult> CheckInMemberAsync(CheckInRequest request);
        Task<CheckInQrResponse?> GetMyCheckInQrAsync(int eventId, Guid userId);
        Task<CheckInByQrResponse> CheckInByQrTokenAsync(int eventId, string token);
        Task<VerifyByLinkResult> VerifyAttendanceByLinkAsync(string? email, string code);
        Task EvaluateMemberAsync(EvaluateMemberRequest request);

        // Original — still useful for internal calls
        Task<IEnumerable<AttendanceDetailDto>> GetEventAttendeesAsync(int eventId);

        // New overload with server-side filter + pagination
        Task<AttendeePagedResult> GetEventAttendeesAsync(int eventId, string? statusFilter, int page = 1, int pageSize = 50);

        Task ApproveRegistrationAsync(int eventId, Guid userId);
        Task RejectRegistrationAsync(int eventId, Guid userId);
        Task<BulkApproveResult> BulkApproveAsync(int eventId, List<Guid> userIds);
        Task CancelRegistrationAsync(int eventId, Guid userId);
        Task<int> AddAttendeesAsync(int eventId, List<Guid> userIds);

        // New: manual status transition (PENDING ↔ WAITLIST)
        Task UpdateAttendeeStatusAsync(int eventId, Guid userId, string newStatus);
    }
}
