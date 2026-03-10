using DataAccess.Models;
using DataAccess.Repositories.Implementation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.Repository.Test.Repository.Test
{
    public class RecruitmentCampaignRepositoryTest
    {
        private UnicContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<UnicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UnicContext(options);
        }

        private RecruitmentCampaign CreateValidCampaign(int id, int clubId, string name = "Test Campaign")
        {
            return new RecruitmentCampaign
            {
                CampaignId = id,
                ClubId = clubId,
                CampaignName = name,
                LinkCampaign = "http://link.com",
                Description = "Description",
                ImageUrl = "http://image.com",
                Content = "Content",
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCampaign_WhenExists()
        {
            // Arrange
            var context = GetInMemoryContext();
            var campaign = CreateValidCampaign(1, 10);
            context.RecruitmentCampaigns.Add(campaign);
            await context.SaveChangesAsync();

            var repository = new RecruitmentCampaignRepository(context);

            // Act
            var result = await repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Campaign", result.CampaignName);
        }

        [Fact]
        public async Task GetByClubIdAsync_ShouldReturnClubCampaigns()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.RecruitmentCampaigns.AddRange(new List<RecruitmentCampaign>
            {
                CreateValidCampaign(1, 10, "C1"),
                CreateValidCampaign(2, 10, "C2"),
                CreateValidCampaign(3, 20, "C Other")
            });
            await context.SaveChangesAsync();

            var repository = new RecruitmentCampaignRepository(context);

            // Act
            var result = await repository.GetByClubIdAsync(10);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task CreateAsync_ShouldAddCampaign()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new RecruitmentCampaignRepository(context);
            var campaign = CreateValidCampaign(0, 10, "New Campaign");

            // Act
            var result = await repository.CreateAsync(campaign);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CampaignId > 0);
            var inDb = await context.RecruitmentCampaigns.FindAsync(result.CampaignId);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenExists()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.RecruitmentCampaigns.Add(CreateValidCampaign(1, 10));
            await context.SaveChangesAsync();

            var repository = new RecruitmentCampaignRepository(context);

            // Act
            var exists = await repository.ExistsAsync(1);

            // Assert
            Assert.True(exists);
        }
    }
}
