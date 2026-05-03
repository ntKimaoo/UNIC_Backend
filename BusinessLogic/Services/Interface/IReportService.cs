using System;
using System.Threading.Tasks;
using BusinessLogic.DTOs;

namespace BusinessLogic.Services.Interface
{
    public interface IReportService
    {
        Task<ReportSummaryResponseDto> GetSummaryAsync(int clubId, Guid currentUserId, bool isSystemAdmin, int? year, int? month);
        Task<ReportAnalyticsResponseDto> GetAnalyticsAsync(int clubId, int? year);
    }
}

