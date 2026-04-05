using BusinessLogic.DTOs;
using BusinessLogic.Options;
using BusinessLogic.Services.Interface;
using DataAccess.Repositories.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementation
{
    public class AiAnalysisService : IAiAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly IInterviewRepository _repo;
        private readonly OpenRouterOptions _options;
        private readonly ILogger<AiAnalysisService> _logger;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public AiAnalysisService(
            HttpClient httpClient,
            IInterviewRepository repo,
            IOptions<OpenRouterOptions> options,
            ILogger<AiAnalysisService> logger)
        {
            _httpClient = httpClient;
            _repo = repo;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<AiCampaignAnalysisResponseDto> AnalyzeCampaignCandidatesAsync(int campaignId)
        {
            // 1. Load all data
            var schedules = (await _repo.GetSchedulesAsync(campaignId, null, null, null)).ToList();
            var criteria = (await _repo.GetCriteriaByCampaignIdAsync(campaignId)).ToList();

            if (schedules.Count == 0)
            {
                return new AiCampaignAnalysisResponseDto
                {
                    CampaignId = campaignId,
                    AnalyzedAt = DateTime.UtcNow.ToString("o"),
                    Candidates = new()
                };
            }

            // 2. Build candidate data for prompt
            var candidateDataList = new List<CandidatePromptData>();

            foreach (var schedule in schedules)
            {
                var allNotes = (await _repo.GetCriteriaScoresByScheduleIdAsync(schedule.Id)).ToList();

                var criteriaDetails = criteria.Select(c =>
                {
                    var notesForCriterion = allNotes.Where(s => s.EvaluationCriterionId == c.Id).ToList();
                    var notes = notesForCriterion
                        .Where(s => !string.IsNullOrEmpty(s.Note))
                        .Select(s => s.Note!)
                        .ToList();

                    return new CriterionPromptData
                    {
                        CriterionId = c.Id,
                        CriterionName = c.Name,
                        Weight = c.Weight,
                        Notes = notes
                    };
                }).ToList();

                // Collect interviewer feedback notes
                var feedbackNotes = schedule.Assignments
                    .Where(a => !string.IsNullOrEmpty(a.FeedbackNotes))
                    .Select(a => a.FeedbackNotes!)
                    .ToList();

                var interviewerResults = schedule.Assignments
                    .Where(a => a.Result != null)
                    .Select(a => a.Result!.ToString())
                    .ToList();

                candidateDataList.Add(new CandidatePromptData
                {
                    InterviewScheduleId = schedule.Id,
                    CandidateUserId = schedule.CandidateUserId.ToString(),
                    CandidateName = schedule.Title,
                    CriteriaDetails = criteriaDetails,
                    FeedbackNotes = feedbackNotes,
                    InterviewerResults = interviewerResults!
                });
            }

            // 3. Build prompt
            var prompt = BuildAnalysisPrompt(candidateDataList, criteria.Select(c => c.Name).ToList());

            // 4. Call AI
            try
            {
                var aiResponseText = await CallOpenRouterAsync(prompt);
                var candidates = ParseAnalysisResponse(aiResponseText, candidateDataList);

                return new AiCampaignAnalysisResponseDto
                {
                    CampaignId = campaignId,
                    AnalyzedAt = DateTime.UtcNow.ToString("o"),
                    Candidates = candidates
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI Analysis failed for campaign {CampaignId}, using fallback", campaignId);
                return BuildFallbackAnalysis(campaignId, candidateDataList, criteria.Select(c => new { c.Id, c.Name, c.Weight }).ToList());
            }
        }

        public async Task<AiSearchResponseDto> SearchCandidatesAsync(int campaignId, AiSearchRequestDto dto)
        {
            // 1. Load data
            var schedules = (await _repo.GetSchedulesAsync(campaignId, null, null, null)).ToList();
            var criteria = (await _repo.GetCriteriaByCampaignIdAsync(campaignId)).ToList();
            var totalWeight = criteria.Sum(c => c.Weight);

            if (schedules.Count == 0)
            {
                return new AiSearchResponseDto
                {
                    Query = dto.Query,
                    Results = new(),
                    TotalFound = 0,
                    AiExplanation = "Không có ứng viên nào trong campaign này."
                };
            }

            // 2. Build candidate summaries for search context
            var candidateSummaries = new List<object>();
            foreach (var schedule in schedules)
            {
                var allNotes = (await _repo.GetCriteriaScoresByScheduleIdAsync(schedule.Id)).ToList();

                var criteriaNotes = new Dictionary<string, List<string>>();
                foreach (var c in criteria)
                {
                    var notesForCriterion = allNotes
                        .Where(s => s.EvaluationCriterionId == c.Id && !string.IsNullOrEmpty(s.Note))
                        .Select(s => s.Note!)
                        .ToList();
                    if (notesForCriterion.Any())
                        criteriaNotes[c.Name] = notesForCriterion;
                }

                var feedbackNotes = schedule.Assignments
                    .Where(a => !string.IsNullOrEmpty(a.FeedbackNotes))
                    .Select(a => a.FeedbackNotes!)
                    .ToList();

                candidateSummaries.Add(new
                {
                    interviewScheduleId = schedule.Id,
                    candidateUserId = schedule.CandidateUserId.ToString(),
                    candidateName = schedule.Title,
                    criteriaNotes,
                    feedbackNotes
                });
            }

            // 3. Build search prompt
            var prompt = BuildSearchPrompt(dto.Query, candidateSummaries);

            // 4. Call AI
            try
            {
                var aiResponseText = await CallOpenRouterAsync(prompt);
                return ParseSearchResponse(aiResponseText, dto.Query);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI Search failed for campaign {CampaignId}, using fallback", campaignId);
                return BuildFallbackSearch(dto.Query, schedules);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  OpenRouter API Call
        // ═══════════════════════════════════════════════════════════

        private async Task<string> CallOpenRouterAsync(string prompt)
        {
            var requestBody = new
            {
                model = _options.Model,
                messages = new[]
                {
                    new { role = "system", content = "You are an expert HR analyst AI. Always respond with valid JSON only, no markdown fences, no explanation text outside JSON." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.3,
                max_tokens = 4000
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");
            request.Headers.Add("HTTP-Referer", "https://uniclub.app");
            request.Headers.Add("X-Title", "UniClub Interview AI");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenRouter API error {StatusCode}: {Body}", response.StatusCode, responseText);
                throw new HttpRequestException($"OpenRouter API returned {response.StatusCode}");
            }

            // Parse OpenRouter response (OpenAI-compatible format)
            using var doc = JsonDocument.Parse(responseText);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
                throw new InvalidOperationException("OpenRouter returned empty choices");

            var messageContent = choices[0].GetProperty("message").GetProperty("content").GetString();
            return messageContent ?? throw new InvalidOperationException("OpenRouter returned null content");
        }

        // ═══════════════════════════════════════════════════════════
        //  Prompt Builders
        // ═══════════════════════════════════════════════════════════

        private static string BuildAnalysisPrompt(List<CandidatePromptData> candidates, List<string> criteriaNames)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Analyze the following interview candidates based on interviewer notes and feedback. Return a JSON array.");
            sb.AppendLine();
            sb.AppendLine($"Evaluation criteria: {string.Join(", ", criteriaNames)}");
            sb.AppendLine();
            sb.AppendLine("For each candidate, provide:");
            sb.AppendLine("- fitLevel: StrongFit, MediumFit, WeakFit, or NoData (based on note sentiment)");
            sb.AppendLine("- suggestedResult: Accept, Reject, Waitlist, or Undecided");
            sb.AppendLine("- summaryText: Brief AI summary in Vietnamese (2-3 sentences)");
            sb.AppendLine("- criteriaSentiments: Array of { criterionId, criterionName, sentiment (positive/negative/neutral), confidence (0-1), explanation }");
            sb.AppendLine("- strengths: Array of strong points in Vietnamese");
            sb.AppendLine("- weaknesses: Array of weak points in Vietnamese");
            sb.AppendLine();
            sb.AppendLine("Return ONLY valid JSON in this exact format:");
            sb.AppendLine("[");
            sb.AppendLine("  {");
            sb.AppendLine("    \"interviewScheduleId\": number,");
            sb.AppendLine("    \"candidateUserId\": \"string\",");
            sb.AppendLine("    \"candidateName\": \"string\",");
            sb.AppendLine("    \"fitLevel\": \"string\",");
            sb.AppendLine("    \"suggestedResult\": \"string\",");
            sb.AppendLine("    \"summaryText\": \"string\",");
            sb.AppendLine("    \"criteriaSentiments\": [{ \"criterionId\": number, \"criterionName\": \"string\", \"sentiment\": \"string\", \"confidence\": number, \"explanation\": \"string\" }],");
            sb.AppendLine("    \"strengths\": [\"string\"],");
            sb.AppendLine("    \"weaknesses\": [\"string\"]");
            sb.AppendLine("  }");
            sb.AppendLine("]");
            sb.AppendLine();
            sb.AppendLine("=== CANDIDATE DATA ===");

            foreach (var c in candidates)
            {
                sb.AppendLine();
                sb.AppendLine($"--- Candidate: {c.CandidateName} (ID: {c.InterviewScheduleId}, UserID: {c.CandidateUserId}) ---");

                foreach (var cr in c.CriteriaDetails)
                {
                    sb.AppendLine($"  Criterion '{cr.CriterionName}' (ID:{cr.CriterionId}, Weight:{cr.Weight}%)");
                    if (cr.Notes.Any())
                        sb.AppendLine($"    Notes: {string.Join(" | ", cr.Notes)}");
                    else
                        sb.AppendLine($"    Notes: (no notes provided)");
                }

                if (c.FeedbackNotes.Any())
                    sb.AppendLine($"  Interviewer Notes: {string.Join(" | ", c.FeedbackNotes)}");

                if (c.InterviewerResults.Any())
                    sb.AppendLine($"  Interviewer Decisions: {string.Join(", ", c.InterviewerResults)}");
            }

            return sb.ToString();
        }

        private static string BuildSearchPrompt(string query, List<object> candidateSummaries)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Given the following candidates data, find the candidates that best match this search query: \"{query}\"");
            sb.AppendLine();
            sb.AppendLine("Return ONLY valid JSON in this exact format:");
            sb.AppendLine("{");
            sb.AppendLine("  \"results\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"interviewScheduleId\": number,");
            sb.AppendLine("      \"candidateUserId\": \"string\",");
            sb.AppendLine("      \"candidateName\": \"string\",");
            sb.AppendLine("      \"relevanceScore\": number (0-1),");
            sb.AppendLine("      \"matchReason\": \"string (in Vietnamese)\",");
            sb.AppendLine("      \"suggestedResult\": \"string\"");
            sb.AppendLine("    }");
            sb.AppendLine("  ],");
            sb.AppendLine("  \"totalFound\": number,");
            sb.AppendLine("  \"aiExplanation\": \"string (in Vietnamese)\"");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("=== CANDIDATES DATA ===");
            sb.AppendLine(JsonSerializer.Serialize(candidateSummaries, JsonOpts));

            return sb.ToString();
        }


        private List<AiCandidateAnalysisDto> ParseAnalysisResponse(string aiText, List<CandidatePromptData> originalData)
        {
            try
            {
                // Clean up: remove markdown code fences if present
                aiText = CleanJsonResponse(aiText);

                var parsed = JsonSerializer.Deserialize<List<AiAnalysisRawItem>>(aiText, JsonOpts);
                if (parsed == null) throw new InvalidOperationException("Parsed null");

                return parsed.Select(item =>
                {
                    return new AiCandidateAnalysisDto
                    {
                        InterviewScheduleId = item.InterviewScheduleId,
                        CandidateUserId = Guid.TryParse(item.CandidateUserId, out var uid) ? uid : Guid.Empty,
                        CandidateName = item.CandidateName ?? "Unknown",
                        FitLevel = item.FitLevel ?? "NoData",
                        SuggestedResult = item.SuggestedResult ?? "Undecided",
                        SummaryText = item.SummaryText ?? "",
                        CriteriaSentiments = item.CriteriaSentiments?.Select(cs => new AiCriteriaSentimentDto
                        {
                            CriterionId = cs.CriterionId,
                            CriterionName = cs.CriterionName ?? "",
                            Sentiment = cs.Sentiment ?? "neutral",
                            Confidence = cs.Confidence,
                            Explanation = cs.Explanation
                        }).ToList() ?? new(),
                        Strengths = item.Strengths ?? new(),
                        Weaknesses = item.Weaknesses ?? new()
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse AI analysis response, raw: {Raw}", aiText?[..Math.Min(500, aiText.Length)]);
                throw;
            }
        }

        private AiSearchResponseDto ParseSearchResponse(string aiText, string query)
        {
            try
            {
                aiText = CleanJsonResponse(aiText);

                var parsed = JsonSerializer.Deserialize<AiSearchRawResponse>(aiText, JsonOpts);
                if (parsed == null) throw new InvalidOperationException("Parsed null");

                return new AiSearchResponseDto
                {
                    Query = query,
                    TotalFound = parsed.TotalFound,
                    AiExplanation = parsed.AiExplanation ?? "",
                    Results = parsed.Results?.Select(r => new AiSearchCandidateDto
                    {
                        InterviewScheduleId = r.InterviewScheduleId,
                        CandidateUserId = Guid.TryParse(r.CandidateUserId, out var uid) ? uid : Guid.Empty,
                        CandidateName = r.CandidateName ?? "Unknown",
                        RelevanceScore = r.RelevanceScore,
                        MatchReason = r.MatchReason ?? "",
                        SuggestedResult = r.SuggestedResult ?? ""
                    }).ToList() ?? new()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse AI search response");
                throw;
            }
        }

        private static string CleanJsonResponse(string text)
        {
            text = text.Trim();
            // Remove markdown code fences
            if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                text = text[7..];
            else if (text.StartsWith("```"))
                text = text[3..];

            if (text.EndsWith("```"))
                text = text[..^3];

            return text.Trim();
        }

        // ═══════════════════════════════════════════════════════════
        //  Fallback (khi AI API không khả dụng)
        // ═══════════════════════════════════════════════════════════

        private AiCampaignAnalysisResponseDto BuildFallbackAnalysis(
            int campaignId,
            List<CandidatePromptData> candidates,
            dynamic criteriaList)
        {
            var result = new AiCampaignAnalysisResponseDto
            {
                CampaignId = campaignId,
                AnalyzedAt = DateTime.UtcNow.ToString("o"),
                Candidates = new()
            };

            foreach (var c in candidates)
            {
                // Determine fit from note content (simple heuristic)
                var hasNotes = c.CriteriaDetails.Any(cr => cr.Notes.Any()) || c.FeedbackNotes.Any();
                var interviewerPassed = c.InterviewerResults.Count(r => r == "Pass");
                var interviewerFailed = c.InterviewerResults.Count(r => r == "Fail");

                string fitLevel;
                string suggested;
                if (!hasNotes)
                {
                    fitLevel = "NoData";
                    suggested = "Undecided";
                }
                else if (interviewerPassed > interviewerFailed)
                {
                    fitLevel = "StrongFit";
                    suggested = "Accept";
                }
                else if (interviewerFailed > interviewerPassed)
                {
                    fitLevel = "WeakFit";
                    suggested = "Reject";
                }
                else
                {
                    fitLevel = "MediumFit";
                    suggested = "Waitlist";
                }

                var sentiments = c.CriteriaDetails.Select(cr => new AiCriteriaSentimentDto
                {
                    CriterionId = cr.CriterionId,
                    CriterionName = cr.CriterionName,
                    Sentiment = cr.Notes.Any() ? "neutral" : "neutral",
                    Confidence = cr.Notes.Any() ? 0.4 : 0,
                    Explanation = cr.Notes.Any()
                        ? $"Nhận xét: {string.Join(" | ", cr.Notes)}"
                        : "(fallback — AI không khả dụng, không có nhận xét)"
                }).ToList();

                result.Candidates.Add(new AiCandidateAnalysisDto
                {
                    InterviewScheduleId = c.InterviewScheduleId,
                    CandidateUserId = Guid.TryParse(c.CandidateUserId, out var uid) ? uid : Guid.Empty,
                    CandidateName = c.CandidateName,
                    FitLevel = fitLevel,
                    SuggestedResult = suggested,
                    SummaryText = "[Fallback] AI model không khả dụng. Dựa trên kết quả của interviewer.",
                    CriteriaSentiments = sentiments,
                    Strengths = c.FeedbackNotes.Take(2).ToList(),
                    Weaknesses = new()
                });
            }

            return result;
        }

        private static AiSearchResponseDto BuildFallbackSearch(string query, List<DataAccess.Models.Meeting.InterviewSchedule> schedules)
        {
            // Simple name-based search fallback
            var queryLower = query.ToLowerInvariant();
            var results = schedules
                .Where(s => s.Title.ToLowerInvariant().Contains(queryLower)
                         || s.CandidateUserId.ToString().Contains(queryLower))
                .Select(s => new AiSearchCandidateDto
                {
                    InterviewScheduleId = s.Id,
                    CandidateUserId = s.CandidateUserId,
                    CandidateName = s.Title,
                    RelevanceScore = 0.5,
                    MatchReason = "[Fallback] Tìm theo tên — AI search không khả dụng.",
                    SuggestedResult = "Undecided"
                })
                .ToList();

            return new AiSearchResponseDto
            {
                Query = query,
                Results = results,
                TotalFound = results.Count,
                AiExplanation = "[Fallback] AI search không khả dụng, tìm kiếm cơ bản theo tên ứng viên."
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  Internal DTOs for parsing AI response
        // ═══════════════════════════════════════════════════════════

        private class AiAnalysisRawItem
        {
            public int InterviewScheduleId { get; set; }
            public string? CandidateUserId { get; set; }
            public string? CandidateName { get; set; }
            public string? FitLevel { get; set; }
            public string? SuggestedResult { get; set; }
            public string? SummaryText { get; set; }
            public List<AiCriteriaSentimentRaw>? CriteriaSentiments { get; set; }
            public List<string>? Strengths { get; set; }
            public List<string>? Weaknesses { get; set; }
        }

        private class AiCriteriaSentimentRaw
        {
            public int CriterionId { get; set; }
            public string? CriterionName { get; set; }
            public string? Sentiment { get; set; }
            public double Confidence { get; set; }
            public string? Explanation { get; set; }
        }

        private class AiSearchRawResponse
        {
            public List<AiSearchRawItem>? Results { get; set; }
            public int TotalFound { get; set; }
            public string? AiExplanation { get; set; }
        }

        private class AiSearchRawItem
        {
            public int InterviewScheduleId { get; set; }
            public string? CandidateUserId { get; set; }
            public string? CandidateName { get; set; }
            public double RelevanceScore { get; set; }
            public string? MatchReason { get; set; }
            public string? SuggestedResult { get; set; }
        }

        // ── Data classes for prompt building ───────────────────────

        private class CandidatePromptData
        {
            public int InterviewScheduleId { get; set; }
            public string CandidateUserId { get; set; } = null!;
            public string CandidateName { get; set; } = null!;
            public List<CriterionPromptData> CriteriaDetails { get; set; } = new();
            public List<string> FeedbackNotes { get; set; } = new();
            public List<string> InterviewerResults { get; set; } = new();
        }

        private class CriterionPromptData
        {
            public int CriterionId { get; set; }
            public string CriterionName { get; set; } = null!;
            public int Weight { get; set; }
            public List<string> Notes { get; set; } = new();
        }
    }
}
