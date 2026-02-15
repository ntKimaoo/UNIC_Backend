using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessLogic.DTOs;
using DataAccess.Models;
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

        public async Task<ApplicationResponseDto> CreateApplicationAsync(CreateApplicationDto request)
        {
            var application = new Application
            {
                FormId = request.FormId,
                UserId = request.UserId,
                SubmissionDate = DateTime.UtcNow,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "PENDING" : request.Status,
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

        public async Task<IEnumerable<ApplicationFormResponseDto>> GetAllFormsAsync()
        {
            var forms = await _applicationRepository.GetAllFormsAsync();
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
    }
}
