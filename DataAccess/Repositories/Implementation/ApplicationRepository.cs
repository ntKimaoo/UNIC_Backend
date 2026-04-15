using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.DataAccess.Repositories.Interface;

namespace UNIC.DataAccess.Repositories.Implementation
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly UnicContext _context;
        public ApplicationRepository(UnicContext context)
        {
            _context = context;
        }

        // ================= APPLICATION =================

        public async Task<Application> CreateAsync(Application application)
        {
            var existing = await _context.Applications
                .FirstOrDefaultAsync(a => a.FormId == application.FormId && a.UserId == application.UserId);

            if (existing != null)
                throw new InvalidOperationException("An application for this user and form already exists.");

            application.SubmissionDate = DateTime.UtcNow;
            _context.Applications.Add(application);
            await _context.SaveChangesAsync();
            return application;
        }

        public async Task<bool> DeleteAsync(int applicationId, int clubId)
        {
            var existing = await _context.Applications
                .Include(a => a.ApplicationForm)
                    .ThenInclude(f => f.RecruitmentCampaign)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == applicationId &&
                    a.ApplicationForm.RecruitmentCampaign.ClubId == clubId);

            if (existing == null) return false;

            _context.Applications.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Application>> GetAllAsync(int clubId)
        {
            return await _context.Applications
                .Include(a => a.ApplicationForm)
                    .ThenInclude(f => f.RecruitmentCampaign)
                .Where(a => a.ApplicationForm.RecruitmentCampaign.ClubId == clubId)
                .ToListAsync();
        }

        public async Task<Application?> GetByIdAsync(int applicationId, int clubId)
        {
            return await _context.Applications
                .Include(a => a.ApplicationForm)
                    .ThenInclude(f => f.RecruitmentCampaign)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == applicationId &&
                    a.ApplicationForm.RecruitmentCampaign.ClubId == clubId);
        }

        public async Task<bool> UpdateAsync(Application application)
        {
            try
            {
                _context.Applications.Update(application);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ================= FILTER =================

        // change
        public async Task<IEnumerable<Application>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Applications
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Application>> GetByFormIdAsync(int formId, int clubId)
        {
            return await _context.Applications
                .Include(a => a.ApplicationForm)
                    .ThenInclude(f => f.RecruitmentCampaign)
                .Where(a =>
                    a.FormId == formId &&
                    a.ApplicationForm.RecruitmentCampaign.ClubId == clubId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Application>> GetByStatusAsync(string status, int clubId)
        {
            return await _context.Applications
                .Include(a => a.ApplicationForm)
                    .ThenInclude(f => f.RecruitmentCampaign)
                .Where(a =>
                    a.Status == status &&
                    a.ApplicationForm.RecruitmentCampaign.ClubId == clubId)
                .ToListAsync();
        }

        // change
        public async Task<Application?> GetByUserIdAndFormIdAsync(Guid userId, int formId)
        {
            return await _context.Applications
                .FirstOrDefaultAsync(a => a.UserId == userId && a.FormId == formId);
        }

        public async Task<IEnumerable<Application>> GetByCampaignIdAsync(int campaignId, int clubId, string? status = null)
        {
            var query = _context.Applications
                .Include(a => a.ApplicationForm)
                    .ThenInclude(f => f.RecruitmentCampaign)
                .Where(a =>
                    a.ApplicationForm.CampaignId == campaignId &&
                    a.ApplicationForm.RecruitmentCampaign.ClubId == clubId);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(a => a.Status == status);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Application>> GetByClubIdAsync(int clubId, string? status = null)
        {
            var query = _context.Applications
                .Include(a => a.ApplicationForm)
                    .ThenInclude(f => f.RecruitmentCampaign)
                .Where(a => a.ApplicationForm.RecruitmentCampaign.ClubId == clubId);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(a => a.Status == status);

            return await query.ToListAsync();
        }

        // ================= FORM =================

        public async Task<IEnumerable<ApplicationForm>> GetAllFormsAsync(int clubId)
        {
            return await _context.ApplicationForms
                .Include(f => f.RecruitmentCampaign)
                .Where(f => f.RecruitmentCampaign.ClubId == clubId)
                .ToListAsync();
        }

        public async Task<IEnumerable<ApplicationForm>> GetFormsByCampaignIdAsync(int campaignId, int clubId)
        {
            return await _context.ApplicationForms
                .Include(f => f.RecruitmentCampaign)
                .Where(f =>
                    f.CampaignId == campaignId &&
                    f.RecruitmentCampaign.ClubId == clubId)
                .OrderBy(f => f.FormId)
                .ToListAsync();
        }

        // change
        public async Task<ApplicationForm?> GetFormByIdAsync(int formId)
        {
            return await _context.ApplicationForms
                .Include(f => f.RecruitmentCampaign)
                .FirstOrDefaultAsync(f => f.FormId == formId);
        }

        public async Task<ApplicationForm> CreateFormAsync(ApplicationForm form)
        {
            form.CreatedAt = DateTime.UtcNow;
            _context.ApplicationForms.Add(form);
            await _context.SaveChangesAsync();
            return form;
        }

        public async Task<bool> UpdateFormAsync(ApplicationForm form)
        {
            try
            {
                _context.ApplicationForms.Update(form);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteFormAsync(int formId, int clubId)
        {
            var existing = await _context.ApplicationForms
                .Include(f => f.RecruitmentCampaign)
                .FirstOrDefaultAsync(f =>
                    f.FormId == formId &&
                    f.RecruitmentCampaign.ClubId == clubId);

            if (existing == null) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var applications = await _context.Applications
                    .Where(a => a.FormId == formId)
                    .Select(a => a.ApplicationId)
                    .ToListAsync();

                await _context.ApplicationAnswers
                    .Where(a => applications.Contains(a.ApplicationId))
                    .ExecuteDeleteAsync();

                await _context.Applications
                    .Where(a => a.FormId == formId)
                    .ExecuteDeleteAsync();

                await _context.ApplicationQuestions
                    .Where(q => q.FormId == formId)
                    .ExecuteDeleteAsync();

                _context.ApplicationForms.Remove(existing);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ================= QUESTION =================

        public async Task<IEnumerable<ApplicationQuestion>> GetAllQuestionsAsync(int clubId)
        {
            return await _context.ApplicationQuestions
                .Include(q => q.ApplicationForm)
                    .ThenInclude(f => f.RecruitmentCampaign)
                .Where(q => q.ApplicationForm.RecruitmentCampaign.ClubId == clubId)
                .ToListAsync();
        }

        // change
        public async Task<IEnumerable<ApplicationQuestion>> GetQuestionsByFormIdAsync(int formId)
        {
            return await _context.ApplicationQuestions
                .Where(q => q.FormId == formId)
                .ToListAsync();
        }

        public async Task<ApplicationQuestion?> GetQuestionByIdAsync(int questionId, int clubId)
        {
            return await _context.ApplicationQuestions
                .Include(q => q.ApplicationForm)
                    .ThenInclude(f => f.RecruitmentCampaign)
                .FirstOrDefaultAsync(q =>
                    q.QuestionId == questionId &&
                    q.ApplicationForm.RecruitmentCampaign.ClubId == clubId);
        }

        public async Task<ApplicationQuestion> CreateQuestionAsync(ApplicationQuestion question)
        {
            _context.ApplicationQuestions.Add(question);
            await _context.SaveChangesAsync();
            return question;
        }

        public async Task<bool> UpdateQuestionAsync(ApplicationQuestion question)
        {
            try
            {
                _context.ApplicationQuestions.Update(question);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteQuestionAsync(int questionId, int clubId)
        {
            var existing = await _context.ApplicationQuestions
                .Include(q => q.ApplicationForm)
                    .ThenInclude(f => f.RecruitmentCampaign)
                .FirstOrDefaultAsync(q =>
                    q.QuestionId == questionId &&
                    q.ApplicationForm.RecruitmentCampaign.ClubId == clubId);

            if (existing == null) return false;

            _context.ApplicationQuestions.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        // ================= ANSWER =================

        public async Task<IEnumerable<ApplicationAnswer>> GetAnswersByApplicationIdAsync(int applicationId, int clubId)
        {
            return await _context.ApplicationAnswers
                .Include(a => a.Application)
                    .ThenInclude(app => app.ApplicationForm)
                        .ThenInclude(f => f.RecruitmentCampaign)
                .Where(a =>
                    a.ApplicationId == applicationId &&
                    a.Application.ApplicationForm.RecruitmentCampaign.ClubId == clubId)
                .ToListAsync();
        }

        public async Task<ApplicationAnswer?> GetAnswerByIdAsync(int answerId, int clubId)
        {
            return await _context.ApplicationAnswers
                .Include(a => a.Application)
                    .ThenInclude(app => app.ApplicationForm)
                        .ThenInclude(f => f.RecruitmentCampaign)
                .FirstOrDefaultAsync(a =>
                    a.AnswerId == answerId &&
                    a.Application.ApplicationForm.RecruitmentCampaign.ClubId == clubId);
        }

        public async Task<ApplicationAnswer> CreateAnswerAsync(ApplicationAnswer answer)
        {
            _context.ApplicationAnswers.Add(answer);
            await _context.SaveChangesAsync();
            return answer;
        }

        public async Task CreateAnswersAsync(IEnumerable<ApplicationAnswer> answers)
        {
            _context.ApplicationAnswers.AddRange(answers);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateAnswerAsync(ApplicationAnswer answer)
        {
            try
            {
                _context.ApplicationAnswers.Update(answer);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAnswerAsync(int answerId, int clubId)
        {
            var existing = await _context.ApplicationAnswers
                .Include(a => a.Application)
                    .ThenInclude(app => app.ApplicationForm)
                        .ThenInclude(f => f.RecruitmentCampaign)
                .FirstOrDefaultAsync(a =>
                    a.AnswerId == answerId &&
                    a.Application.ApplicationForm.RecruitmentCampaign.ClubId == clubId);

            if (existing == null) return false;

            _context.ApplicationAnswers.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
