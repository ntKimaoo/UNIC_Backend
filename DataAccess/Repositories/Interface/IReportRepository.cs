using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccess.Models.Meeting.Enums;

namespace DataAccess.Repositories.Interface
{
    public interface IReportRepository
    {
        // ── Club summary ────────────────────────────────────────────
        Task<(int TotalMembers, int ActiveMembers, int TotalRoles, int TotalDepartments)> GetClubBasicStatsAsync(int clubId);
        Task<int> CountEventsInPeriodAsync(int clubId, DateTime fromUtc, DateTime toExclusiveUtc);
        Task<(int Total, int Pending)> CountRefundRequestsInPeriodAsync(int clubId, DateTime fromUtc, DateTime toExclusiveUtc);
        Task<bool> IsSystemAdminAsync(Guid userId);
        Task<int> CountAllClubsAsync();

        // ── Member section ──────────────────────────────────────────
        Task<List<(int DepartmentId, string? DepartmentName, int MemberCount)>> GetMembersByDepartmentAsync(int clubId);
        Task<Dictionary<int, int>> GetMembersJoinedByMonthAsync(int clubId, int year);

        // ── Event section ───────────────────────────────────────────
        Task<int> CountOrganizedEventsAsync(int clubId);
        Task<List<(int EventId, string? EventName, DateTime? StartDate, DateTime? EndDate)>> GetEventsInPeriodAsync(int clubId, DateTime fromUtc, DateTime toExclusiveUtc);
        Task<Dictionary<int, (int Registered, int CheckedIn)>> GetAttendanceByEventIdsAsync(List<int> eventIds);
        Task<(int EventId, string? EventName, int Registered, int CheckedIn)?> GetMostAttendedEventAsync(int clubId);

        // ── Recruitment section ─────────────────────────────────────
        Task<List<int>> GetCampaignIdsForClubAsync(int clubId);
        Task<List<string?>> GetApplicationStatusesInPeriodAsync(List<int> campaignIds, DateTime fromUtc, DateTime toExclusiveUtc);
        Task<List<(int Id, string? Title)>> GetInterviewSchedulesInPeriodAsync(List<int> campaignIds, DateTime fromUtc, DateTime toExclusiveUtc);
        Task<List<(InterviewResult? Result, DateTime? FeedbackSubmittedAt)>> GetInterviewAssignmentResultsAsync(List<int> scheduleIds);

        // ── Announcement section ────────────────────────────────────
        Task<List<Guid>> GetClubMemberUserIdsAsync(int clubId);
        Task<List<(Guid UserId, string? Type, DateTime CreatedAt)>> GetMemberNotificationsInPeriodAsync(List<Guid> userIds, DateTime fromUtc, DateTime toExclusiveUtc);
        Task<List<(Guid UserId, string? Status)>> GetMemberStatusesForClubAsync(int clubId);

        // ── Analytics ───────────────────────────────────────────────
        Task<List<(Guid UserId, string? FullName, int EventsParticipated)>> GetMemberParticipationAsync(int clubId);
        Task<List<(int DepartmentId, string? DepartmentName, int ParticipationCount)>> GetDepartmentActivityAsync(int clubId);
        Task<(int TotalMembers, int ActiveMembers, int InactiveAfter3Months)> GetMemberRetentionStatsAsync(int clubId);
        Task<List<(int EventId, string? EventName, DateTime? StartDate, DateTime? EndDate)>> GetAllEventBasicsAsync(int clubId);
    }
}
