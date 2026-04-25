using System;
using System.Collections.Generic;

namespace BusinessLogic.DTOs
{
    public class MemberByDepartmentDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
    }

    public class MonthlyNewMemberDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int NewMembers { get; set; }
    }

    public class EventParticipantReportDto
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int RegisteredCount { get; set; }
        public int CheckInCount { get; set; }
        public int NotCheckedInCount { get; set; }
        public decimal AttendanceRatePercent { get; set; }
    }

    public class RecruitmentApplicantByRoundDto
    {
        public string Round { get; set; } = string.Empty;
        public int ApplicantCount { get; set; }
    }

    public class InterviewEvaluationSummaryDto
    {
        public int TotalAssignments { get; set; }
        public int FeedbackSubmittedCount { get; set; }
        public int PassRecommendationCount { get; set; }
        public int FailRecommendationCount { get; set; }
    }

    public class AnnouncementRecipientGroupDto
    {
        public string GroupName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class AnnouncementSendTimelineDto
    {
        public DateOnly Date { get; set; }
        public int Count { get; set; }
    }

    public class MemberReportSectionDto
    {
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int InactiveMembers { get; set; }
        public List<MemberByDepartmentDto> MembersByDepartment { get; set; } = new();
        public List<MonthlyNewMemberDto> NewMembersByMonth { get; set; } = new();
    }

    public class EventReportSectionDto
    {
        public int TotalOrganizedEvents { get; set; }
        public int TotalEventsInMonth { get; set; }
        public int TotalRegisteredButNotCheckedIn { get; set; }
        public decimal OverallAttendanceRatePercent { get; set; }
        public EventParticipantReportDto? MostAttendedEvent { get; set; }
        public List<EventParticipantReportDto> EventParticipants { get; set; } = new();
    }

    public class RecruitmentInterviewReportSectionDto
    {
        public int TotalApplicants { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public decimal PassRatePercent { get; set; }
        public decimal FailRatePercent { get; set; }
        public List<RecruitmentApplicantByRoundDto> ApplicantsByRound { get; set; } = new();
        public InterviewEvaluationSummaryDto InterviewerEvaluationSummary { get; set; } = new();
    }

    public class AnnouncementReportSectionDto
    {
        public int TotalAnnouncements { get; set; }
        public List<AnnouncementRecipientGroupDto> RecipientGroups { get; set; } = new();
        public List<AnnouncementSendTimelineDto> SendTimeline { get; set; } = new();
    }

    public class ClubReportSummaryDto
    {
        public int ClubId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; } // 1-12
        public DateTime GeneratedAt { get; set; }

        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int TotalRoles { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalEventsInMonth { get; set; }
        public decimal TotalApprovedIncomeInMonth { get; set; }
        public decimal TotalApprovedExpenseInMonth { get; set; }
        public int TotalFundRefundRequestsInMonth { get; set; }
        public int PendingFundRefundRequestsInMonth { get; set; }

        public MemberReportSectionDto MemberReport { get; set; } = new();
        public EventReportSectionDto EventReport { get; set; } = new();
        public RecruitmentInterviewReportSectionDto RecruitmentInterviewReport { get; set; } = new();
        public AnnouncementReportSectionDto AnnouncementReport { get; set; } = new();
    }

    public class AdminReportSummaryDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalClubs { get; set; }
    }

    public class ReportSummaryResponseDto
    {
        public ClubReportSummaryDto ClubReport { get; set; } = new();
        public AdminReportSummaryDto? AdminReport { get; set; }
    }

    public class MembershipGrowthPointDto
    {
        public int Month { get; set; }
        public int NewMembers { get; set; }
    }

    public class MembershipGrowthAnalysisDto
    {
        public int Year { get; set; }
        public List<MembershipGrowthPointDto> GrowthByMonth { get; set; } = new();
        public int BestGrowthMonth { get; set; }
        public int BestGrowthCount { get; set; }
    }

    public class MemberParticipationInsightDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int EventsParticipated { get; set; }
    }

    public class DepartmentActivityInsightDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int ParticipationCount { get; set; }
    }

    public class ParticipationAnalysisDto
    {
        public MemberParticipationInsightDto? MostActiveMember { get; set; }
        public DepartmentActivityInsightDto? MostActiveDepartment { get; set; }
    }

    public class RetentionAnalysisDto
    {
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int InactiveAfter3Months { get; set; }
        public decimal ActiveRetentionRatePercent { get; set; }
    }

    public class EventPerformanceInsightDto
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int RegisteredCount { get; set; }
        public int CheckInCount { get; set; }
        public decimal EngagementRatePercent { get; set; }
    }

    public class EventPerformanceAnalysisDto
    {
        public EventPerformanceInsightDto? HighestEngagementEvent { get; set; }
        public EventPerformanceInsightDto? LowestParticipationEvent { get; set; }
    }

    public class ReportAnalyticsResponseDto
    {
        public int ClubId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public MembershipGrowthAnalysisDto MembershipGrowth { get; set; } = new();
        public ParticipationAnalysisDto Participation { get; set; } = new();
        public RetentionAnalysisDto Retention { get; set; } = new();
        public EventPerformanceAnalysisDto EventPerformance { get; set; } = new();
    }
}

