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
    public class ClubControllerTest
    {
        private readonly Mock<IClubService> _mockService;
        private readonly ClubController _controller;

        public ClubControllerTest()
        {
            _mockService = new Mock<IClubService>();
            _controller = new ClubController(_mockService.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOk()
        {
            _mockService.Setup(s => s.GetAllAsync())
                .ReturnsAsync(new List<ClubResponseDto> { new() });

            var result = await _controller.GetAll();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetActiveClubs_ReturnsOk()
        {
            _mockService.Setup(s => s.GetActiveClubsAsync())
                .ReturnsAsync(new List<ClubResponseDto>());

            var result = await _controller.GetActiveClubs();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetPublicClubs_ReturnsOk()
        {
            _mockService.Setup(s => s.GetPublicClubsAsync())
                .ReturnsAsync(new List<ClubResponseDto>());

            var result = await _controller.GetPublicClubs();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(new ClubResponseDto { ClubId = 1 });

            var result = await _controller.GetById(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync((ClubResponseDto?)null);

            var result = await _controller.GetById(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WhenSuccess()
        {
            var dto = new CreateClubDto { ClubName = "NewClub" };
            _mockService.Setup(s => s.CreateAsync(dto))
                .ReturnsAsync(new ClubResponseDto { ClubId = 1 });

            var result = await _controller.Create(dto);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenInvalidOperation()
        {
            var dto = new CreateClubDto { ClubName = "Duplicate" };
            _mockService.Setup(s => s.CreateAsync(dto))
                .ThrowsAsync(new InvalidOperationException("Already exists"));

            var result = await _controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_Returns500_WhenUnexpectedException()
        {
            var dto = new CreateClubDto { ClubName = "Broken" };
            _mockService.Setup(s => s.CreateAsync(dto))
                .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.Create(dto);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task Update_ReturnsOk_WhenSuccess()
        {
            var dto = new UpdateClubDto { ClubName = "Updated" };
            _mockService.Setup(s => s.UpdateAsync(1, dto))
                .ReturnsAsync(new ClubResponseDto { ClubId = 1 });

            var result = await _controller.Update(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenNull()
        {
            var dto = new UpdateClubDto();
            _mockService.Setup(s => s.UpdateAsync(99, dto))
                .ReturnsAsync((ClubResponseDto?)null);

            var result = await _controller.Update(99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenInvalidOperation()
        {
            var dto = new UpdateClubDto();
            _mockService.Setup(s => s.UpdateAsync(1, dto))
                .ThrowsAsync(new InvalidOperationException("Conflict"));

            var result = await _controller.Update(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task changStatus_ReturnsOk_WhenSuccess()
        {
            _mockService.Setup(s => s.ChangeStatusClub(1)).Returns(Task.CompletedTask);

            var result = await _controller.changStatus(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task changStatus_ReturnsBadRequest_WhenInvalidOperation()
        {
            _mockService.Setup(s => s.ChangeStatusClub(1))
                .ThrowsAsync(new InvalidOperationException("Already inactive"));

            var result = await _controller.changStatus(1);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SoftDelete_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.SoftDeleteAsync(1)).ReturnsAsync(true);

            var result = await _controller.SoftDelete(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task SoftDelete_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.SoftDeleteAsync(99)).ReturnsAsync(false);

            var result = await _controller.SoftDelete(99);

            Assert.IsType<NotFoundObjectResult>(result);
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
