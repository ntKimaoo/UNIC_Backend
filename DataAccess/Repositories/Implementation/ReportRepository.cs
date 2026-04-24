using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.Context;
using DataAccess.Models;
using DataAccess.Models.Meeting.Enums;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implementation
{
    public class ReportRepository : IReportRepository
    {
        private readonly UnicContext _context;
        private readonly MeetingDbContext _meetingContext;
        private const string ACTIVE = "ACTIVE";

        public ReportRepository(UnicContext context, MeetingDbContext meetingContext)
        {
            _context = context;
            _meetingContext = meetingContext;
        }

        // ── Club summary ────────────────────────────────────────────

        public async Task<(int TotalMembers, int ActiveMembers, int TotalRoles, int TotalDepartments)> GetClubBasicStatsAsync(int clubId)
        {
            var totalMembers = await _context.UserClubRoles.AsNoTracking()
                .CountAsync(m => m.ClubId == clubId);

            var activeMembers = await _context.UserClubRoles.AsNoTracking()
                .CountAsync(m => m.ClubId == clubId && m.Status != null && m.Status.ToUpper() == ACTIVE);

            var totalRoles = await _context.ClubRoles.AsNoTracking()
                .CountAsync(r => r.ClubId == clubId);

            var totalDepartments = await _context.Departments.AsNoTracking()
                .CountAsync(d => d.ClubId == clubId);

            return (totalMembers, activeMembers, totalRoles, totalDepartments);
        }

        public async Task<int> CountEventsInPeriodAsync(int clubId, DateTime fromUtc, DateTime toExclusiveUtc)
        {
            return await _context.Events.AsNoTracking()
                .CountAsync(e => e.ClubId == clubId
                                 && e.StartDate.HasValue
                                 && e.StartDate.Value >= fromUtc
                                 && e.StartDate.Value < toExclusiveUtc);
        }

        public async Task<(int Total, int Pending)> CountRefundRequestsInPeriodAsync(int clubId, DateTime fromUtc, DateTime toExclusiveUtc)
        {
            var total = await _context.FundRefundRequests.AsNoTracking()
                .CountAsync(r => r.ClubId == clubId
                                 && r.CreatedAtUtc >= fromUtc
                                 && r.CreatedAtUtc < toExclusiveUtc);

            var pending = await _context.FundRefundRequests.AsNoTracking()
                .CountAsync(r => r.ClubId == clubId
                                 && r.CreatedAtUtc >= fromUtc
                                 && r.CreatedAtUtc < toExclusiveUtc
                                 && r.Status != null && r.Status.ToUpper() == "PENDING");

            return (total, pending);
        }

        public async Task<bool> IsSystemAdminAsync(Guid userId)
        {
            return await _context.UserRoles.AsNoTracking()
                .AnyAsync(ur => ur.UserId == userId
                                && ur.RoleName != null
                                && ur.RoleName.ToUpper() == "ADMIN");
        }

        public async Task<int> CountAllClubsAsync()
        {
            return await _context.Clubs.AsNoTracking().CountAsync();
        }

        // ── Member section ──────────────────────────────────────────

        public async Task<List<(int DepartmentId, string? DepartmentName, int MemberCount)>> GetMembersByDepartmentAsync(int clubId)
        {
            var raw = await _context.UserClubRoleDepartments.AsNoTracking()
                .Where(ud => ud.Department.ClubId == clubId)
                .GroupBy(ud => new { ud.DepartmentId, ud.Department.DepartmentName })
                .Select(g => new
                {
                    g.Key.DepartmentId,
                    g.Key.DepartmentName,
                    MemberCount = g.Select(x => x.ClubMemberId).Distinct().Count()
                })
                .OrderByDescending(x => x.MemberCount)
                .ToListAsync();

            return raw.Select(x => (x.DepartmentId, x.DepartmentName, x.MemberCount)).ToList();
        }

        public async Task<Dictionary<int, int>> GetMembersJoinedByMonthAsync(int clubId, int year)
        {
            var rows = await _context.UserClubRoles.AsNoTracking()
                .Where(m => m.ClubId == clubId && m.JoinDate.Year == year)
                .GroupBy(m => m.JoinDate.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            return rows.ToDictionary(x => x.Month, x => x.Count);
        }

        // ── Event section ───────────────────────────────────────────

        public async Task<int> CountOrganizedEventsAsync(int clubId)
        {
            var nowUtc = DateTime.UtcNow;
            return await _context.Events.AsNoTracking()
                .CountAsync(e => e.ClubId == clubId
                                 && ((e.EndDate.HasValue && e.EndDate.Value <= nowUtc)
                                     || (!e.EndDate.HasValue && e.StartDate.HasValue && e.StartDate.Value <= nowUtc)));
        }

        public async Task<List<(int EventId, string? EventName, DateTime? StartDate, DateTime? EndDate)>> GetEventsInPeriodAsync(
            int clubId, DateTime fromUtc, DateTime toExclusiveUtc)
        {
            var raw = await _context.Events.AsNoTracking()
                .Where(e => e.ClubId == clubId
                            && e.StartDate.HasValue
                            && e.StartDate.Value >= fromUtc
                            && e.StartDate.Value < toExclusiveUtc)
                .Select(e => new { e.EventId, e.EventName, e.StartDate, e.EndDate })
                .ToListAsync();

            return raw.Select(e => (e.EventId, (string?)e.EventName, e.StartDate, e.EndDate)).ToList();
        }

        public async Task<Dictionary<int, (int Registered, int CheckedIn)>> GetAttendanceByEventIdsAsync(List<int> eventIds)
        {
            var rows = await _context.Attendances.AsNoTracking()
                .Where(a => eventIds.Contains(a.EventId))
                .GroupBy(a => a.EventId)
                .Select(g => new
                {
                    EventId = g.Key,
                    Registered = g.Count(),
                    CheckedIn = g.Count(a => a.CheckInTime.HasValue
                                             || (a.AttendanceStatus != null && a.AttendanceStatus.ToUpper() == "CHECKED_IN"))
                })
                .ToListAsync();

            return rows.ToDictionary(x => x.EventId, x => (x.Registered, x.CheckedIn));
        }

        public async Task<(int EventId, string? EventName, int Registered, int CheckedIn)?> GetMostAttendedEventAsync(int clubId)
        {
            var raw = await _context.Attendances.AsNoTracking()
                .Where(a => a.Event.ClubId == clubId)
                .GroupBy(a => new { a.EventId, a.Event.EventName })
                .Select(g => new
                {
                    g.Key.EventId,
                    g.Key.EventName,
                    Registered = g.Count(),
                    CheckedIn = g.Count(a => a.CheckInTime.HasValue
                                             || (a.AttendanceStatus != null && a.AttendanceStatus.ToUpper() == "CHECKED_IN"))
                })
                .OrderByDescending(x => x.Registered)
                .FirstOrDefaultAsync();

            if (raw == null) return null;
            return (raw.EventId, raw.EventName, raw.Registered, raw.CheckedIn);
        }

        // ── Recruitment section ─────────────────────────────────────

        public async Task<List<int>> GetCampaignIdsForClubAsync(int clubId)
        {
            return await _context.RecruitmentCampaigns.AsNoTracking()
                .Where(c => c.ClubId == clubId)
                .Select(c => c.CampaignId)
                .ToListAsync();
        }

        public async Task<List<string?>> GetApplicationStatusesInPeriodAsync(
            List<int> campaignIds, DateTime fromUtc, DateTime toExclusiveUtc)
        {
            return await _context.Applications.AsNoTracking()
                .Where(a => a.SubmissionDate >= fromUtc
                            && a.SubmissionDate < toExclusiveUtc
                            && campaignIds.Contains(a.ApplicationForm.CampaignId))
                .Select(a => (string?)a.Status)
                .ToListAsync();
        }

        public async Task<List<(int Id, string? Title)>> GetInterviewSchedulesInPeriodAsync(
            List<int> campaignIds, DateTime fromUtc, DateTime toExclusiveUtc)
        {
            var raw = await _meetingContext.InterviewSchedules.AsNoTracking()
                .Where(s => campaignIds.Contains(s.CampaignId)
                            && s.ScheduledAt >= fromUtc
                            && s.ScheduledAt < toExclusiveUtc)
                .Select(s => new { s.Id, s.Title })
                .ToListAsync();

            return raw.Select(s => (s.Id, (string?)s.Title)).ToList();
        }

        public async Task<List<(InterviewResult? Result, DateTime? FeedbackSubmittedAt)>> GetInterviewAssignmentResultsAsync(
            List<int> scheduleIds)
        {
            var raw = await _meetingContext.InterviewAssignments.AsNoTracking()
                .Where(a => scheduleIds.Contains(a.InterviewScheduleId))
                .Select(a => new { a.Result, a.FeedbackSubmittedAt })
                .ToListAsync();

            return raw.Select(a => (a.Result, a.FeedbackSubmittedAt)).ToList();
        }

        // ── Announcement section ────────────────────────────────────

        public async Task<List<Guid>> GetClubMemberUserIdsAsync(int clubId)
        {
            return await _context.UserClubRoles.AsNoTracking()
                .Where(m => m.ClubId == clubId)
                .Select(m => m.UserId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<(Guid UserId, string? Type, DateTime CreatedAt)>> GetMemberNotificationsInPeriodAsync(
            List<Guid> userIds, DateTime fromUtc, DateTime toExclusiveUtc)
        {
            var raw = await _context.Notifications.AsNoTracking()
                .Where(n => userIds.Contains(n.UserId)
                            && n.CreatedAt >= fromUtc
                            && n.CreatedAt < toExclusiveUtc)
                .Select(n => new { n.UserId, n.Type, n.CreatedAt })
                .ToListAsync();

            return raw.Select(n => (n.UserId, (string?)n.Type, n.CreatedAt)).ToList();
        }

        public async Task<List<(Guid UserId, string? Status)>> GetMemberStatusesForClubAsync(int clubId)
        {
            var raw = await _context.UserClubRoles.AsNoTracking()
                .Where(m => m.ClubId == clubId)
                .Select(m => new { m.UserId, m.Status })
                .ToListAsync();

            return raw.Select(m => (m.UserId, (string?)m.Status)).ToList();
        }

        // ── Analytics ───────────────────────────────────────────────

        public async Task<List<(Guid UserId, string? FullName, int EventsParticipated)>> GetMemberParticipationAsync(int clubId)
        {
            var raw = await _context.Attendances.AsNoTracking()
                .Where(a => a.Event.ClubId == clubId)
                .GroupBy(a => new { a.UserId, a.User.FullName })
                .Select(g => new
                {
                    g.Key.UserId,
                    g.Key.FullName,
                    EventsParticipated = g.Select(x => x.EventId).Distinct().Count()
                })
                .OrderByDescending(x => x.EventsParticipated)
                .ThenBy(x => x.FullName)
                .ToListAsync();

            return raw.Select(x => (x.UserId, (string?)x.FullName, x.EventsParticipated)).ToList();
        }

        public async Task<List<(int DepartmentId, string? DepartmentName, int ParticipationCount)>> GetDepartmentActivityAsync(int clubId)
        {
            var raw = await (
                from ud in _context.UserClubRoleDepartments.AsNoTracking()
                join m in _context.UserClubRoles.AsNoTracking() on ud.ClubMemberId equals m.ClubMemberId
                join a in _context.Attendances.AsNoTracking() on m.UserId equals a.UserId
                join e in _context.Events.AsNoTracking() on a.EventId equals e.EventId
                where ud.Department.ClubId == clubId && e.ClubId == clubId
                select new { ud.DepartmentId, ud.Department.DepartmentName, a.AttendId })
                .GroupBy(x => new { x.DepartmentId, x.DepartmentName })
                .Select(g => new
                {
                    g.Key.DepartmentId,
                    g.Key.DepartmentName,
                    ParticipationCount = g.Select(x => x.AttendId).Distinct().Count()
                })
                .OrderByDescending(x => x.ParticipationCount)
                .ThenBy(x => x.DepartmentName)
                .ToListAsync();

            return raw.Select(x => (x.DepartmentId, (string?)x.DepartmentName, x.ParticipationCount)).ToList();
        }

        public async Task<(int TotalMembers, int ActiveMembers, int InactiveAfter3Months)> GetMemberRetentionStatsAsync(int clubId)
        {
            var totalMembers = await _context.UserClubRoles.AsNoTracking()
                .CountAsync(m => m.ClubId == clubId);

            var activeMembers = await _context.UserClubRoles.AsNoTracking()
                .CountAsync(m => m.ClubId == clubId && m.Status != null && m.Status.ToUpper() == ACTIVE);

            var threshold = DateTime.UtcNow.AddMonths(-3);
            var inactiveAfter3Months = await _context.UserClubRoles.AsNoTracking()
                .CountAsync(m => m.ClubId == clubId
                                 && m.JoinDate <= threshold
                                 && (m.Status == null || m.Status.ToUpper() != ACTIVE));

            return (totalMembers, activeMembers, inactiveAfter3Months);
        }

        public async Task<List<(int EventId, string? EventName, DateTime? StartDate, DateTime? EndDate)>> GetAllEventBasicsAsync(int clubId)
        {
            var raw = await _context.Events.AsNoTracking()
                .Where(e => e.ClubId == clubId)
                .Select(e => new { e.EventId, e.EventName, e.StartDate, e.EndDate })
                .ToListAsync();

            return raw.Select(e => (e.EventId, (string?)e.EventName, e.StartDate, e.EndDate)).ToList();
        }
    }
}
