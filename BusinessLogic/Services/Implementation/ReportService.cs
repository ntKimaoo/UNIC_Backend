using System;
using System.Threading.Tasks;
using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using UNIC.DataAccess.Repositories.Interface;

namespace BusinessLogic.Services.Implementation
{
    public class ReportService : IReportService
    {
        private readonly UnicContext _context;
        private readonly IFundRepository _fundRepository;

        private const string MEMBER_STATUS_ACTIVE = "ACTIVE";

        public ReportService(UnicContext context, IFundRepository fundRepository)
        {
            _context = context;
            _fundRepository = fundRepository;
        }

        public async Task<ClubReportSummaryDto> GetClubSummaryAsync(
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
            var toUtc = nextMonthStartUtc.AddTicks(-1);

            // NOTE: Không chạy các query song song trên cùng DbContext (EF Core không thread-safe).

            var totalMembers = await _context.UserClubRoles
                .AsNoTracking()
                .CountAsync(mb => mb.ClubId == clubId);

            var activeMembers = await _context.UserClubRoles
                .AsNoTracking()
                .CountAsync(mb => mb.ClubId == clubId
                                  && mb.Status != null
                                  && mb.Status.ToUpper() == MEMBER_STATUS_ACTIVE);

            var totalRoles = await _context.ClubRoles
                .AsNoTracking()
                .CountAsync(r => r.ClubId == clubId);

            var totalDepartments = await _context.Departments
                .AsNoTracking()
                .CountAsync(d => d.ClubId == clubId);

            var totalEventsInMonth = await _context.Events
                .AsNoTracking()
                .CountAsync(e =>
                    e.ClubId == clubId
                    && e.StartDate.HasValue
                    && e.StartDate.Value >= fromUtc
                    && e.StartDate.Value < nextMonthStartUtc);

            var refundRequestsInMonth = await _context.FundRefundRequests
                .AsNoTracking()
                .CountAsync(r => r.ClubId == clubId
                                 && r.CreatedAtUtc >= fromUtc
                                 && r.CreatedAtUtc < nextMonthStartUtc);

            var pendingRefundRequestsInMonth = await _context.FundRefundRequests
                .AsNoTracking()
                .CountAsync(r => r.ClubId == clubId
                                 && r.CreatedAtUtc >= fromUtc
                                 && r.CreatedAtUtc < nextMonthStartUtc
                                 && r.Status != null
                                 && r.Status.ToUpper() == "PENDING");

            var fundAgg = await _fundRepository.GetClubFundReportAggregatesAsync(clubId, fromUtc, toUtc);

            int? totalClubs = null;
            if (isSystemAdmin || await IsSystemAdminFromDbAsync(currentUserId))
                totalClubs = await _context.Clubs.AsNoTracking().CountAsync();

            return new ClubReportSummaryDto
            {
                ClubId = clubId,
                Year = y,
                Month = m,
                TotalMembers = totalMembers,
                ActiveMembers = activeMembers,
                TotalRoles = totalRoles,
                TotalDepartments = totalDepartments,
                TotalEventsInMonth = totalEventsInMonth,
                TotalApprovedIncomeInMonth = fundAgg.TotalApprovedIncome,
                TotalApprovedExpenseInMonth = fundAgg.TotalApprovedExpense,
                TotalFundRefundRequestsInMonth = refundRequestsInMonth,
                PendingFundRefundRequestsInMonth = pendingRefundRequestsInMonth,
                TotalClubs = totalClubs
            };
        }

        private async Task<bool> IsSystemAdminFromDbAsync(Guid userId)
        {
            return await _context.UserRoles
                .AsNoTracking()
                .AnyAsync(ur => ur.UserId == userId
                                && ur.RoleName != null
                                && ur.RoleName.ToUpper() == "ADMIN");
        }
    }
}

