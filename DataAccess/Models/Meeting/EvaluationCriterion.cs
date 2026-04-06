using System;
using System.Collections.Generic;

namespace DataAccess.Models.Meeting;

/// <summary>
/// Tiêu chí đánh giá ứng viên cho một campaign.
/// Mỗi campaign có bộ tiêu chí riêng (dynamic).
/// Hệ thống tự tạo 5 tiêu chí mặc định khi campaign chưa có.
///
/// CampaignId → RecruitmentCampaigns.CampaignId (FK mềm)
/// </summary>
public class EvaluationCriterion
{
    public int Id { get; set; }

    /// <summary>
    /// RecruitmentCampaigns.CampaignId – Campaign sở hữu tiêu chí này.
    /// </summary>
    public int CampaignId { get; set; }

    /// <summary>
    /// Tên tiêu chí. VD: "Kiến thức chuyên môn", "Kỹ năng giao tiếp"
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Mô tả chi tiết tiêu chí.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Trọng số (phần trăm). VD: 25 = 25%.
    /// Tổng trọng số các tiêu chí trong campaign phải = 100.
    /// </summary>
    public int Weight { get; set; }

    /// <summary>
    /// Tiêu chí mặc định hay do admin tạo thêm.
    /// </summary>
    public bool IsDefault { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<CriteriaScore> CriteriaScores { get; set; } = new List<CriteriaScore>();
}
