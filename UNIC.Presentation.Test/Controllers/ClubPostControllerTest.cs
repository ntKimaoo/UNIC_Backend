using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Presentation.Controllers;
using Xunit;

namespace UNIC.Presentation.Test.Controllers
{
    public class ClubPostControllerTest
    {
        private readonly Mock<IClubPostService> _mockPostService;
        private readonly Mock<IFileStorageService> _mockFileStorage;
        private readonly ClubPostController _controller;

        public ClubPostControllerTest()
        {
            _mockPostService = new Mock<IClubPostService>();
            _mockFileStorage = new Mock<IFileStorageService>();
            _controller = new ClubPostController(_mockPostService.Object, _mockFileStorage.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOk()
        {
            _mockPostService.Setup(s => s.GetAllAsync())
                .ReturnsAsync(new List<ClubPostResponseDto> { new() });

            var result = await _controller.GetAll();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            _mockPostService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(new ClubPostResponseDto { PostId = 1 });

            var result = await _controller.GetById(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenNull()
        {
            _mockPostService.Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync((ClubPostResponseDto?)null);

            var result = await _controller.GetById(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetByClubId_ReturnsOk()
        {
            _mockPostService.Setup(s => s.GetByClubIdAsync(1))
                .ReturnsAsync(new List<ClubPostResponseDto> { new() });

            var result = await _controller.GetByClubId(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetByUserId_ReturnsOk()
        {
            var userId = Guid.NewGuid();
            _mockPostService.Setup(s => s.GetByUserIdAsync(userId))
                .ReturnsAsync(new List<ClubPostResponseDto>());

            var result = await _controller.GetByUserId(userId);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WhenSuccess()
        {
            var dto = new CreateClubPostDto { ClubId = 1, Title = "Post" };
            _mockPostService.Setup(s => s.CreateAsync(dto, null))
                .ReturnsAsync(new ClubPostResponseDto { PostId = 1 });

            var result = await _controller.Create(dto, null);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new CreateClubPostDto();
            _mockPostService.Setup(s => s.CreateAsync(dto, null))
                .ThrowsAsync(new Exception("Service error"));

            var result = await _controller.Create(dto, null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsOk_WhenSuccess()
        {
            var dto = new UpdateClubPostDto { Title = "Updated" };
            _mockPostService.Setup(s => s.UpdateAsync(1, dto, null))
                .ReturnsAsync(new ClubPostResponseDto { PostId = 1 });

            var result = await _controller.Update(1, dto, null);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenNull()
        {
            var dto = new UpdateClubPostDto();
            _mockPostService.Setup(s => s.UpdateAsync(99, dto, null))
                .ReturnsAsync((ClubPostResponseDto?)null);

            var result = await _controller.Update(99, dto, null);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsOk_WhenFound()
        {
            _mockPostService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

            var result = await _controller.Delete(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            _mockPostService.Setup(s => s.DeleteAsync(99)).ReturnsAsync(false);

            var result = await _controller.Delete(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UploadEditorImage_ReturnsBadRequest_WhenNoFile()
        {
            var result = await _controller.UploadEditorImage(null!);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadEditorImage_ReturnsOk_WhenSuccess()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(100);
            _mockFileStorage.Setup(s => s.SaveFileAsync(mockFile.Object, "clubposts"))
                .ReturnsAsync("https://cdn.example.com/image.jpg");

            var result = await _controller.UploadEditorImage(mockFile.Object);

            Assert.IsType<OkObjectResult>(result);
        }
    }
}
