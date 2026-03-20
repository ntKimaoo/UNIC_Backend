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
        Task<VerifyByLinkResult> VerifyAttendanceByLinkAsync(string? email, string code);
        Task EvaluateMemberAsync(EvaluateMemberRequest request);
        Task<IEnumerable<AttendanceDetailDto>> GetEventAttendeesAsync(int eventId);
    }
}
