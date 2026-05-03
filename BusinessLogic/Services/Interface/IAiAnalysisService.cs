using BusinessLogic.DTOs;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IAiAnalysisService
    {
        /// <summary>
        /// Phân tích AI toàn bộ ứng viên trong campaign. (Chỉ lấy kết quả đã lưu trong DB)
        /// </summary>
        Task<AiCampaignAnalysisResponseDto> AnalyzeCampaignCandidatesAsync(int campaignId);

        /// <summary>
        /// Scan các ứng viên (Status = Completed) chưa được phân tích, gọi AI để tạo phân tích mới rồi lưu vào DB.
        /// </summary>
        Task<AiCampaignAnalysisResponseDto> GenerateAiAnalysisAsync(int campaignId);

        /// <summary>
        /// Tìm kiếm ứng viên bằng ngôn ngữ tự nhiên.
        /// VD: "ứng viên giỏi code nhất", "ai phù hợp nhất cho backend"
        /// </summary>
        Task<AiSearchResponseDto> SearchCandidatesAsync(int campaignId, AiSearchRequestDto dto);
    }
}
