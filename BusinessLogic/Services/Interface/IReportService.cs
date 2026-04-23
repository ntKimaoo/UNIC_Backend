using System;
using System.Threading.Tasks;
using BusinessLogic.DTOs;

namespace BusinessLogic.Services.Interface
{
    public interface IReportService
    {
        Task<ClubReportSummaryDto> GetClubSummaryAsync(int clubId, Guid currentUserId, bool isSystemAdmin, int? year, int? month);
    }
}

