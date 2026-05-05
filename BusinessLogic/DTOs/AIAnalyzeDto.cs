using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNIC.BusinessLogic.DTOs
{
    public class AiAnalysisRawItem
    {
        public int InterviewScheduleId { get; set; }
        public string? CandidateUserId { get; set; }
        public string? CandidateName { get; set; }
        public string? Result { get; set; }
        public List<AiCriteriaEvaluationRaw>? CriteriaEvaluations { get; set; }
        public List<string>? Strengths { get; set; }
        public List<string>? Weaknesses { get; set; }
    }

    public class AiCriteriaEvaluationRaw
    {
        public int CriterionId { get; set; }
        public string? CriterionName { get; set; }
        public string? Result { get; set; }
        public string? Reason { get; set; }
    }

    public class AiSearchRawResponse
    {
        public List<AiSearchRawItem>? Results { get; set; }
        public int TotalFound { get; set; }
        public string? AiExplanation { get; set; }
    }

    public class AiSearchRawItem
    {
        public int InterviewScheduleId { get; set; }
        public string? CandidateUserId { get; set; }
        public string? CandidateName { get; set; }
        public string? MatchReason { get; set; }
        public string? Result { get; set; }
    }


    public class CandidatePromptData
    {
        public int InterviewScheduleId { get; set; }
        public string CandidateUserId { get; set; } = null!;
        public string CandidateName { get; set; } = null!;
        public List<CriterionPromptData> CriteriaDetails { get; set; } = new();
        public List<string> FeedbackNotes { get; set; } = new();
        public List<string> InterviewerResults { get; set; } = new();
    }

    public class CriterionPromptData
    {
        public int CriterionId { get; set; }
        public string CriterionName { get; set; } = null!;
        public List<string> Notes { get; set; } = new();
    }
}
