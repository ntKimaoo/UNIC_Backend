using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models.Meeting.Enums;
using DataAccess.Repositories.Interface;
using UNIC.DataAccess.Repositories.Interface;

namespace BusinessLogic.Services.Implementation
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly IFundRepository _fundRepository;

        private const string MEMBER_STATUS_ACTIVE = "ACTIVE";

        public ReportService(IReportRepository reportRepository, IFundRepository fundRepository)
        {
            _reportRepository = reportRepository;
            _fundRepository = fundRepository;
        }

        public async Task<ReportSummaryResponseDto> GetSummaryAsync(
            int clubId,
            Guid currentUserId,
            bool isSystemAdmin,
            int? year,
            int? month)
        {
            var nowUtc = DateTime.UtcNow;
            var y = year ?? nowUtc.Year;
            var m = month ?? nowUtc.Month;
            if (m < 1 || m > 12)
                throw new ArgumentException("month must be between 1 and 12", nameof(month));

            var fromUtc = new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
            var nextMonthStartUtc = fromUtc.AddMonths(1);
            var toUtcInclusive = nextMonthStartUtc.AddTicks(-1);

            var stats = await _reportRepository.GetClubBasicStatsAsync(clubId);
            var totalEventsInMonth = await _reportRepository.CountEventsInPeriodAsync(clubId, fromUtc, nextMonthStartUtc);
            var (refundTotal, refundPending) = await _reportRepository.CountRefundRequestsInPeriodAsync(clubId, fromUtc, nextMonthStartUtc);
            var fundAgg = await _fundRepository.GetClubFundReportAggregatesAsync(clubId, fromUtc, toUtcInclusive);

            var generatedAt = DateTime.UtcNow;

            var clubReport = new ClubReportSummaryDto
            {
                ClubId = clubId,
                Year = y,
                Month = m,
                GeneratedAt = generatedAt,
                TotalMembers = stats.TotalMembers,
                ActiveMembers = stats.ActiveMembers,
                TotalRoles = stats.TotalRoles,
                TotalDepartments = stats.TotalDepartments,
                TotalEventsInMonth = totalEventsInMonth,
                TotalApprovedIncomeInMonth = fundAgg.TotalApprovedIncome,
                TotalApprovedExpenseInMonth = fundAgg.TotalApprovedExpense,
                TotalFundRefundRequestsInMonth = refundTotal,
                PendingFundRefundRequestsInMonth = refundPending
            };

            clubReport.MemberReport = await BuildMemberReportAsync(clubId, y, stats.TotalMembers, stats.ActiveMembers);
            clubReport.EventReport = await BuildEventReportAsync(clubId, fromUtc, nextMonthStartUtc);
            clubReport.RecruitmentInterviewReport = await BuildRecruitmentInterviewReportAsync(clubId, fromUtc, nextMonthStartUtc);
            clubReport.AnnouncementReport = await BuildAnnouncementReportAsync(clubId, fromUtc, nextMonthStartUtc);

            AdminReportSummaryDto? adminReport = null;
            if (isSystemAdmin || await _reportRepository.IsSystemAdminAsync(currentUserId))
            {
                adminReport = new AdminReportSummaryDto
                {
                    Year = y,
                    Month = m,
                    TotalClubs = await _reportRepository.CountAllClubsAsync()
                };
            }

            return new ReportSummaryResponseDto
            {
                ClubReport = clubReport,
                AdminReport = adminReport
            };
        }

        private async Task<MemberReportSectionDto> BuildMemberReportAsync(
            int clubId, int year, int totalMembers, int activeMembers)
        {
            var departmentRaw = await _reportRepository.GetMembersByDepartmentAsync(clubId);
            var membersByDepartment = departmentRaw
                .Select(x => new MemberByDepartmentDto
                {
                    DepartmentId = x.DepartmentId,
                    DepartmentName = x.DepartmentName ?? string.Empty,
                    MemberCount = x.MemberCount
                })
                .ToList();

            var monthlyMap = await _reportRepository.GetMembersJoinedByMonthAsync(clubId, year);
            var newMembersByMonth = Enumerable.Range(1, 12)
                .Select(m => new MonthlyNewMemberDto
                {
                    Year = year,
                    Month = m,
                    NewMembers = monthlyMap.TryGetValue(m, out var count) ? count : 0
                })
                .ToList();

            return new MemberReportSectionDto
            {
                TotalMembers = totalMembers,
                ActiveMembers = activeMembers,
                InactiveMembers = Math.Max(0, totalMembers - activeMembers),
                MembersByDepartment = membersByDepartment,
                NewMembersByMonth = newMembersByMonth
            };
        }

        private async Task<EventReportSectionDto> BuildEventReportAsync(
            int clubId, DateTime fromUtc, DateTime nextMonthStartUtc)
        {
            var totalOrganizedEvents = await _reportRepository.CountOrganizedEventsAsync(clubId);
            var events = await _reportRepository.GetEventsInPeriodAsync(clubId, fromUtc, nextMonthStartUtc);
            var eventIds = events.Select(e => e.EventId).ToList();
            var attendanceMap = await _reportRepository.GetAttendanceByEventIdsAsync(eventIds);

            var eventParticipants = events.Select(e =>
            {
                attendanceMap.TryGetValue(e.EventId, out var att);
                var registered = att.Registered;
                var checkedIn = att.CheckedIn;
                var notCheckedIn = Math.Max(0, registered - checkedIn);
                var rate = registered == 0 ? 0m : Math.Round((decimal)checkedIn * 100m / registered, 2);
                return new EventParticipantReportDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName ?? string.Empty,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    RegisteredCount = registered,
                    CheckInCount = checkedIn,
                    NotCheckedInCount = notCheckedIn,
                    AttendanceRatePercent = rate
                };
            }).OrderByDescending(x => x.RegisteredCount).ToList();

            var totalRegistered = eventParticipants.Sum(x => x.RegisteredCount);
            var totalCheckedIn = eventParticipants.Sum(x => x.CheckInCount);

            var topRaw = await _reportRepository.GetMostAttendedEventAsync(clubId);
            EventParticipantReportDto? mostAttendedEvent = null;
            if (topRaw.HasValue)
            {
                var top = topRaw.Value;
                mostAttendedEvent = new EventParticipantReportDto
                {
                    EventId = top.EventId,
                    EventName = top.EventName ?? string.Empty,
                    RegisteredCount = top.Registered,
                    CheckInCount = top.CheckedIn,
                    NotCheckedInCount = Math.Max(0, top.Registered - top.CheckedIn),
                    AttendanceRatePercent = top.Registered == 0 ? 0m : Math.Round((decimal)top.CheckedIn * 100m / top.Registered, 2)
                };
            }

            return new EventReportSectionDto
            {
                TotalOrganizedEvents = totalOrganizedEvents,
                TotalEventsInMonth = events.Count,
                TotalRegisteredButNotCheckedIn = Math.Max(0, totalRegistered - totalCheckedIn),
                OverallAttendanceRatePercent = totalRegistered == 0 ? 0m : Math.Round((decimal)totalCheckedIn * 100m / totalRegistered, 2),
                MostAttendedEvent = mostAttendedEvent,
                EventParticipants = eventParticipants
            };
        }

        private async Task<RecruitmentInterviewReportSectionDto> BuildRecruitmentInterviewReportAsync(
            int clubId, DateTime fromUtc, DateTime nextMonthStartUtc)
        {
            var campaignIds = await _reportRepository.GetCampaignIdsForClubAsync(clubId);
            if (campaignIds.Count == 0)
                return new RecruitmentInterviewReportSectionDto();

            var statuses = await _reportRepository.GetApplicationStatusesInPeriodAsync(campaignIds, fromUtc, nextMonthStartUtc);
            var totalApplicants = statuses.Count;
            var passCount = statuses.Count(s =>
                !string.IsNullOrWhiteSpace(s)
                && (s.ToUpper().Contains("PASS") || s.ToUpper().Contains("APPROVED")));
            var failCount = statuses.Count(s =>
                !string.IsNullOrWhiteSpace(s)
                && (s.ToUpper().Contains("FAIL") || s.ToUpper().Contains("REJECT")));

            var schedules = await _reportRepository.GetInterviewSchedulesInPeriodAsync(campaignIds, fromUtc, nextMonthStartUtc);
            var applicantsByRound = schedules
                .GroupBy(s => string.IsNullOrWhiteSpace(s.Title) ? "Unknown Round" : s.Title.Trim())
                .Select(g => new RecruitmentApplicantByRoundDto
                {
                    Round = g.Key,
                    ApplicantCount = g.Count()
                })
                .OrderByDescending(x => x.ApplicantCount)
                .ToList();

            var scheduleIds = schedules.Select(s => s.Id).ToList();
            var assignments = await _reportRepository.GetInterviewAssignmentResultsAsync(scheduleIds);
            var evaluationSummary = new InterviewEvaluationSummaryDto
            {
                TotalAssignments = assignments.Count,
                FeedbackSubmittedCount = assignments.Count(a => a.FeedbackSubmittedAt.HasValue),
                PassRecommendationCount = assignments.Count(a => a.Result == InterviewResult.Pass),
                FailRecommendationCount = assignments.Count(a => a.Result == InterviewResult.Fail)
            };

            return new RecruitmentInterviewReportSectionDto
            {
                TotalApplicants = totalApplicants,
                PassCount = passCount,
                FailCount = failCount,
                PassRatePercent = totalApplicants == 0 ? 0m : Math.Round((decimal)passCount * 100m / totalApplicants, 2),
                FailRatePercent = totalApplicants == 0 ? 0m : Math.Round((decimal)failCount * 100m / totalApplicants, 2),
                ApplicantsByRound = applicantsByRound,
                InterviewerEvaluationSummary = evaluationSummary
            };
        }

        private async Task<AnnouncementReportSectionDto> BuildAnnouncementReportAsync(
            int clubId, DateTime fromUtc, DateTime nextMonthStartUtc)
        {
            var memberUserIds = await _reportRepository.GetClubMemberUserIdsAsync(clubId);
            if (memberUserIds.Count == 0)
                return new AnnouncementReportSectionDto();

            var notifications = await _reportRepository.GetMemberNotificationsInPeriodAsync(memberUserIds, fromUtc, nextMonthStartUtc);
            var memberStatuses = await _reportRepository.GetMemberStatusesForClubAsync(clubId);

            var activeRecipientCount = memberStatuses
                .Where(m => m.Status != null && m.Status.ToUpper() == MEMBER_STATUS_ACTIVE)
                .Select(m => m.UserId)
                .Distinct()
                .Count();

            var inactiveRecipientCount = memberStatuses
                .Where(m => m.Status == null || m.Status.ToUpper() != MEMBER_STATUS_ACTIVE)
                .Select(m => m.UserId)
                .Distinct()
                .Count();

            var recipientGroups = new List<AnnouncementRecipientGroupDto>
            {
                new AnnouncementRecipientGroupDto { GroupName = "Active members", Count = activeRecipientCount },
                new AnnouncementRecipientGroupDto { GroupName = "Inactive members", Count = inactiveRecipientCount }
            };

            var byType = notifications
                .GroupBy(n => string.IsNullOrWhiteSpace(n.Type) ? "Unknown" : n.Type.Trim())
                .Select(g => new AnnouncementRecipientGroupDto
                {
                    GroupName = $"Type: {g.Key}",
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();
            recipientGroups.AddRange(byType);

            var sendTimeline = notifications
                .GroupBy(n => DateOnly.FromDateTime(n.CreatedAt.Date))
                .Select(g => new AnnouncementSendTimelineDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(g => g.Date)
                .ToList();

            return new AnnouncementReportSectionDto
            {
                TotalAnnouncements = notifications.Count,
                RecipientGroups = recipientGroups,
                SendTimeline = sendTimeline
            };
        }

        public async Task<ReportAnalyticsResponseDto> GetAnalyticsAsync(int clubId, int? year)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;

            var growthMap = await _reportRepository.GetMembersJoinedByMonthAsync(clubId, targetYear);
            var growthByMonth = Enumerable.Range(1, 12)
                .Select(m => new MembershipGrowthPointDto
                {
                    Month = m,
                    NewMembers = growthMap.TryGetValue(m, out var c) ? c : 0
                })
                .ToList();
            var bestGrowth = growthByMonth
                .OrderByDescending(x => x.NewMembers)
                .ThenBy(x => x.Month)
                .First();

            var participationRaw = await _reportRepository.GetMemberParticipationAsync(clubId);
            var memberParticipation = participationRaw
                .Select(x => new MemberParticipationInsightDto
                {
                    UserId = x.UserId,
                    FullName = x.FullName ?? string.Empty,
                    EventsParticipated = x.EventsParticipated
                })
                .ToList();

            var deptRaw = await _reportRepository.GetDepartmentActivityAsync(clubId);
            var deptActivity = deptRaw
                .Select(x => new DepartmentActivityInsightDto
                {
                    DepartmentId = x.DepartmentId,
                    DepartmentName = x.DepartmentName ?? string.Empty,
                    ParticipationCount = x.ParticipationCount
                })
                .ToList();

            var retention = await _reportRepository.GetMemberRetentionStatsAsync(clubId);

            var events = await _reportRepository.GetAllEventBasicsAsync(clubId);
            var eventIds = events.Select(e => e.EventId).ToList();
            var attendanceMap = await _reportRepository.GetAttendanceByEventIdsAsync(eventIds);

            var eventInsights = events.Select(e =>
            {
                attendanceMap.TryGetValue(e.EventId, out var att);
                var registered = att.Registered;
                var checkedIn = att.CheckedIn;
                return new EventPerformanceInsightDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName ?? string.Empty,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    RegisteredCount = registered,
                    CheckInCount = checkedIn,
                    EngagementRatePercent = registered == 0 ? 0m : Math.Round((decimal)checkedIn * 100m / registered, 2)
                };
            }).ToList();

            var highestEngagement = eventInsights
                .Where(x => x.RegisteredCount > 0)
                .OrderByDescending(x => x.EngagementRatePercent)
                .ThenByDescending(x => x.RegisteredCount)
                .ThenBy(x => x.EventId)
                .FirstOrDefault();

            var lowestParticipation = eventInsights
                .OrderBy(x => x.RegisteredCount)
                .ThenBy(x => x.EventId)
                .FirstOrDefault();

            return new ReportAnalyticsResponseDto
            {
                ClubId = clubId,
                GeneratedAt = DateTime.UtcNow,
                MembershipGrowth = new MembershipGrowthAnalysisDto
                {
                    Year = targetYear,
                    GrowthByMonth = growthByMonth,
                    BestGrowthMonth = bestGrowth.Month,
                    BestGrowthCount = bestGrowth.NewMembers
                },
                Participation = new ParticipationAnalysisDto
                {
                    MostActiveMember = memberParticipation.FirstOrDefault(),
                    MostActiveDepartment = deptActivity.FirstOrDefault()
                },
                Retention = new RetentionAnalysisDto
                {
                    TotalMembers = retention.TotalMembers,
                    ActiveMembers = retention.ActiveMembers,
                    InactiveAfter3Months = retention.InactiveAfter3Months,
                    ActiveRetentionRatePercent = retention.TotalMembers == 0
                        ? 0m
                        : Math.Round((decimal)retention.ActiveMembers * 100m / retention.TotalMembers, 2)
                },
                EventPerformance = new EventPerformanceAnalysisDto
                {
                    HighestEngagementEvent = highestEngagement,
                    LowestParticipationEvent = lowestParticipation
                }
            };
        }
    }
}
