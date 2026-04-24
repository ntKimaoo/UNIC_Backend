using System;

namespace BusinessLogic.DTOs
{
    public class ClubReportSummaryDto
    {
        public int ClubId { get; set; }

        public int Year { get; set; }
        public int Month { get; set; } // 1-12

        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }

        public int TotalRoles { get; set; }
        public int TotalDepartments { get; set; }

        public int TotalEventsInMonth { get; set; }

        public decimal TotalApprovedIncomeInMonth { get; set; }
        public decimal TotalApprovedExpenseInMonth { get; set; }

        public int TotalFundRefundRequestsInMonth { get; set; }
        public int PendingFundRefundRequestsInMonth { get; set; }

        /// <summary>
        /// Chỉ trả khi user là system admin.
        /// </summary>
        public int? TotalClubs { get; set; }
    }
}

