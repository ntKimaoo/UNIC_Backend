using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.DataAccess.Repositories.Implementation;
using Xunit;

namespace UNIC.Repository.Test.Repository.Test
{
    public class ApplicationRepositoryTest
    {
        private UnicContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<UnicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UnicContext(options);
        }

        private ApplicationForm CreateValidForm(int id, int campaignId, string name = "Test Form")
        {
            return new ApplicationForm
            {
                FormId = id,
                CampaignId = campaignId,
                FormName = name,
                FormTitle = "Title",
                Description = "Description",
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task CreateAsync_ShouldAddApplication_WhenNotExists()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new ApplicationRepository(context);
            var userId = Guid.NewGuid();
            var application = new Application { FormId = 1, UserId = userId, Status = "PENDING" };

            // Act
            var result = await repository.CreateAsync(application);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ApplicationId > 0);
            var inDb = await context.Applications.FindAsync(result.ApplicationId);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenDuplicateExists()
        {
            // Arrange
            var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            context.Applications.Add(new Application { FormId = 1, UserId = userId });
            await context.SaveChangesAsync();

            var repository = new ApplicationRepository(context);
            var duplicate = new Application { FormId = 1, UserId = userId };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreateAsync(duplicate));
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnUserApplications()
        {
            // Arrange
            var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            context.Applications.AddRange(new List<Application>
            {
                new Application { FormId = 1, UserId = userId },
                new Application { FormId = 2, UserId = userId },
                new Application { FormId = 1, UserId = Guid.NewGuid() }
            });
            await context.SaveChangesAsync();

            var repository = new ApplicationRepository(context);

            // Act
            var result = await repository.GetByUserIdAsync(userId);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task CreateFormAsync_ShouldAddForm()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new ApplicationRepository(context);
            var form = new ApplicationForm { CampaignId = 1, FormName = "New Form", FormTitle = "T", Description = "D" };

            // Act
            var result = await repository.CreateFormAsync(form);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FormId > 0);
            Assert.Equal("New Form", result.FormName);
        }

        [Fact]
        public async Task GetFormsByCampaignIdAsync_ShouldReturnCampaignForms()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.ApplicationForms.AddRange(new List<ApplicationForm>
            {
                CreateValidForm(1, 10, "Form 1"),
                CreateValidForm(2, 10, "Form 2"),
                CreateValidForm(3, 20, "Form Other")
            });
            await context.SaveChangesAsync();

            var repository = new ApplicationRepository(context);

            // Act
            var result = await repository.GetFormsByCampaignIdAsync(10);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task CreateQuestionAsync_ShouldAddQuestion()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new ApplicationRepository(context);
            var question = new ApplicationQuestion { FormId = 1, QuestionText = "Tell us about yourself", QuestionType = "Text" };

            // Act
            var result = await repository.CreateQuestionAsync(question);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.QuestionId > 0);
            var inDb = await context.ApplicationQuestions.FindAsync(result.QuestionId);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyApplicationStatus()
        {
            // Arrange
            var context = GetInMemoryContext();
            var app = new Application { FormId = 1, UserId = Guid.NewGuid(), Status = "PENDING" };
            context.Applications.Add(app);
            await context.SaveChangesAsync();

            var repository = new ApplicationRepository(context);
            app.Status = "ACCEPTED";

            // Act
            var success = await repository.UpdateAsync(app);

            // Assert
            Assert.True(success);
            var updated = await context.Applications.FindAsync(app.ApplicationId);
            Assert.Equal("ACCEPTED", updated.Status);
        }
    }
}
