using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UNIC.DataAccess.Repositories.Interface
{
    public interface IApplicationRepository
    {
        Task<IEnumerable<Application>> GetAllAsync();
        Task<Application?> GetByIdAsync(int applicationId);
        Task<Application> CreateAsync(Application application);
        Task<bool> UpdateAsync(Application application);
        Task<bool> DeleteAsync(int applicationId);

        Task<IEnumerable<Application>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Application>> GetByFormIdAsync(int formId);
        Task<IEnumerable<Application>> GetByStatusAsync(string status);
        Task<Application?> GetByUserIdAndFormIdAsync(Guid userId, int formId);
        Task<IEnumerable<Application>> GetByCampaignIdAsync(int campaignId, string? status = null);
        Task<IEnumerable<Application>> GetByClubIdAsync(int clubId, string? status = null);

        Task<IEnumerable<ApplicationForm>> GetAllFormsAsync();
        Task<IEnumerable<ApplicationForm>> GetFormsByCampaignIdAsync(int campaignId);
        Task<ApplicationForm?> GetFormByIdAsync(int formId);
        Task<ApplicationForm> CreateFormAsync(ApplicationForm form);
        Task<bool> UpdateFormAsync(ApplicationForm form);
        Task<bool> DeleteFormAsync(int formId);

        Task<IEnumerable<ApplicationQuestion>> GetAllQuestionsAsync();
        Task<IEnumerable<ApplicationQuestion>> GetQuestionsByFormIdAsync(int formId);
        Task<ApplicationQuestion?> GetQuestionByIdAsync(int questionId);
        Task<ApplicationQuestion> CreateQuestionAsync(ApplicationQuestion question);
        Task<bool> UpdateQuestionAsync(ApplicationQuestion question);
        Task<bool> DeleteQuestionAsync(int questionId);

        Task<IEnumerable<ApplicationAnswer>> GetAnswersByApplicationIdAsync(int applicationId);
        Task<ApplicationAnswer?> GetAnswerByIdAsync(int answerId);
        Task<ApplicationAnswer> CreateAnswerAsync(ApplicationAnswer answer);
        Task CreateAnswersAsync(IEnumerable<ApplicationAnswer> answers);
        Task<bool> UpdateAnswerAsync(ApplicationAnswer answer);
        Task<bool> DeleteAnswerAsync(int answerId);
    }
}
