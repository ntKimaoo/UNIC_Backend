using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Implementation;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class RecruitmentCampaignServiceTest
    {
        private readonly Mock<IRecruitmentCampaignRepository> _mockRepo;
        private readonly Mock<IClubRepository> _mockClubRepo;
        private readonly RecruitmentCampaignService _service;

        public RecruitmentCampaignServiceTest()
        {
            _mockRepo = new Mock<IRecruitmentCampaignRepository>();
            _mockClubRepo = new Mock<IClubRepository>();
            _service = new RecruitmentCampaignService(_mockRepo.Object, _mockClubRepo.Object);
        }

        private static RecruitmentCampaign CreateCampaign(int id = 1) => new RecruitmentCampaign
        {
            CampaignId = id,
            ClubId = 1,
            CampaignName = $"Campaign {id}",
            Description = "Test Description",
            Status = "OPEN",
            CreatedAt = DateTime.UtcNow
        };

        #region GetByIdAsync

        [Fact]
        public async Task GetByIdAsync_ReturnsDto_WhenFound()
        {
            var campaign = CreateCampaign();
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(campaign);

            var result = await _service.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result!.CampaignId);
            Assert.Equal("Campaign 1", result.CampaignName);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((RecruitmentCampaign?)null);

            var result = await _service.GetByIdAsync(99);

            Assert.Null(result);
        }

        #endregion

        #region GetAllAsync

        [Fact]
        public async Task GetAllAsync_ReturnsMappedDtos()
        {
            var campaigns = new List<RecruitmentCampaign> { CreateCampaign(1), CreateCampaign(2) };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(campaigns);

            var result = await _service.GetAllAsync();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmpty_WhenNoCampaigns()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<RecruitmentCampaign>());

            var result = await _service.GetAllAsync();

            Assert.Empty(result);
        }

        #endregion

        #region GetByClubIdAsync

        [Fact]
        public async Task GetByClubIdAsync_ReturnsFilteredDtos()
        {
            var campaigns = new List<RecruitmentCampaign> { CreateCampaign(1) };
            _mockRepo.Setup(r => r.GetByClubIdAsync(1)).ReturnsAsync(campaigns);

            var result = await _service.GetByClubIdAsync(1);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetByClubIdAsync_ReturnsEmpty_WhenNoMatch()
        {
            _mockRepo.Setup(r => r.GetByClubIdAsync(99)).ReturnsAsync(new List<RecruitmentCampaign>());

            var result = await _service.GetByClubIdAsync(99);

            Assert.Empty(result);
        }

        #endregion

        #region CreateAsync

        [Fact]
        public async Task CreateAsync_ReturnsDto_WhenValidInput()
        {
            var dto = new CreateRecruitmentCampaignDto
            {
                ClubId = 1,
                CampaignName = "New Campaign",
                Description = "Desc",
                Status = "OPEN"
            };

            _mockClubRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<RecruitmentCampaign>()))
                     .ReturnsAsync((RecruitmentCampaign c) => { c.CampaignId = 10; return c; });

            var result = await _service.CreateAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(10, result.CampaignId);
            Assert.Equal("New Campaign", result.CampaignName);
            Assert.Equal("OPEN", result.Status);
        }

        [Fact]
        public async Task CreateAsync_SetsDefaultStatusToOpen_WhenStatusIsNull()
        {
            var dto = new CreateRecruitmentCampaignDto
            {
                ClubId = 1,
                CampaignName = "No Status",
                Status = null
            };

            _mockClubRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<RecruitmentCampaign>()))
                     .ReturnsAsync((RecruitmentCampaign c) => { c.CampaignId = 11; return c; });

            var result = await _service.CreateAsync(dto);

            Assert.Equal("OPEN", result.Status);
        }

        [Fact]
        public async Task CreateAsync_ThrowsNotFoundException_WhenClubNotFound()
        {
            var dto = new CreateRecruitmentCampaignDto { ClubId = 99, CampaignName = "Test" };

            _mockClubRepo.Setup(r => r.ExistsAsync(99)).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(dto));
            _mockRepo.Verify(r => r.CreateAsync(It.IsAny<RecruitmentCampaign>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ThrowsDomainException_WhenStartDateEqualsEndDate()
        {
            var date = DateTime.UtcNow;
            var dto = new CreateRecruitmentCampaignDto
            {
                ClubId = 1,
                CampaignName = "Test",
                StartDate = date,
                EndDate = date
            };

            _mockClubRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(dto));
            Assert.Equal("StartDate must be earlier than EndDate.", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_ThrowsDomainException_WhenStartDateAfterEndDate()
        {
            var dto = new CreateRecruitmentCampaignDto
            {
                ClubId = 1,
                CampaignName = "Test",
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = DateTime.UtcNow
            };

            _mockClubRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);

            await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ThrowsDomainException_WhenStatusIsInvalid()
        {
            var dto = new CreateRecruitmentCampaignDto
            {
                ClubId = 1,
                CampaignName = "Test",
                Status = "INVALID"
            };

            _mockClubRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(dto));
            Assert.Contains("Invalid status", ex.Message);
            Assert.Contains("OPEN, CLOSED, DRAFT", ex.Message);
        }

        [Theory]
        [InlineData("OPEN")]
        [InlineData("CLOSED")]
        [InlineData("DRAFT")]
        public async Task CreateAsync_Succeeds_WithAllValidStatuses(string status)
        {
            var dto = new CreateRecruitmentCampaignDto
            {
                ClubId = 1,
                CampaignName = "Test",
                Status = status
            };

            _mockClubRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<RecruitmentCampaign>()))
                     .ReturnsAsync((RecruitmentCampaign c) => c);

            var result = await _service.CreateAsync(dto);

            Assert.Equal(status, result.Status);
        }

        [Fact]
        public async Task CreateAsync_Succeeds_WhenOnlyStartDateProvided()
        {
            var dto = new CreateRecruitmentCampaignDto
            {
                ClubId = 1,
                CampaignName = "Test",
                StartDate = DateTime.UtcNow,
                EndDate = null
            };

            _mockClubRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<RecruitmentCampaign>()))
                     .ReturnsAsync((RecruitmentCampaign c) => c);

            var result = await _service.CreateAsync(dto);

            Assert.NotNull(result);
        }

        #endregion

        #region UpdateAsync

        [Fact]
        public async Task UpdateAsync_ReturnsUpdatedDto_WhenFound()
        {
            var campaign = CreateCampaign();
            var dto = new UpdateRecruitmentCampaignDto
            {
                CampaignName = "Updated Name",
                Status = "CLOSED"
            };

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(campaign);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<RecruitmentCampaign>())).ReturnsAsync(true);

            var result = await _service.UpdateAsync(1, dto);

            Assert.NotNull(result);
            Assert.Equal("Updated Name", result!.CampaignName);
            Assert.Equal("CLOSED", result.Status);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsNull_WhenCampaignNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((RecruitmentCampaign?)null);

            var result = await _service.UpdateAsync(99, new UpdateRecruitmentCampaignDto());

            Assert.Null(result);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<RecruitmentCampaign>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsNull_WhenRepositoryUpdateFails()
        {
            var campaign = CreateCampaign();
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(campaign);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<RecruitmentCampaign>())).ReturnsAsync(false);

            var result = await _service.UpdateAsync(1, new UpdateRecruitmentCampaignDto
            {
                CampaignName = "Will Fail"
            });

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_PreservesUnchangedFields_WhenPartialUpdate()
        {
            var campaign = CreateCampaign();
            campaign.Description = "Original Description";
            campaign.Content = "Original Content";
            campaign.LinkCampaign = "http://original.com";

            var dto = new UpdateRecruitmentCampaignDto
            {
                CampaignName = "New Name"
            };

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(campaign);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<RecruitmentCampaign>())).ReturnsAsync(true);

            var result = await _service.UpdateAsync(1, dto);

            Assert.NotNull(result);
            Assert.Equal("New Name", result!.CampaignName);
            Assert.Equal("Original Description", result.Description);
            Assert.Equal("Original Content", result.Content);
            Assert.Equal("http://original.com", result.LinkCampaign);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesAllFields_WhenFullUpdate()
        {
            var campaign = CreateCampaign();
            var now = DateTime.UtcNow;
            var dto = new UpdateRecruitmentCampaignDto
            {
                CampaignName = "Full Update",
                LinkCampaign = "http://link.com",
                Description = "New Desc",
                StartDate = now,
                EndDate = now.AddDays(30),
                Status = "CLOSED",
                ImageUrl = "http://img.com/pic.jpg",
                Content = "New Content"
            };

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(campaign);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<RecruitmentCampaign>())).ReturnsAsync(true);

            var result = await _service.UpdateAsync(1, dto);

            Assert.NotNull(result);
            Assert.Equal("Full Update", result!.CampaignName);
            Assert.Equal("http://link.com", result.LinkCampaign);
            Assert.Equal("New Desc", result.Description);
            Assert.Equal(now, result.StartDate);
            Assert.Equal(now.AddDays(30), result.EndDate);
            Assert.Equal("CLOSED", result.Status);
            Assert.Equal("http://img.com/pic.jpg", result.ImageUrl);
            Assert.Equal("New Content", result.Content);
        }

        #endregion

        #region DeleteAsync

        [Fact]
        public async Task DeleteAsync_ReturnsTrue_WhenDeleted()
        {
            _mockRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            var result = await _service.DeleteAsync(1);

            Assert.True(result);
            _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
        {
            _mockRepo.Setup(r => r.DeleteAsync(99)).ReturnsAsync(false);

            var result = await _service.DeleteAsync(99);

            Assert.False(result);
        }

        #endregion
    }
}
