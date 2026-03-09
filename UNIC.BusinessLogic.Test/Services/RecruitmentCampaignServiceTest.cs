using BusinessLogic.DTOs;
using BusinessLogic.Services.Implementation;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.BusinessLogic.Test.Services
{
    public class RecruitmentCampaignServiceTest
    {
        private readonly Mock<IRecruitmentCampaignRepository> _mockRepo;
        private readonly RecruitmentCampaignService _campaignService;

        public RecruitmentCampaignServiceTest()
        {
            _mockRepo = new Mock<IRecruitmentCampaignRepository>();
            _campaignService = new RecruitmentCampaignService(_mockRepo.Object);
        }

        #region GetMethods

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((RecruitmentCampaign?)null);
            var result = await _campaignService.GetByIdAsync(1);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedDto_WhenFound()
        {
            var campaign = new RecruitmentCampaign { CampaignId = 1, CampaignName = "Test" };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(campaign);

            var result = await _campaignService.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Test", result.CampaignName);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedDtos()
        {
            var campaigns = new List<RecruitmentCampaign> { new RecruitmentCampaign { CampaignId = 1 } };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(campaigns);

            var result = await _campaignService.GetAllAsync();

            Assert.Single(result);
            Assert.Equal(1, result.First().CampaignId);
        }

        [Fact]
        public async Task GetByClubIdAsync_ShouldReturnMappedDtos()
        {
            var campaigns = new List<RecruitmentCampaign> { new RecruitmentCampaign { CampaignId = 1, ClubId = 2 } };
            _mockRepo.Setup(r => r.GetByClubIdAsync(2)).ReturnsAsync(campaigns);

            var result = await _campaignService.GetByClubIdAsync(2);

            Assert.Single(result);
            Assert.Equal(2, result.First().ClubId);
        }

        #endregion

        #region CreateAsync

        [Fact]
        public async Task CreateAsync_ShouldReturnMappedDto()
        {
            var dto = new CreateRecruitmentCampaignDto { CampaignName = "New Campaign" };
            var createdEntity = new RecruitmentCampaign { CampaignId = 10, CampaignName = "New Campaign", Status = "OPEN" };
            
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<RecruitmentCampaign>())).ReturnsAsync(createdEntity);

            var result = await _campaignService.CreateAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(10, result.CampaignId);
            Assert.Equal("OPEN", result.Status);
            _mockRepo.Verify(r => r.CreateAsync(It.IsAny<RecruitmentCampaign>()), Times.Once);
        }

        #endregion

        #region UpdateAsync

        [Fact]
        public async Task UpdateAsync_ShouldReturnNull_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((RecruitmentCampaign?)null);
            var result = await _campaignService.UpdateAsync(1, new UpdateRecruitmentCampaignDto());
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateFieldsAndReturnDto_WhenValid()
        {
            var campaign = new RecruitmentCampaign { CampaignId = 1, CampaignName = "Old", Status = "OPEN" };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(campaign);
            _mockRepo.Setup(r => r.UpdateAsync(campaign)).ReturnsAsync(true);

            var dto = new UpdateRecruitmentCampaignDto { CampaignName = "New", Status = "CLOSED" };
            var result = await _campaignService.UpdateAsync(1, dto);

            Assert.NotNull(result);
            Assert.Equal("New", campaign.CampaignName);
            Assert.Equal("CLOSED", campaign.Status);
            _mockRepo.Verify(r => r.UpdateAsync(campaign), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnNull_WhenUpdateFailsInRepo()
        {
            var campaign = new RecruitmentCampaign { CampaignId = 1 };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(campaign);
            _mockRepo.Setup(r => r.UpdateAsync(campaign)).ReturnsAsync(false);

            var result = await _campaignService.UpdateAsync(1, new UpdateRecruitmentCampaignDto());
            
            Assert.Null(result);
        }

        #endregion

        #region DeleteAsync

        [Fact]
        public async Task DeleteAsync_ShouldReturnRepoResult()
        {
            _mockRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);
            var result = await _campaignService.DeleteAsync(1);
            Assert.True(result);
        }

        #endregion
    }
}
