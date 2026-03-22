using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UNIC.DataAccess.Repositories.Interface
{
    public interface IApplicationRepository
    {
        Task<IEnumerable<Application>> GetAllAsync(int clubId);
        Task<Application?> GetByIdAsync(int applicationId, int clubId);
        Task<Application> CreateAsync(Application application);
        Task<bool> UpdateAsync(Application application);
        Task<bool> DeleteAsync(int applicationId, int clubId);

        Task<IEnumerable<Application>> GetByUserIdAsync(Guid userId, int clubId);
        Task<IEnumerable<Application>> GetByFormIdAsync(int formId, int clubId);
        Task<IEnumerable<Application>> GetByStatusAsync(string status, int clubId);
        Task<Application?> GetByUserIdAndFormIdAsync(Guid userId, int formId, int clubId);
        Task<IEnumerable<Application>> GetByCampaignIdAsync(int campaignId, int clubId, string? status = null);
        Task<IEnumerable<Application>> GetByClubIdAsync(int clubId, string? status = null);

        Task<IEnumerable<ApplicationForm>> GetAllFormsAsync(int clubId);
        Task<IEnumerable<ApplicationForm>> GetFormsByCampaignIdAsync(int campaignId, int clubId);
        Task<ApplicationForm?> GetFormByIdAsync(int formId, int clubId);
        Task<ApplicationForm> CreateFormAsync(ApplicationForm form);
        Task<bool> UpdateFormAsync(ApplicationForm form);
        Task<bool> DeleteFormAsync(int formId, int clubId);

        Task<IEnumerable<ApplicationQuestion>> GetAllQuestionsAsync(int clubId);
        Task<IEnumerable<ApplicationQuestion>> GetQuestionsByFormIdAsync(int formId, int clubId);
        Task<ApplicationQuestion?> GetQuestionByIdAsync(int questionId, int clubId);
        Task<ApplicationQuestion> CreateQuestionAsync(ApplicationQuestion question);
        Task<bool> UpdateQuestionAsync(ApplicationQuestion question);
        Task<bool> DeleteQuestionAsync(int questionId, int clubId);

        Task<IEnumerable<ApplicationAnswer>> GetAnswersByApplicationIdAsync(int applicationId, int clubId);
        Task<ApplicationAnswer?> GetAnswerByIdAsync(int answerId, int clubId);
        Task<ApplicationAnswer> CreateAnswerAsync(ApplicationAnswer answer);
        Task CreateAnswersAsync(IEnumerable<ApplicationAnswer> answers);
        Task<bool> UpdateAnswerAsync(ApplicationAnswer answer);
        Task<bool> DeleteAnswerAsync(int answerId, int clubId);
    }
}
