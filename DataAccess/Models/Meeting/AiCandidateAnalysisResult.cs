using System;

namespace DataAccess.Models.Meeting;

public class AiCandidateAnalysisResult
{
    public int Id { get; set; }
    
    // Thuộc Campaign nào
    public int CampaignId { get; set; }
    
    // Lịch phỏng vấn cụ thể
    public int InterviewScheduleId { get; set; }
    
    // UserId của Ứng viên
    public Guid CandidateUserId { get; set; }
    
    // Pass | Fail | Consider
    public string Result { get; set; } = "Consider";
    
    // Lưu các bài phân tích dạng JSON string nguyên thuỷ (ưu tiên tốc độ / lưu trữ không cấu trúc)
    // Chứa một List<AiCriteriaEvaluationDto>
    public string CriteriaEvaluationsJson { get; set; } = "[]";
    
    // List<string> JSON
    public string StrengthsJson { get; set; } = "[]";
    
    // List<string> JSON
    public string WeaknessesJson { get; set; } = "[]";
    
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public InterviewSchedule InterviewSchedule { get; set; } = null!;
}
