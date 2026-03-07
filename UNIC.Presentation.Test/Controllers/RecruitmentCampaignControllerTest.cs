using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Presentation.Controllers;
using Xunit;

namespace UNIC.Presentation.Test.Controllers
{
    public class RecruitmentCampaignControllerTest
    {
        private readonly Mock<IRecruitmentCampaignService> _mockService;
        private readonly RecruitmentCampaignController _controller;

        public RecruitmentCampaignControllerTest()
        {
            _mockService = new Mock<IRecruitmentCampaignService>();
            _controller = new RecruitmentCampaignController(_mockService.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOk()
        {
            _mockService.Setup(s => s.GetAllAsync())
                .ReturnsAsync(new List<RecruitmentCampaignResponseDto> { new() });

            var result = await _controller.GetAll();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(new RecruitmentCampaignResponseDto { CampaignId = 1 });

            var result = await _controller.GetById(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync((RecruitmentCampaignResponseDto?)null);

            var result = await _controller.GetById(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetByClubId_ReturnsOk()
        {
            _mockService.Setup(s => s.GetByClubIdAsync(1))
                .ReturnsAsync(new List<RecruitmentCampaignResponseDto> { new() });

            var result = await _controller.GetByClubId(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WhenSuccess()
        {
            var dto = new CreateRecruitmentCampaignDto { ClubId = 1, CampaignName = "Sprint" };
            _mockService.Setup(s => s.CreateAsync(dto))
                .ReturnsAsync(new RecruitmentCampaignResponseDto { CampaignId = 1 });

            var result = await _controller.Create(dto);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new CreateRecruitmentCampaignDto();
            _mockService.Setup(s => s.CreateAsync(dto))
                .ThrowsAsync(new Exception("Club not found"));

            var result = await _controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsOk_WhenSuccess()
        {
            var dto = new UpdateRecruitmentCampaignDto { CampaignName = "Updated" };
            _mockService.Setup(s => s.UpdateAsync(1, dto))
                .ReturnsAsync(new RecruitmentCampaignResponseDto { CampaignId = 1 });

            var result = await _controller.Update(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenNull()
        {
            var dto = new UpdateRecruitmentCampaignDto();
            _mockService.Setup(s => s.UpdateAsync(99, dto))
                .ReturnsAsync((RecruitmentCampaignResponseDto?)null);

            var result = await _controller.Update(99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new UpdateRecruitmentCampaignDto();
            _mockService.Setup(s => s.UpdateAsync(1, dto))
                .ThrowsAsync(new Exception("Service error"));

            var result = await _controller.Update(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

            var result = await _controller.Delete(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.DeleteAsync(99)).ReturnsAsync(false);

            var result = await _controller.Delete(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
