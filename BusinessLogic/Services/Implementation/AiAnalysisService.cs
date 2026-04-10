using BusinessLogic.DTOs;
using BusinessLogic.Options;
using BusinessLogic.Services.Interface;
using DataAccess.Models.Meeting;
using DataAccess.Models.Meeting.Enums;
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
using UNIC.BusinessLogic.DTOs;

namespace BusinessLogic.Services.Implementation
{
    public class AiAnalysisService : IAiAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly IInterviewRepository _repo;
        private readonly GeminiOptions _options;
        private readonly ILogger<AiAnalysisService> _logger;
        private readonly IUserRepository _userRepo;

        private const int BatchSize = 10;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public AiAnalysisService(
            HttpClient httpClient,
            IInterviewRepository repo,
            IOptions<GeminiOptions> options,
            ILogger<AiAnalysisService> logger,
            IUserRepository userRepo)
        {
            _httpClient = httpClient;
            _repo = repo;
            _options = options.Value;
            _logger = logger;
            _userRepo = userRepo;
        }

        public async Task<AiCampaignAnalysisResponseDto> AnalyzeCampaignCandidatesAsync(int campaignId)
        {
            var dbResults = await _repo.GetAiAnalysisResultsByCampaignIdAsync(campaignId);

            var candidates = dbResults.Select(r => new AiCandidateAnalysisDto
            {
                InterviewScheduleId = r.InterviewScheduleId,
                CandidateUserId = r.CandidateUserId,
                CandidateName = "Ứng viên (Lấy từ Frontend)", // Frontend will map names since DB doesn't store CandidateName or maybe we should fetch it? Actually, wait. User query returns Guid. Let's fetch the name.
                Result = r.Result,
                CriteriaEvaluations = JsonSerializer.Deserialize<List<AiCriteriaEvaluationDto>>(r.CriteriaEvaluationsJson, JsonOpts) ?? new(),
                Strengths = JsonSerializer.Deserialize<List<string>>(r.StrengthsJson, JsonOpts) ?? new(),
                Weaknesses = JsonSerializer.Deserialize<List<string>>(r.WeaknessesJson, JsonOpts) ?? new()
            }).ToList();

            // Populate names
            foreach (var cand in candidates)
            {
                var user = await _userRepo.GetByIdAsync(cand.CandidateUserId);
                if (user != null)
                {
                    cand.CandidateName = user.FullName;
                }
            }

            return new AiCampaignAnalysisResponseDto
            {
                CampaignId = campaignId,
                AnalyzedAt = DateTime.UtcNow.ToString("o"),
                Candidates = candidates
            };
        }

        public async Task<AiCampaignAnalysisResponseDto> GenerateAiAnalysisAsync(int campaignId)
        {
            var allSchedules = (await _repo.GetSchedulesAsync(campaignId, null, null, null)).ToList();
            var criteria = (await _repo.GetCriteriaByCampaignIdAsync(campaignId)).ToList();
            var existingResults = (await _repo.GetAiAnalysisResultsByCampaignIdAsync(campaignId)).ToList();
            var existingScheduleIds = existingResults.Select(r => r.InterviewScheduleId).ToHashSet();

            // Chỉ lấy các schedule đã Completed VÀ CHƯA CÓ TRONG DB
            var completedSchedules = allSchedules
                .Where(s => s.Status == InterviewStatus.Completed && !existingScheduleIds.Contains(s.Id))
                .ToList();

            if (completedSchedules.Count == 0)
            {
                // Nếu không có schedule mới, gọi lại hàm Analyze để lấy danh sách từ DB
                return await AnalyzeCampaignCandidatesAsync(campaignId);
            }

            var candidateDataList = new List<CandidatePromptData>();

            foreach (var schedule in completedSchedules)
            {
                var allNotes = (await _repo.GetCriteriaScoresByScheduleIdAsync(schedule.Id)).ToList();

                if (!allNotes.Any()) continue;

                var criteriaDetails = criteria.Select(c =>
                {
                    var notes = allNotes
                        .Where(s => s.EvaluationCriterionId == c.Id && !string.IsNullOrEmpty(s.Note))
                        .Select(s => s.Note!)
                        .ToList();

                    return new CriterionPromptData
                    {
                        CriterionId = c.Id,
                        CriterionName = c.Name,
                        Notes = notes
                    };
                }).ToList();

                var feedbackNotes = schedule.Assignments
                    .Where(a => !string.IsNullOrEmpty(a.FeedbackNotes))
                    .Select(a => a.FeedbackNotes!)
                    .ToList();

                var interviewerResults = schedule.Assignments
                    .Where(a => a.Result != null)
                    .Select(a => a.Result!.ToString())
                    .ToList();

                var userFind = await _userRepo.GetByIdAsync(schedule.CandidateUserId);
                candidateDataList.Add(new CandidatePromptData
                {
                    InterviewScheduleId = schedule.Id,
                    CandidateUserId = schedule.CandidateUserId.ToString(),
                    CandidateName = userFind!.FullName,
                    CriteriaDetails = criteriaDetails,
                    FeedbackNotes = feedbackNotes,
                    InterviewerResults = interviewerResults!
                });
            }

            if (candidateDataList.Count > 0)
            {
                var criteriaNames = criteria.Select(c => c.Name).ToList();

                try
                {
                    // Chia batch và gọi song song
                    var batches = candidateDataList
                        .Select((c, i) => new { c, i })
                        .GroupBy(x => x.i / BatchSize)
                        .Select(g => g.Select(x => x.c).ToList())
                        .ToList();

                    var batchTasks = batches.Select(batch => ProcessAnalysisBatchAsync(batch, criteriaNames));
                    var batchResults = await Task.WhenAll(batchTasks);

                    var allCandidates = batchResults.SelectMany(r => r).ToList();

                    // Save to DB
                    var newDbResults = allCandidates.Select(c => new AiCandidateAnalysisResult
                    {
                        CampaignId = campaignId,
                        InterviewScheduleId = c.InterviewScheduleId,
                        CandidateUserId = c.CandidateUserId,
                        Result = c.Result,
                        CriteriaEvaluationsJson = JsonSerializer.Serialize(c.CriteriaEvaluations, JsonOpts),
                        StrengthsJson = JsonSerializer.Serialize(c.Strengths, JsonOpts),
                        WeaknessesJson = JsonSerializer.Serialize(c.Weaknesses, JsonOpts),
                        AnalyzedAt = DateTime.UtcNow
                    });

                    await _repo.CreateAiAnalysisResultsAsync(newDbResults);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI Analysis failed for campaign {CampaignId}, using fallback", campaignId);
                    var fallbackResponse = BuildFallbackAnalysis(campaignId, candidateDataList);
                    
                    var fallbackDbResults = fallbackResponse.Candidates.Select(c => new AiCandidateAnalysisResult
                    {
                        CampaignId = campaignId,
                        InterviewScheduleId = c.InterviewScheduleId,
                        CandidateUserId = c.CandidateUserId,
                        Result = c.Result,
                        CriteriaEvaluationsJson = JsonSerializer.Serialize(c.CriteriaEvaluations, JsonOpts),
                        StrengthsJson = JsonSerializer.Serialize(c.Strengths, JsonOpts),
                        WeaknessesJson = JsonSerializer.Serialize(c.Weaknesses, JsonOpts),
                        AnalyzedAt = DateTime.UtcNow
                    });

                    await _repo.CreateAiAnalysisResultsAsync(fallbackDbResults);
                }
            }

            // Cuối cùng return lại tất cả bao gồm cũ và mới từ DB
            return await AnalyzeCampaignCandidatesAsync(campaignId);
        }

        public async Task<AiSearchResponseDto> SearchCandidatesAsync(int campaignId, AiSearchRequestDto dto)
        {
            var allSchedules = (await _repo.GetSchedulesAsync(campaignId, null, null, null)).ToList();
            var criteria = (await _repo.GetCriteriaByCampaignIdAsync(campaignId)).ToList();

            var completedSchedules = allSchedules
                .Where(s => s.Status == InterviewStatus.Completed)
                .ToList();

            if (completedSchedules.Count == 0)
            {
                return new AiSearchResponseDto
                {
                    Query = dto.Query,
                    Results = new(),
                    TotalFound = 0,
                    AiExplanation = "Không có ứng viên nào đã hoàn thành phỏng vấn trong campaign này."
                };
            }

            // Build compact candidate data cho search
            var candidateSummaries = new List<object>();
            foreach (var schedule in completedSchedules)
            {
                var allNotes = (await _repo.GetCriteriaScoresByScheduleIdAsync(schedule.Id)).ToList();
                if (!allNotes.Any()) continue;

                var notes = new Dictionary<string, string>();
                foreach (var c in criteria)
                {
                    var criterionNotes = allNotes
                        .Where(s => s.EvaluationCriterionId == c.Id && !string.IsNullOrEmpty(s.Note))
                        .Select(s => s.Note!)
                        .ToList();
                    if (criterionNotes.Any())
                        notes[c.Name] = string.Join("; ", criterionNotes);
                }

                var fb = schedule.Assignments
                    .Where(a => !string.IsNullOrEmpty(a.FeedbackNotes))
                    .Select(a => a.FeedbackNotes!)
                    .ToList();

                candidateSummaries.Add(new
                {
                    id = schedule.Id,
                    uid = schedule.CandidateUserId.ToString(),
                    name = schedule.Title,
                    notes,
                    fb = fb.Any() ? string.Join("; ", fb) : null
                });
            }

            if (candidateSummaries.Count == 0)
            {
                return new AiSearchResponseDto
                {
                    Query = dto.Query,
                    Results = new(),
                    TotalFound = 0,
                    AiExplanation = "Không có dữ liệu đánh giá nào."
                };
            }

            var prompt = BuildSearchPrompt(dto.Query, candidateSummaries);

            _logger.LogInformation("=== AI SEARCH PROMPT ===\n{Prompt}", prompt);

            try
            {
                var aiResponseText = await CallGeminiAsync(prompt, candidateSummaries.Count);
                _logger.LogInformation("=== AI SEARCH RESPONSE ===\n{Response}", aiResponseText);
                return ParseSearchResponse(aiResponseText, dto.Query);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI Search failed for campaign {CampaignId}, using fallback", campaignId);
                return BuildFallbackSearch(dto.Query, completedSchedules);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Batch Processing
        // ═══════════════════════════════════════════════════════════

        private async Task<List<AiCandidateAnalysisDto>> ProcessAnalysisBatchAsync(
            List<CandidatePromptData> batch, List<string> criteriaNames)
        {
            var prompt = BuildAnalysisPrompt(batch, criteriaNames);

            _logger.LogInformation("=== AI ANALYSIS PROMPT (batch {Count}) ===\n{Prompt}", batch.Count, prompt);

            try
            {
                var aiResponseText = await CallGeminiAsync(prompt, batch.Count);
                _logger.LogInformation("=== AI ANALYSIS RESPONSE (batch {Count}) ===\n{Response}", batch.Count, aiResponseText);
                return ParseAnalysisResponse(aiResponseText, batch);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI batch failed, using fallback for {Count} candidates", batch.Count);
                // Fallback cho batch bị lỗi
                return batch.Select(c => BuildFallbackCandidate(c)).ToList();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Gemini API Call
        // ═══════════════════════════════════════════════════════════

        private async Task<string> CallGeminiAsync(string prompt, int candidateCount = 1)
        {
            // Tăng mạnh giới hạn token. 3 ứng viên có thể cần rất nhiều nếu feedbacks dài.
            var maxTokens = Math.Min(Math.Max(candidateCount * 1500, 3000), 8192);
            
            // Đảm bảo dùng model hợp lệ (ưu tiên config, fallback về 1.5-flash nếu config lạ)
            var modelName = _options.Model;
            if (string.IsNullOrEmpty(modelName) || modelName.Contains("2.5")) modelName = "gemini-3-flash-preview";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = "Chuyên gia HR. Trả JSON thuần, không markdown, không giải thích.\n\n" + prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    maxOutputTokens = maxTokens,
                    responseMimeType = "application/json"
                }
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _options.ApiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error {StatusCode}: {Body}", response.StatusCode, responseText);
                throw new HttpRequestException($"Gemini API returned {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(responseText);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
                throw new InvalidOperationException("Gemini returned empty candidates");

            var firstCandidate = candidates[0];
            
            // Log lý do dừng và dung lượng token đã dùng để debug
            if (firstCandidate.TryGetProperty("finishReason", out var reason))
            {
                _logger.LogInformation("Gemini FinishReason: {Reason}", reason.GetString());
            }
            if (doc.RootElement.TryGetProperty("usageMetadata", out var usage))
            {
                _logger.LogInformation("Gemini Usage: {Usage}", usage.GetRawText());
            }

            var text = firstCandidate
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? throw new InvalidOperationException("Gemini returned null content");
        }

        // ═══════════════════════════════════════════════════════════
        //  Prompt Builders (compact JSON format)
        // ═══════════════════════════════════════════════════════════

        private static string BuildAnalysisPrompt(List<CandidatePromptData> candidates, List<string> criteriaNames)
        {
            var compactData = candidates.Select(c => new
            {
                id = c.InterviewScheduleId,
                uid = c.CandidateUserId,
                name = c.CandidateName,
                criteria = c.CriteriaDetails
                    .Where(cr => cr.Notes.Any())
                    .Select(cr => new
                    {
                        cid = cr.CriterionId,
                        n = cr.CriterionName,
                        notes = string.Join("; ", cr.Notes)
                    }),
                fb = c.FeedbackNotes.Any() ? string.Join("; ", c.FeedbackNotes) : null
            });

            var dataJson = JsonSerializer.Serialize(compactData, JsonOpts);

            return $@"Đánh giá ứng viên. Tiêu chí: {string.Join(", ", criteriaNames)}
Trả JSON: [{{""interviewScheduleId"":n,""candidateUserId"":""s"",""candidateName"":""s"",""result"":""Pass|Fail|Consider"",""criteriaEvaluations"":[{{""criterionId"":n,""criterionName"":""s"",""result"":""Pass|Fail|Hold""}}],""strengths"":[""s""],""weaknesses"":[""s""]}}]
DATA:{dataJson}";
        }

        private static string BuildSearchPrompt(string query, List<object> candidateSummaries)
        {
            var dataJson = JsonSerializer.Serialize(candidateSummaries, JsonOpts);

            return $@"Tìm ứng viên khớp: ""{query}""
Trả JSON: {{""results"":[{{""interviewScheduleId"":n,""candidateUserId"":""s"",""candidateName"":""s"",""matchReason"":""s"",""result"":""Pass|Fail|Consider""}}],""totalFound"":n,""aiExplanation"":""s""}}
DATA:{dataJson}";
        }

        // ═══════════════════════════════════════════════════════════
        //  Response Parsers
        // ═══════════════════════════════════════════════════════════

        private List<AiCandidateAnalysisDto> ParseAnalysisResponse(string aiText, List<CandidatePromptData> originalData)
        {
            try
            {
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
                        Result = item.Result ?? "Consider",
                        CriteriaEvaluations = item.CriteriaEvaluations?.Select(ce => new AiCriteriaEvaluationDto
                        {
                            CriterionId = ce.CriterionId,
                            CriterionName = ce.CriterionName ?? "",
                            Result = NormalizeCriteriaResult(ce.Result)
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
                        MatchReason = r.MatchReason ?? "",
                        Result = r.Result ?? ""
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
            if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                text = text[7..];
            else if (text.StartsWith("```"))
                text = text[3..];

            if (text.EndsWith("```"))
                text = text[..^3];

            return text.Trim();
        }

        private static string NormalizeCriteriaResult(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Hold";
            var val = raw.Trim();
            if (val.Equals("Pass", StringComparison.OrdinalIgnoreCase)) return "Pass";
            if (val.Equals("Fail", StringComparison.OrdinalIgnoreCase)) return "Fail";
            return "Hold";
        }

        // ═══════════════════════════════════════════════════════════
        //  Fallback (khi AI API không khả dụng)
        // ═══════════════════════════════════════════════════════════

        private static AiCandidateAnalysisDto BuildFallbackCandidate(CandidatePromptData c)
        {
            var hasNotes = c.CriteriaDetails.Any(cr => cr.Notes.Any()) || c.FeedbackNotes.Any();
            var interviewerPassed = c.InterviewerResults.Count(r => r == "Pass");
            var interviewerFailed = c.InterviewerResults.Count(r => r == "Fail");

            string fallbackResult;
            if (!hasNotes)
                fallbackResult = "Consider";
            else if (interviewerPassed > interviewerFailed)
                fallbackResult = "Pass";
            else if (interviewerFailed > interviewerPassed)
                fallbackResult = "Fail";
            else
                fallbackResult = "Consider";

            var evaluations = c.CriteriaDetails.Select(cr => new AiCriteriaEvaluationDto
            {
                CriterionId = cr.CriterionId,
                CriterionName = cr.CriterionName,
                Result = cr.Notes.Any() ? "Hold" : "Hold"
            }).ToList();

            return new AiCandidateAnalysisDto
            {
                InterviewScheduleId = c.InterviewScheduleId,
                CandidateUserId = Guid.TryParse(c.CandidateUserId, out var uid) ? uid : Guid.Empty,
                CandidateName = c.CandidateName,
                Result = fallbackResult,
                CriteriaEvaluations = evaluations,
                Strengths = c.FeedbackNotes.Take(2).ToList(),
                Weaknesses = new()
            };
        }

        private AiCampaignAnalysisResponseDto BuildFallbackAnalysis(
            int campaignId,
            List<CandidatePromptData> candidates)
        {
            return new AiCampaignAnalysisResponseDto
            {
                CampaignId = campaignId,
                AnalyzedAt = DateTime.UtcNow.ToString("o"),
                Candidates = candidates.Select(BuildFallbackCandidate).ToList()
            };
        }

        private static AiSearchResponseDto BuildFallbackSearch(string query, List<DataAccess.Models.Meeting.InterviewSchedule> schedules)
        {
            var queryLower = query.ToLowerInvariant();
            var results = schedules
                .Where(s => s.Title.ToLowerInvariant().Contains(queryLower)
                         || s.CandidateUserId.ToString().Contains(queryLower))
                .Select(s => new AiSearchCandidateDto
                {
                    InterviewScheduleId = s.Id,
                    CandidateUserId = s.CandidateUserId,
                    CandidateName = s.Title,
                    MatchReason = "[Fallback] Tìm theo tên — AI không khả dụng.",
                    Result = "Consider"
                })
                .ToList();

            return new AiSearchResponseDto
            {
                Query = query,
                Results = results,
                TotalFound = results.Count,
                AiExplanation = "[Fallback] AI không khả dụng, tìm cơ bản theo tên."
            };
        }
    }
}
