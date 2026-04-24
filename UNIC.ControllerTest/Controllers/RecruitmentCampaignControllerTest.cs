using BusinessLogic.DTOs;
using BusinessLogic.Exceptions;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Controllers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ControllerTest.Controllers
{
    public class RecruitmentCampaignControllerTest
    {
        private readonly Mock<IRecruitmentCampaignService> _mockService;
        private readonly RecruitmentCampaignController _controller;

        public RecruitmentCampaignControllerTest()
        {
            _mockService = new Mock<IRecruitmentCampaignService>();
            _controller = new RecruitmentCampaignController(_mockService.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region GetAll

        [Fact]
        public async Task GetAll_ReturnsOk_WithCampaignList()
        {
            var campaigns = new List<RecruitmentCampaignResponseDto>
            {
                new RecruitmentCampaignResponseDto { CampaignId = 1, CampaignName = "Campaign 1" }
            };

            _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(campaigns);

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithEmptyList()
        {
            _mockService.Setup(s => s.GetAllAsync())
                        .ReturnsAsync(new List<RecruitmentCampaignResponseDto>());

            var result = await _controller.GetAll();

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region GetAllByClubId

        [Fact]
        public async Task GetAllByClubId_ReturnsOk_WithCampaigns()
        {
            var campaigns = new List<RecruitmentCampaignResponseDto>
            {
                new RecruitmentCampaignResponseDto { CampaignId = 1, ClubId = 1 }
            };

            _mockService.Setup(s => s.GetByClubIdAsync(1)).ReturnsAsync(campaigns);

            var result = await _controller.GetAllByClubId(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetAllByClubId_ReturnsOk_WithEmptyList_WhenNoMatch()
        {
            _mockService.Setup(s => s.GetByClubIdAsync(99))
                        .ReturnsAsync(new List<RecruitmentCampaignResponseDto>());

            var result = await _controller.GetAllByClubId(99);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region GetById

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var campaign = new RecruitmentCampaignResponseDto { CampaignId = 1, CampaignName = "Test" };

            _mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(campaign);

            var result = await _controller.GetById(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenCampaignDoesNotExist()
        {
            _mockService.Setup(s => s.GetByIdAsync(99))
                        .ReturnsAsync((RecruitmentCampaignResponseDto?)null);

            var result = await _controller.GetById(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region Create

        [Fact]
        public async Task Create_ReturnsCreated_WhenSuccess()
        {
            var dto = new CreateRecruitmentCampaignDto { ClubId = 1, CampaignName = "New Campaign" };
            var response = new RecruitmentCampaignResponseDto { CampaignId = 1, CampaignName = "New Campaign" };

            _mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(response);

            var result = await _controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetById), created.ActionName);
        }

        [Fact]
        public async Task Create_ReturnsNotFound_WhenClubNotFound()
        {
            var dto = new CreateRecruitmentCampaignDto { ClubId = 99, CampaignName = "Test" };

            _mockService.Setup(s => s.CreateAsync(dto))
                        .ThrowsAsync(new NotFoundException("Club", 99));

            var result = await _controller.Create(dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenDomainRuleViolated()
        {
            var dto = new CreateRecruitmentCampaignDto { ClubId = 1, CampaignName = "Test" };

            _mockService.Setup(s => s.CreateAsync(dto))
                        .ThrowsAsync(new DomainException("StartDate must be earlier than EndDate."));

            var result = await _controller.Create(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task Create_Returns500_WhenUnexpectedExceptionThrown()
        {
            var dto = new CreateRecruitmentCampaignDto { ClubId = 1, CampaignName = "Test" };

            _mockService.Setup(s => s.CreateAsync(dto))
                        .ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.Create(dto);

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        #endregion

        #region Update

        [Fact]
        public async Task Update_ReturnsOk_WhenSuccess()
        {
            var dto = new UpdateRecruitmentCampaignDto { CampaignName = "Updated" };
            var response = new RecruitmentCampaignResponseDto { CampaignId = 1, CampaignName = "Updated" };

            _mockService.Setup(s => s.UpdateAsync(1, dto)).ReturnsAsync(response);

            var result = await _controller.Update(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenCampaignDoesNotExist()
        {
            var dto = new UpdateRecruitmentCampaignDto { CampaignName = "Missing" };

            _mockService.Setup(s => s.UpdateAsync(99, dto))
                        .ReturnsAsync((RecruitmentCampaignResponseDto?)null);

            var result = await _controller.Update(99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenNotFoundExceptionThrown()
        {
            var dto = new UpdateRecruitmentCampaignDto { CampaignName = "Test" };

            _mockService.Setup(s => s.UpdateAsync(99, dto))
                        .ThrowsAsync(new NotFoundException("Campaign", 99));

            var result = await _controller.Update(99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenDomainRuleViolated()
        {
            var dto = new UpdateRecruitmentCampaignDto { CampaignName = "Test" };

            _mockService.Setup(s => s.UpdateAsync(1, dto))
                        .ThrowsAsync(new DomainException("StartDate must be earlier than EndDate."));

            var result = await _controller.Update(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Update_Returns500_WhenUnexpectedExceptionThrown()
        {
            var dto = new UpdateRecruitmentCampaignDto { CampaignName = "Error" };

            _mockService.Setup(s => s.UpdateAsync(1, dto))
                        .ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.Update(1, dto);

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        #endregion

        #region Delete

        [Fact]
        public async Task Delete_ReturnsOk_WhenSuccess()
        {
            _mockService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

            var result = await _controller.Delete(1);

            Assert.IsType<OkObjectResult>(result);
            _mockService.Verify(s => s.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenCampaignDoesNotExist()
        {
            _mockService.Setup(s => s.DeleteAsync(99)).ReturnsAsync(false);

            var result = await _controller.Delete(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion
    }
}
