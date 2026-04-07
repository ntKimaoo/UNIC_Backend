using BusinessLogic.DTOs;
using BusinessLogic.Options;
using BusinessLogic.Services.Implementation;
using DataAccess.Models.Meeting;
using DataAccess.Models.Meeting.Enums;
using DataAccess.Repositories.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class AiAnalysisServiceTest
    {
        private readonly Mock<IInterviewRepository> _mockRepo;
        private readonly Mock<ILogger<AiAnalysisService>> _mockLogger;
        private readonly IOptions<OpenRouterOptions> _options;

        public AiAnalysisServiceTest()
        {
            _mockRepo = new Mock<IInterviewRepository>();
            _mockLogger = new Mock<ILogger<AiAnalysisService>>();
            _options = Options.Create(new OpenRouterOptions
            {
                ApiKey = "test-key",
                Model = "test-model",
                BaseUrl = "https://fake.openrouter.ai/api/v1"
            });
        }

        private AiAnalysisService CreateService(HttpClient httpClient)
        {
            return new AiAnalysisService(httpClient, _mockRepo.Object, _options, _mockLogger.Object);
        }

        private HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string responseBody)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                });
            return new HttpClient(handler.Object);
        }

        #region AnalyzeCampaignCandidatesAsync

        [Fact]
        public async Task AnalyzeCampaignCandidatesAsync_ReturnsEmpty_WhenNoSchedules()
        {
            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null))
                     .ReturnsAsync(new List<InterviewSchedule>());
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1))
                     .ReturnsAsync(new List<EvaluationCriterion>());

            var service = CreateService(new HttpClient());

            var result = await service.AnalyzeCampaignCandidatesAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.CampaignId);
            Assert.Empty(result.Candidates);
        }

        [Fact]
        public async Task AnalyzeCampaignCandidatesAsync_ReturnsFallback_WhenAiFails()
        {
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule
                {
                    Id = 1, CandidateUserId = Guid.NewGuid(), Title = "Applicant A",
                    Assignments = new List<InterviewAssignment>
                    {
                        new InterviewAssignment { FeedbackNotes = "Good candidate", Result = InterviewResult.Pass }
                    }
                }
            };
            var criteria = new List<EvaluationCriterion>
            {
                new EvaluationCriterion { Id = 1, Name = "Communication", Weight = 50 }
            };

            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(criteria);
            _mockRepo.Setup(r => r.GetCriteriaScoresByScheduleIdAsync(1)).ReturnsAsync(new List<CriteriaScore>());

            // AI returns error → fallback
            var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "error");
            var service = CreateService(httpClient);

            var result = await service.AnalyzeCampaignCandidatesAsync(1);

            Assert.NotNull(result);
            Assert.Single(result.Candidates);
            Assert.Contains("[Fallback]", result.Candidates[0].SummaryText);
        }

        [Fact]
        public async Task AnalyzeCampaignCandidatesAsync_ReturnsAiResult_WhenSuccess()
        {
            var userId = Guid.NewGuid();
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule
                {
                    Id = 1, CandidateUserId = userId, Title = "Applicant A",
                    Assignments = new List<InterviewAssignment>()
                }
            };
            var criteria = new List<EvaluationCriterion>
            {
                new EvaluationCriterion { Id = 1, Name = "Communication", Weight = 50 }
            };

            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(criteria);
            _mockRepo.Setup(r => r.GetCriteriaScoresByScheduleIdAsync(1)).ReturnsAsync(new List<CriteriaScore>());

            var aiJson = JsonSerializer.Serialize(new[]
            {
                new
                {
                    interviewScheduleId = 1,
                    candidateUserId = userId.ToString(),
                    candidateName = "Applicant A",
                    fitLevel = "StrongFit",
                    suggestedResult = "Accept",
                    summaryText = "Ứng viên tốt",
                    criteriaSentiments = new[] { new { criterionId = 1, criterionName = "Communication", sentiment = "positive", confidence = 0.9, explanation = "Good" } },
                    strengths = new[] { "Giao tiếp tốt" },
                    weaknesses = new string[] { }
                }
            });

            var openRouterResponse = JsonSerializer.Serialize(new
            {
                choices = new[]
                {
                    new { message = new { content = aiJson } }
                }
            });

            var httpClient = CreateMockHttpClient(HttpStatusCode.OK, openRouterResponse);
            var service = CreateService(httpClient);

            var result = await service.AnalyzeCampaignCandidatesAsync(1);

            Assert.NotNull(result);
            Assert.Single(result.Candidates);
            Assert.Equal("StrongFit", result.Candidates[0].FitLevel);
            Assert.Equal("Accept", result.Candidates[0].SuggestedResult);
        }

        #endregion

        #region SearchCandidatesAsync

        [Fact]
        public async Task SearchCandidatesAsync_ReturnsEmpty_WhenNoSchedules()
        {
            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null))
                     .ReturnsAsync(new List<InterviewSchedule>());
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1))
                     .ReturnsAsync(new List<EvaluationCriterion>());

            var service = CreateService(new HttpClient());

            var result = await service.SearchCandidatesAsync(1, new AiSearchRequestDto { Query = "test" });

            Assert.NotNull(result);
            Assert.Equal(0, result.TotalFound);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchCandidatesAsync_ReturnsFallback_WhenAiFails()
        {
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule
                {
                    Id = 1, CandidateUserId = Guid.NewGuid(), Title = "John Doe",
                    Assignments = new List<InterviewAssignment>()
                }
            };
            var criteria = new List<EvaluationCriterion>();

            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(criteria);

            var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "error");
            var service = CreateService(httpClient);

            var result = await service.SearchCandidatesAsync(1, new AiSearchRequestDto { Query = "john" });

            Assert.NotNull(result);
            Assert.Contains("[Fallback]", result.AiExplanation);
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchCandidatesAsync_ReturnsAiResult_WhenSuccess()
        {
            var userId = Guid.NewGuid();
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule
                {
                    Id = 1, CandidateUserId = userId, Title = "Alice",
                    Assignments = new List<InterviewAssignment>()
                }
            };
            var criteria = new List<EvaluationCriterion>();

            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(criteria);
            _mockRepo.Setup(r => r.GetCriteriaScoresByScheduleIdAsync(1)).ReturnsAsync(new List<CriteriaScore>());

            var searchJson = JsonSerializer.Serialize(new
            {
                results = new[]
                {
                    new
                    {
                        interviewScheduleId = 1,
                        candidateUserId = userId.ToString(),
                        candidateName = "Alice",
                        relevanceScore = 0.95,
                        matchReason = "Phù hợp yêu cầu",
                        suggestedResult = "Accept"
                    }
                },
                totalFound = 1,
                aiExplanation = "Tìm thấy 1 ứng viên phù hợp."
            });

            var openRouterResponse = JsonSerializer.Serialize(new
            {
                choices = new[] { new { message = new { content = searchJson } } }
            });

            var httpClient = CreateMockHttpClient(HttpStatusCode.OK, openRouterResponse);
            var service = CreateService(httpClient);

            var result = await service.SearchCandidatesAsync(1, new AiSearchRequestDto { Query = "alice" });

            Assert.NotNull(result);
            Assert.Equal(1, result.TotalFound);
            Assert.Single(result.Results);
            Assert.Equal("Alice", result.Results[0].CandidateName);
        }

        #endregion

        #region AnalyzeCampaign_WithNotesAndFeedback

        [Fact]
        public async Task AnalyzeCampaignCandidatesAsync_WithPassResults_ReturnsFallbackStrongFit()
        {
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule
                {
                    Id = 1, CandidateUserId = Guid.NewGuid(), Title = "Winner",
                    Assignments = new List<InterviewAssignment>
                    {
                        new InterviewAssignment { FeedbackNotes = "Excellent", Result = InterviewResult.Pass },
                        new InterviewAssignment { FeedbackNotes = "Good", Result = InterviewResult.Pass }
                    }
                }
            };
            var criteria = new List<EvaluationCriterion>
            {
                new EvaluationCriterion { Id = 1, Name = "Skills", Weight = 50 }
            };

            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(criteria);
            _mockRepo.Setup(r => r.GetCriteriaScoresByScheduleIdAsync(1))
                     .ReturnsAsync(new List<CriteriaScore>
                     {
                         new CriteriaScore { EvaluationCriterionId = 1, Note = "Very strong skills" }
                     });

            var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "error");
            var service = CreateService(httpClient);

            var result = await service.AnalyzeCampaignCandidatesAsync(1);

            Assert.Single(result.Candidates);
            Assert.Equal("StrongFit", result.Candidates[0].FitLevel);
            Assert.Equal("Accept", result.Candidates[0].SuggestedResult);
        }

        [Fact]
        public async Task AnalyzeCampaignCandidatesAsync_WithFailResults_ReturnsFallbackWeakFit()
        {
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule
                {
                    Id = 1, CandidateUserId = Guid.NewGuid(), Title = "Weak",
                    Assignments = new List<InterviewAssignment>
                    {
                        new InterviewAssignment { FeedbackNotes = "Poor", Result = InterviewResult.Fail },
                        new InterviewAssignment { FeedbackNotes = "Not good", Result = InterviewResult.Fail }
                    }
                }
            };
            var criteria = new List<EvaluationCriterion>
            {
                new EvaluationCriterion { Id = 1, Name = "Communication", Weight = 30 }
            };

            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(criteria);
            _mockRepo.Setup(r => r.GetCriteriaScoresByScheduleIdAsync(1))
                     .ReturnsAsync(new List<CriteriaScore>());

            var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "error");
            var service = CreateService(httpClient);

            var result = await service.AnalyzeCampaignCandidatesAsync(1);

            Assert.Equal("WeakFit", result.Candidates[0].FitLevel);
            Assert.Equal("Reject", result.Candidates[0].SuggestedResult);
        }

        [Fact]
        public async Task AnalyzeCampaignCandidatesAsync_WithEqualResults_ReturnsFallbackMediumFit()
        {
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule
                {
                    Id = 1, CandidateUserId = Guid.NewGuid(), Title = "Middle",
                    Assignments = new List<InterviewAssignment>
                    {
                        new InterviewAssignment { FeedbackNotes = "Mixed", Result = InterviewResult.Pass },
                        new InterviewAssignment { FeedbackNotes = "Unclear", Result = InterviewResult.Fail }
                    }
                }
            };
            var criteria = new List<EvaluationCriterion>();

            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(criteria);
            _mockRepo.Setup(r => r.GetCriteriaScoresByScheduleIdAsync(1)).ReturnsAsync(new List<CriteriaScore>());

            var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "error");
            var service = CreateService(httpClient);

            var result = await service.AnalyzeCampaignCandidatesAsync(1);

            Assert.Equal("MediumFit", result.Candidates[0].FitLevel);
            Assert.Equal("Waitlist", result.Candidates[0].SuggestedResult);
        }

        [Fact]
        public async Task AnalyzeCampaignCandidatesAsync_WithNoNotes_ReturnsFallbackNoData()
        {
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule
                {
                    Id = 1, CandidateUserId = Guid.NewGuid(), Title = "Silent",
                    Assignments = new List<InterviewAssignment>() // no feedback
                }
            };
            var criteria = new List<EvaluationCriterion>
            {
                new EvaluationCriterion { Id = 1, Name = "Technical", Weight = 40 }
            };

            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(criteria);
            _mockRepo.Setup(r => r.GetCriteriaScoresByScheduleIdAsync(1)).ReturnsAsync(new List<CriteriaScore>());

            var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "error");
            var service = CreateService(httpClient);

            var result = await service.AnalyzeCampaignCandidatesAsync(1);

            Assert.Equal("NoData", result.Candidates[0].FitLevel);
            Assert.Equal("Undecided", result.Candidates[0].SuggestedResult);
        }

        #endregion

        #region SearchCandidates_WithFeedback

        [Fact]
        public async Task SearchCandidatesAsync_IncludesCriteriaNotes_InContext()
        {
            var userId = Guid.NewGuid();
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule
                {
                    Id = 1, CandidateUserId = userId, Title = "Rich Data",
                    Assignments = new List<InterviewAssignment>
                    {
                        new InterviewAssignment { FeedbackNotes = "Detailed feedback" }
                    }
                }
            };
            var criteria = new List<EvaluationCriterion>
            {
                new EvaluationCriterion { Id = 1, Name = "Technical", Weight = 50 }
            };

            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(criteria);
            _mockRepo.Setup(r => r.GetCriteriaScoresByScheduleIdAsync(1))
                     .ReturnsAsync(new List<CriteriaScore>
                     {
                         new CriteriaScore { EvaluationCriterionId = 1, Note = "Strong technical" }
                     });

            // AI fails → fallback
            var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "error");
            var service = CreateService(httpClient);

            var result = await service.SearchCandidatesAsync(1, new AiSearchRequestDto { Query = "rich" });

            Assert.NotNull(result);
            Assert.Contains("[Fallback]", result.AiExplanation);
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchCandidatesAsync_FallbackNoMatch_ReturnsEmpty()
        {
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule
                {
                    Id = 1, CandidateUserId = Guid.NewGuid(), Title = "Alice",
                    Assignments = new List<InterviewAssignment>()
                }
            };

            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(new List<EvaluationCriterion>());

            var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "error");
            var service = CreateService(httpClient);

            var result = await service.SearchCandidatesAsync(1, new AiSearchRequestDto { Query = "xyz_no_match" });

            Assert.Empty(result.Results);
            Assert.Equal(0, result.TotalFound);
        }

        #endregion
    }
}
