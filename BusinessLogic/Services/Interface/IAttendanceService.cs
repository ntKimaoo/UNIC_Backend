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
        Task CheckInMemberAsync(CheckInRequest request);
        Task<CheckInQrResponse?> GetMyCheckInQrAsync(int eventId, Guid userId);
        Task<CheckInByQrResponse> CheckInByQrTokenAsync(int eventId, string token);
        Task EvaluateMemberAsync(EvaluateMemberRequest request);
        Task<IEnumerable<AttendanceDetailDto>> GetEventAttendeesAsync(int eventId);
        Task ApproveRegistrationAsync(int eventId, Guid userId);
        Task RejectRegistrationAsync(int eventId, Guid userId);
        Task CancelRegistrationAsync(int eventId, Guid userId);
    }
}
