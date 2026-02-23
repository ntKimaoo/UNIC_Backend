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

        public async Task<bool> DeleteAsync(int applicationId)
        {
            var existing = await _context.Applications.FindAsync(applicationId);
            if (existing == null) return false;

            _context.Applications.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Application>> GetAllAsync()
        {
            return await _context.Applications.ToListAsync();
        }

        public async Task<Application?> GetByIdAsync(int applicationId)
        {
            return await _context.Applications
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);
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

        public async Task<IEnumerable<Application>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Applications
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Application>> GetByFormIdAsync(int formId)
        {
            return await _context.Applications
                .Where(a => a.FormId == formId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Application>> GetByStatusAsync(string status)
        {
            return await _context.Applications
                .Where(a => a.Status == status)
                .ToListAsync();
        }

        public async Task<Application?> GetByUserIdAndFormIdAsync(Guid userId, int formId)
        {
            return await _context.Applications
                .FirstOrDefaultAsync(a => a.UserId == userId && a.FormId == formId);
        }

        public async Task<IEnumerable<ApplicationForm>> GetAllFormsAsync()
        {
            return await _context.ApplicationForms.ToListAsync();
        }

        public async Task<ApplicationForm?> GetFormByIdAsync(int formId)
        {
            return await _context.ApplicationForms
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

        public async Task<bool> DeleteFormAsync(int formId)
        {
            var existing = await _context.ApplicationForms.FindAsync(formId);
            if (existing == null) return false;

            _context.ApplicationForms.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ApplicationQuestion>> GetAllQuestionsAsync()
        {
            return await _context.ApplicationQuestions.ToListAsync();
        }

        public async Task<IEnumerable<ApplicationQuestion>> GetQuestionsByFormIdAsync(int formId)
        {
            return await _context.ApplicationQuestions
                .Where(q => q.FormId == formId)
                .ToListAsync();
        }

        public async Task<ApplicationQuestion?> GetQuestionByIdAsync(int questionId)
        {
            return await _context.ApplicationQuestions
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);
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

        public async Task<bool> DeleteQuestionAsync(int questionId)
        {
            var existing = await _context.ApplicationQuestions.FindAsync(questionId);
            if (existing == null) return false;

            _context.ApplicationQuestions.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ApplicationAnswer>> GetAnswersByApplicationIdAsync(int applicationId)
        {
            return await _context.ApplicationAnswers
                .Where(a => a.ApplicationId == applicationId)
                .ToListAsync();
        }

        public async Task<ApplicationAnswer?> GetAnswerByIdAsync(int answerId)
        {
            return await _context.ApplicationAnswers
                .FirstOrDefaultAsync(a => a.AnswerId == answerId);
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

        public async Task<bool> DeleteAnswerAsync(int answerId)
        {
            var existing = await _context.ApplicationAnswers.FindAsync(answerId);
            if (existing == null) return false;

            _context.ApplicationAnswers.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
