using BusinessLogic.DTOs;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IAiAnalysisService
    {
        /// <summary>
        /// Phân tích AI toàn bộ ứng viên trong campaign.
        /// Sử dụng dữ liệu feedback + criteria scores để tạo AI summary.
        /// </summary>
        Task<AiCampaignAnalysisResponseDto> AnalyzeCampaignCandidatesAsync(int campaignId);

        /// <summary>
        /// Tìm kiếm ứng viên bằng ngôn ngữ tự nhiên.
        /// VD: "ứng viên giỏi code nhất", "ai phù hợp nhất cho backend"
        /// </summary>
        Task<AiSearchResponseDto> SearchCandidatesAsync(int campaignId, AiSearchRequestDto dto);
    }
}
