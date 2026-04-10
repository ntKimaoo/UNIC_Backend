using BusinessLogic.DTOs;
using BusinessLogic.Options;
using BusinessLogic.Services.Implementation;
using DataAccess.Models;
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
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly IOptions<GeminiOptions> _options;

        public AiAnalysisServiceTest()
        {
            _mockRepo = new Mock<IInterviewRepository>();
            _mockLogger = new Mock<ILogger<AiAnalysisService>>();
            _mockUserRepo = new Mock<IUserRepository>();
            _options = Options.Create(new GeminiOptions
            {
                ApiKey = "test-key",
                Model = "gemini-2.0-flash"
            });
        }

        private AiAnalysisService CreateService(HttpClient httpClient)
        {
            return new AiAnalysisService(httpClient, _mockRepo.Object, _options, _mockLogger.Object, _mockUserRepo.Object);
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
            var userId = Guid.NewGuid();
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule
                {
                    Id = 1, CandidateUserId = userId, Title = "Applicant A",
                    Assignments = new List<InterviewAssignment>
                    {
                        new InterviewAssignment { FeedbackNotes = "Good candidate", Result = InterviewResult.Pass }
                    }
                }
            };
            var criteria = new List<EvaluationCriterion>
            {
                new EvaluationCriterion { Id = 1, Name = "Communication" }
            };

            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(criteria);
            _mockRepo.Setup(r => r.GetCriteriaScoresByScheduleIdAsync(1)).ReturnsAsync(new List<CriteriaScore>());
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { FullName = "Applicant A" });

            // AI returns error → fallback
            var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "error");
            var service = CreateService(httpClient);

            var result = await service.AnalyzeCampaignCandidatesAsync(1);

            Assert.NotNull(result);
            Assert.Single(result.Candidates);
            Assert.Equal("Pass", result.Candidates[0].Result);
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
                new EvaluationCriterion { Id = 1, Name = "Communication" }
            };

            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(criteria);
            _mockRepo.Setup(r => r.GetCriteriaScoresByScheduleIdAsync(1)).ReturnsAsync(new List<CriteriaScore>());
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { FullName = "Applicant A" });

            var aiJson = JsonSerializer.Serialize(new[]
            {
                new
                {
                    interviewScheduleId = 1,
                    candidateUserId = userId.ToString(),
                    candidateName = "Applicant A",
                    result = "Pass",
                    criteriaEvaluations = new[] { new { criterionId = 1, criterionName = "Communication", result = "Pass" } },
                    strengths = new[] { "Giao tiếp tốt" },
                    weaknesses = new string[] { }
                }
            });

            var geminiResponse = JsonSerializer.Serialize(new
            {
                candidates = new[]
                {
                    new { content = new { parts = new[] { new { text = aiJson } } } }
                }
            });

            var httpClient = CreateMockHttpClient(HttpStatusCode.OK, geminiResponse);
            var service = CreateService(httpClient);

            var result = await service.AnalyzeCampaignCandidatesAsync(1);

            Assert.NotNull(result);
            Assert.Single(result.Candidates);
            Assert.Equal("Pass", result.Candidates[0].Result);
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
                        matchReason = "Phù hợp yêu cầu",
                        result = "Pass"
                    }
                },
                totalFound = 1,
                aiExplanation = "Tìm thấy 1 ứng viên phù hợp."
            });

            var geminiResponse = JsonSerializer.Serialize(new
            {
                candidates = new[] { new { content = new { parts = new[] { new { text = searchJson } } } } }
            });

            var httpClient = CreateMockHttpClient(HttpStatusCode.OK, geminiResponse);
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
        public async Task AnalyzeCampaignCandidatesAsync_WithPassResults_ReturnsFallbackPass()
        {
            var userId = Guid.NewGuid();
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule
                {
                    Id = 1, CandidateUserId = userId, Title = "Winner",
                    Assignments = new List<InterviewAssignment>
                    {
                        new InterviewAssignment { FeedbackNotes = "Excellent", Result = InterviewResult.Pass },
                        new InterviewAssignment { FeedbackNotes = "Good", Result = InterviewResult.Pass }
                    }
                }
            };
            var criteria = new List<EvaluationCriterion>
            {
                new EvaluationCriterion { Id = 1, Name = "Skills" }
            };

            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(criteria);
            _mockRepo.Setup(r => r.GetCriteriaScoresByScheduleIdAsync(1))
                     .ReturnsAsync(new List<CriteriaScore>
                     {
                         new CriteriaScore { EvaluationCriterionId = 1, Note = "Very strong skills" }
                     });
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { FullName = "Winner" });

            var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "error");
            var service = CreateService(httpClient);

            var result = await service.AnalyzeCampaignCandidatesAsync(1);

            Assert.Single(result.Candidates);
            Assert.Equal("Pass", result.Candidates[0].Result);
        }

        [Fact]
        public async Task AnalyzeCampaignCandidatesAsync_WithFailResults_ReturnsFallbackFail()
        {
            var userId = Guid.NewGuid();
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule
                {
                    Id = 1, CandidateUserId = userId, Title = "Weak",
                    Assignments = new List<InterviewAssignment>
                    {
                        new InterviewAssignment { FeedbackNotes = "Poor", Result = InterviewResult.Fail },
                        new InterviewAssignment { FeedbackNotes = "Not good", Result = InterviewResult.Fail }
                    }
                }
            };
            var criteria = new List<EvaluationCriterion>
            {
                new EvaluationCriterion { Id = 1, Name = "Communication" }
            };

            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(criteria);
            _mockRepo.Setup(r => r.GetCriteriaScoresByScheduleIdAsync(1))
                     .ReturnsAsync(new List<CriteriaScore>());
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { FullName = "Weak" });

            var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "error");
            var service = CreateService(httpClient);

            var result = await service.AnalyzeCampaignCandidatesAsync(1);

            Assert.Equal("Fail", result.Candidates[0].Result);
        }

        [Fact]
        public async Task AnalyzeCampaignCandidatesAsync_WithEqualResults_ReturnsFallbackConsider()
        {
            var userId = Guid.NewGuid();
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule
                {
                    Id = 1, CandidateUserId = userId, Title = "Middle",
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
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { FullName = "Middle" });

            var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "error");
            var service = CreateService(httpClient);

            var result = await service.AnalyzeCampaignCandidatesAsync(1);

            Assert.Equal("Consider", result.Candidates[0].Result);
        }

        [Fact]
        public async Task AnalyzeCampaignCandidatesAsync_WithNoNotes_ReturnsFallbackConsider()
        {
            var userId = Guid.NewGuid();
            var schedules = new List<InterviewSchedule>
            {
                new InterviewSchedule
                {
                    Id = 1, CandidateUserId = userId, Title = "Silent",
                    Assignments = new List<InterviewAssignment>() // no feedback
                }
            };
            var criteria = new List<EvaluationCriterion>
            {
                new EvaluationCriterion { Id = 1, Name = "Technical" }
            };

            _mockRepo.Setup(r => r.GetSchedulesAsync(1, null, null, null)).ReturnsAsync(schedules);
            _mockRepo.Setup(r => r.GetCriteriaByCampaignIdAsync(1)).ReturnsAsync(criteria);
            _mockRepo.Setup(r => r.GetCriteriaScoresByScheduleIdAsync(1)).ReturnsAsync(new List<CriteriaScore>());
            _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { FullName = "Silent" });

            var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "error");
            var service = CreateService(httpClient);

            var result = await service.AnalyzeCampaignCandidatesAsync(1);

            Assert.Equal("Consider", result.Candidates[0].Result);
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
                new EvaluationCriterion { Id = 1, Name = "Technical" }
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
