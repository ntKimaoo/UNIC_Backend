using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogic.DTOs;
using UNIC.BusinessLogic.DTOs;

namespace UNIC.BusinessLogic.Services.Interface
{
    public interface IApplicationService
    {
        Task<IEnumerable<ApplicationResponseDto>> GetAllApplicationsAsync();
        Task<ApplicationResponseDto?> GetApplicationByIdAsync(int id);
        Task<ApplicationResponseDto> CreateApplicationAsync(CreateApplicationDto request);
        Task<bool> UpdateApplicationAsync(int id, ApplicationResponseDto application);
        Task<ApplicationResponseDto?> UpdateApplicationStatusAsync(int id, string status);
        Task<bool> DeleteApplicationAsync(int id);

        Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByUserAsync(Guid userId);
        Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByFormAsync(int formId);
        Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByStatusAsync(string status);
        Task<ApplicationResponseDto?> GetApplicationByUserAndFormAsync(Guid userId, int formId);
        Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByCampaignAsync(int campaignId, string? status = null);
        Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByClubAsync(int clubId, string? status = null);

        Task<IEnumerable<ApplicationFormResponseDto>> GetAllFormsAsync();
        Task<IEnumerable<ApplicationFormResponseDto>> GetFormsByCampaignAsync(int campaignId);
        Task<ApplicationFormResponseDto?> GetFormByIdAsync(int id);
        Task<ApplicationFormResponseDto> CreateFormAsync(CreateApplicationFormDto request);
        Task<bool> UpdateFormAsync(int id, ApplicationFormResponseDto form);
        Task<bool> DeleteFormAsync(int id);

        Task<IEnumerable<ApplicationQuestionResponseDto>> GetQuestionsByFormAsync(int formId);
        Task<ApplicationQuestionResponseDto?> GetQuestionByIdAsync(int id);
        Task<ApplicationQuestionResponseDto> CreateQuestionAsync(CreateApplicationQuestionDto request);
        Task<bool> UpdateQuestionAsync(int id, ApplicationQuestionResponseDto question);
        Task<bool> DeleteQuestionAsync(int id);

        Task<IEnumerable<ApplicationAnswerResponseDto>> GetAnswersByApplicationAsync(int applicationId);
        Task<ApplicationAnswerResponseDto?> GetAnswerByIdAsync(int answerId);
        Task<ApplicationAnswerResponseDto> CreateAnswerAsync(CreateApplicationAnswerDto request);
        Task<ApplicationResponseDto> SubmitApplicationWithAnswersAsync(SubmitApplicationWithAnswersDto request);
        Task<bool> UpdateAnswerAsync(int id, ApplicationAnswerResponseDto answer);
        Task<bool> DeleteAnswerAsync(int id);
    }
}
