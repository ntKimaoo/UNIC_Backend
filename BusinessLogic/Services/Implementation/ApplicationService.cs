using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessLogic.DTOs;
using DataAccess.Models;
using UNIC.BusinessLogic.Constants;
using UNIC.BusinessLogic.DTOs;
using UNIC.BusinessLogic.Services.Interface;
using UNIC.DataAccess.Repositories.Interface;

namespace UNIC.BusinessLogic.Services.Implementation
{
    public class ApplicationService : IApplicationService
    {
        private readonly IApplicationRepository _applicationRepository;

        public ApplicationService(IApplicationRepository applicationRepository)
        {
            _applicationRepository = applicationRepository;
        }

        private ApplicationResponseDto MapToDto(Application application)
        {
            return new ApplicationResponseDto
            {
                ApplicationId = application.ApplicationId,
                FormId = application.FormId,
                UserId = application.UserId,
                SubmissionDate = application.SubmissionDate,
                Status = application.Status,
                ReviewedAt = application.ReviewedAt
            };
        }

        private ApplicationFormResponseDto MapFormToDto(ApplicationForm form)
        {
            return new ApplicationFormResponseDto
            {
                FormId = form.FormId,
                CampaignId = form.CampaignId,
                FormName = form.FormName,
                FormTitle = form.FormTitle,
                Description = form.Description,
                CreatedAt = form.CreatedAt
            };
        }

        private ApplicationQuestionResponseDto MapQuestionToDto(ApplicationQuestion q)
        {
            return new ApplicationQuestionResponseDto
            {
                QuestionId = q.QuestionId,
                FormId = q.FormId,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                IsRequired = q.IsRequired,
                DisplayOrder = q.DisplayOrder
            };
        }

        private static ApplicationAnswerResponseDto MapAnswerToDto(ApplicationAnswer a)
        {
            return new ApplicationAnswerResponseDto
            {
                AnswerId = a.AnswerId,
                ApplicationId = a.ApplicationId,
                QuestionId = a.QuestionId,
                AnswerText = a.AnswerText
            };
        }

        /// <summary>
        /// Validate câu trả lời theo loại câu hỏi: IsRequired và (tuỳ loại) format.
        /// </summary>
        private static void ValidateAnswerForQuestion(ApplicationQuestion question, string? answerText)
        {
            if (question.IsRequired && string.IsNullOrWhiteSpace(answerText))
                throw new ArgumentException($"Câu hỏi bắt buộc (QuestionId: {question.QuestionId}) chưa có câu trả lời.");
        }

        public async Task<ApplicationResponseDto> CreateApplicationAsync(CreateApplicationDto request)
        {
            var application = new Application
            {
                FormId = request.FormId,
                UserId = request.UserId,
                SubmissionDate = DateTime.UtcNow,
                Status = string.IsNullOrWhiteSpace(request.Status) ? ApplicationStatus.Pending : request.Status,
                ReviewedAt = request.ReviewedAt
            };

            var created = await _applicationRepository.CreateAsync(application);
            return MapToDto(created);
        }

        public async Task<bool> DeleteApplicationAsync(int id)
        {
            return await _applicationRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<ApplicationResponseDto>> GetAllApplicationsAsync()
        {
            var applications = await _applicationRepository.GetAllAsync();
            return applications.Select(MapToDto);
        }

        public async Task<ApplicationResponseDto?> GetApplicationByIdAsync(int id)
        {
            var application = await _applicationRepository.GetByIdAsync(id);
            if (application == null) return null;
            return MapToDto(application);
        }

        public async Task<bool> UpdateApplicationAsync(int id, ApplicationResponseDto application)
        {
            var existing = await _applicationRepository.GetByIdAsync(id);
            if (existing == null) return false;
            if (application.ApplicationId != existing.ApplicationId) return false;

            existing.FormId = application.FormId;
            existing.UserId = application.UserId;
            existing.Status = application.Status;
            existing.ReviewedAt = application.ReviewedAt;

            return await _applicationRepository.UpdateAsync(existing);
        }

        public async Task<ApplicationResponseDto?> UpdateApplicationStatusAsync(int id, string status)
        {
            if (!ApplicationStatus.IsValid(status))
                throw new ArgumentException($"Invalid status. Allowed: {string.Join(", ", ApplicationStatus.ValidStatuses)}.");

            var existing = await _applicationRepository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Status = status;
            existing.ReviewedAt = DateTime.UtcNow;
            var updated = await _applicationRepository.UpdateAsync(existing);
            return updated ? MapToDto(existing) : null;
        }

        public async Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByUserAsync(Guid userId)
        {
            var applications = await _applicationRepository.GetByUserIdAsync(userId);
            return applications.Select(MapToDto);
        }

        public async Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByFormAsync(int formId)
        {
            var applications = await _applicationRepository.GetByFormIdAsync(formId);
            return applications.Select(MapToDto);
        }

        public async Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByStatusAsync(string status)
        {
            var applications = await _applicationRepository.GetByStatusAsync(status);
            return applications.Select(MapToDto);
        }

        public async Task<ApplicationResponseDto?> GetApplicationByUserAndFormAsync(Guid userId, int formId)
        {
            var application = await _applicationRepository.GetByUserIdAndFormIdAsync(userId, formId);
            if (application == null) return null;
            return MapToDto(application);
        }

        public async Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByCampaignAsync(int campaignId, string? status = null)
        {
            var applications = await _applicationRepository.GetByCampaignIdAsync(campaignId, status);
            return applications.Select(MapToDto);
        }

        public async Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByClubAsync(int clubId, string? status = null)
        {
            var applications = await _applicationRepository.GetByClubIdAsync(clubId, status);
            return applications.Select(MapToDto);
        }

        public async Task<IEnumerable<ApplicationFormResponseDto>> GetAllFormsAsync()
        {
            var forms = await _applicationRepository.GetAllFormsAsync();
            return forms.Select(MapFormToDto);
        }

        public async Task<IEnumerable<ApplicationFormResponseDto>> GetFormsByCampaignAsync(int campaignId)
        {
            var forms = await _applicationRepository.GetFormsByCampaignIdAsync(campaignId);
            return forms.Select(MapFormToDto);
        }

        public async Task<ApplicationFormResponseDto?> GetFormByIdAsync(int id)
        {
            var form = await _applicationRepository.GetFormByIdAsync(id);
            if (form == null) return null;
            return MapFormToDto(form);
        }

        public async Task<ApplicationFormResponseDto> CreateFormAsync(CreateApplicationFormDto request)
        {
            var form = new ApplicationForm
            {
                CampaignId = request.CampaignId,
                FormName = request.FormName,
                FormTitle = request.FormTitle,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _applicationRepository.CreateFormAsync(form);
            return MapFormToDto(created);
        }

        public async Task<bool> UpdateFormAsync(int id, ApplicationFormResponseDto form)
        {
            var existing = await _applicationRepository.GetFormByIdAsync(id);
            if (existing == null) return false;
            if (form.FormId != existing.FormId) return false;

            existing.CampaignId = form.CampaignId;
            existing.FormName = form.FormName;
            existing.FormTitle = form.FormTitle;
            existing.Description = form.Description;

            return await _applicationRepository.UpdateFormAsync(existing);
        }

        public async Task<bool> DeleteFormAsync(int id)
        {
            return await _applicationRepository.DeleteFormAsync(id);
        }

        public async Task<IEnumerable<ApplicationQuestionResponseDto>> GetQuestionsByFormAsync(int formId)
        {
            var qs = await _applicationRepository.GetQuestionsByFormIdAsync(formId);
            return qs.Select(MapQuestionToDto);
        }

        public async Task<ApplicationQuestionResponseDto?> GetQuestionByIdAsync(int id)
        {
            var q = await _applicationRepository.GetQuestionByIdAsync(id);
            if (q == null) return null;
            return MapQuestionToDto(q);
        }

        public async Task<ApplicationQuestionResponseDto> CreateQuestionAsync(CreateApplicationQuestionDto request)
        {
            var question = new ApplicationQuestion
            {
                FormId = request.FormId,
                QuestionText = request.QuestionText,
                QuestionType = request.QuestionType,
                IsRequired = request.IsRequired,
                DisplayOrder = request.DisplayOrder
            };

            var created = await _applicationRepository.CreateQuestionAsync(question);
            return MapQuestionToDto(created);
        }

        public async Task<bool> UpdateQuestionAsync(int id, ApplicationQuestionResponseDto question)
        {
            var existing = await _applicationRepository.GetQuestionByIdAsync(id);
            if (existing == null) return false;
            if (question.QuestionId != existing.QuestionId) return false;

            existing.FormId = question.FormId;
            existing.QuestionText = question.QuestionText;
            existing.QuestionType = question.QuestionType;
            existing.IsRequired = question.IsRequired;
            existing.DisplayOrder = question.DisplayOrder;

            return await _applicationRepository.UpdateQuestionAsync(existing);
        }

        public async Task<bool> DeleteQuestionAsync(int id)
        {
            return await _applicationRepository.DeleteQuestionAsync(id);
        }

        public async Task<IEnumerable<ApplicationAnswerResponseDto>> GetAnswersByApplicationAsync(int applicationId)
        {
            var answers = await _applicationRepository.GetAnswersByApplicationIdAsync(applicationId);
            return answers.Select(MapAnswerToDto);
        }

        public async Task<ApplicationAnswerResponseDto?> GetAnswerByIdAsync(int answerId)
        {
            var answer = await _applicationRepository.GetAnswerByIdAsync(answerId);
            if (answer == null) return null;
            return MapAnswerToDto(answer);
        }

        public async Task<ApplicationAnswerResponseDto> CreateAnswerAsync(CreateApplicationAnswerDto request)
        {
            var application = await _applicationRepository.GetByIdAsync(request.ApplicationId);
            if (application == null)
                throw new ArgumentException("Application không tồn tại.");

            var question = await _applicationRepository.GetQuestionByIdAsync(request.QuestionId);
            if (question == null)
                throw new ArgumentException("Câu hỏi không tồn tại.");
            if (question.FormId != application.FormId)
                throw new ArgumentException("Câu hỏi không thuộc form của đơn này.");

            ValidateAnswerForQuestion(question, request.AnswerText);

            var answer = new ApplicationAnswer
            {
                ApplicationId = request.ApplicationId,
                QuestionId = request.QuestionId,
                AnswerText = request.AnswerText ?? string.Empty
            };
            var created = await _applicationRepository.CreateAnswerAsync(answer);
            return MapAnswerToDto(created);
        }

        public async Task<ApplicationResponseDto> SubmitApplicationWithAnswersAsync(SubmitApplicationWithAnswersDto request)
        {
            var form = await _applicationRepository.GetFormByIdAsync(request.FormId);
            if (form == null)
                throw new ArgumentException("Form không tồn tại.");

            var questionsOfForm = (await _applicationRepository.GetQuestionsByFormIdAsync(request.FormId)).ToList();
            var questionsById = questionsOfForm.ToDictionary(q => q.QuestionId);

            if (request.Answers != null)
            {
                foreach (var item in request.Answers)
                {
                    if (!questionsById.TryGetValue(item.QuestionId, out var question))
                        throw new ArgumentException($"Câu hỏi (QuestionId: {item.QuestionId}) không thuộc form này.");
                    ValidateAnswerForQuestion(question, item.AnswerText);
                }

                var requiredIds = questionsOfForm.Where(q => q.IsRequired).Select(q => q.QuestionId).ToHashSet();
                var answeredIds = request.Answers.Select(a => a.QuestionId).ToHashSet();
                var missing = requiredIds.Except(answeredIds).ToList();
                if (missing.Any())
                    throw new ArgumentException($"Thiếu câu trả lời bắt buộc cho QuestionId: {string.Join(", ", missing)}.");
            }
            else
            {
                var anyRequired = questionsOfForm.Any(q => q.IsRequired);
                if (anyRequired)
                    throw new ArgumentException("Form có câu hỏi bắt buộc nhưng không có câu trả lời nào.");
            }

            var application = new Application
            {
                FormId = request.FormId,
                UserId = request.UserId,
                SubmissionDate = DateTime.UtcNow,
                Status = ApplicationStatus.Pending
            };
            var createdApp = await _applicationRepository.CreateAsync(application);
            if (request.Answers != null && request.Answers.Any())
            {
                var answers = request.Answers.Select(a => new ApplicationAnswer
                {
                    ApplicationId = createdApp.ApplicationId,
                    QuestionId = a.QuestionId,
                    AnswerText = a.AnswerText ?? string.Empty
                }).ToList();
                await _applicationRepository.CreateAnswersAsync(answers);
            }
            return MapToDto(createdApp);
        }

        public async Task<bool> UpdateAnswerAsync(int id, ApplicationAnswerResponseDto dto)
        {
            var existing = await _applicationRepository.GetAnswerByIdAsync(id);
            if (existing == null) return false;
            if (dto.AnswerId != existing.AnswerId) return false;

            existing.AnswerText = dto.AnswerText ?? string.Empty;
            return await _applicationRepository.UpdateAnswerAsync(existing);
        }

        public async Task<bool> DeleteAnswerAsync(int id)
        {
            return await _applicationRepository.DeleteAnswerAsync(id);
        }
    }
}
