using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogic.DTOs;
using UNIC.BusinessLogic.DTOs;

namespace UNIC.BusinessLogic.Services.Interface
{
    public interface IApplicationService
    {
        Task<IEnumerable<ApplicationResponseDto>> GetAllApplicationsAsync(int clubId);
        Task<ApplicationResponseDto?> GetApplicationByIdAsync(int id, int clubId);
        Task<ApplicationResponseDto> CreateApplicationAsync(CreateApplicationDto request);
        Task<bool> UpdateApplicationAsync(int id, int clubId, ApplicationResponseDto application);
        Task<ApplicationResponseDto?> UpdateApplicationStatusAsync(int id, int clubId, string status);
        Task<bool> DeleteApplicationAsync(int id, int clubId);

        Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByUserAsync(Guid userId, int clubId);
        Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByFormAsync(int formId, int clubId);
        Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByStatusAsync(string status, int clubId);
        Task<ApplicationResponseDto?> GetApplicationByUserAndFormAsync(Guid userId, int formId, int clubId);
        Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByCampaignAsync(int campaignId, int clubId, string? status = null);
        Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByClubAsync(int clubId, string? status = null);

        Task<IEnumerable<ApplicationFormResponseDto>> GetAllFormsAsync(int clubId);
        Task<IEnumerable<ApplicationFormResponseDto>> GetFormsByCampaignAsync(int campaignId, int clubId);
        Task<ApplicationFormResponseDto?> GetFormByIdAsync(int id, int clubId);
        Task<ApplicationFormResponseDto> CreateFormAsync(CreateApplicationFormDto request);
        Task<bool> UpdateFormAsync(int id, int clubId, ApplicationFormResponseDto form);
        Task<bool> DeleteFormAsync(int id, int clubId);

        Task<IEnumerable<ApplicationQuestionResponseDto>> GetQuestionsByFormAsync(int formId, int clubId);
        Task<ApplicationQuestionResponseDto?> GetQuestionByIdAsync(int id, int clubId);
        Task<ApplicationQuestionResponseDto> CreateQuestionAsync(CreateApplicationQuestionDto request);
        Task<bool> UpdateQuestionAsync(int id, int clubId, ApplicationQuestionResponseDto question);
        Task<bool> DeleteQuestionAsync(int id, int clubId);

        Task<IEnumerable<ApplicationAnswerResponseDto>> GetAnswersByApplicationAsync(int applicationId, int clubId);
        Task<ApplicationAnswerResponseDto?> GetAnswerByIdAsync(int answerId, int clubId);
        Task<ApplicationAnswerResponseDto> CreateAnswerAsync(CreateApplicationAnswerDto request, int clubId);
        Task<ApplicationResponseDto> SubmitApplicationWithAnswersAsync(int clubId, SubmitApplicationWithAnswersDto request);
        Task<bool> UpdateAnswerAsync(int id, int clubId, ApplicationAnswerResponseDto answer);
        Task<bool> DeleteAnswerAsync(int id, int clubId);
    }
}
