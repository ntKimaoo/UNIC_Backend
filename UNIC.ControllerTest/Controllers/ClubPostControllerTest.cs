using BusinessLogic.DTOs;
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
    public class ClubPostControllerTest
    {
        private readonly Mock<IClubPostService> _mockClubPostService;
        private readonly Mock<IFileStorageService> _mockFileStorageService;
        private readonly ClubPostController _controller;

        public ClubPostControllerTest()
        {
            _mockClubPostService = new Mock<IClubPostService>();
            _mockFileStorageService = new Mock<IFileStorageService>();
            _controller = new ClubPostController(
                _mockClubPostService.Object,
                _mockFileStorageService.Object
            );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region GetAll

        [Fact]
        public async Task GetAll_ReturnsOk()
        {
            var posts = new List<ClubPostResponseDto>
            {
                new ClubPostResponseDto { PostId = 1, Title = "Post 1" }
            };

            _mockClubPostService.Setup(s => s.GetByClubIdAsync(1))
                                .ReturnsAsync(posts);

            var result = await _controller.GetAll(1);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region GetById

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var post = new ClubPostResponseDto { PostId = 1, Title = "Post 1" };

            _mockClubPostService.Setup(s => s.GetByIdAsync(1))
                                .ReturnsAsync(post);

            var result = await _controller.GetById(1, 1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenNull()
        {
            _mockClubPostService.Setup(s => s.GetByIdAsync(99))
                                .ReturnsAsync((ClubPostResponseDto?)null);

            var result = await _controller.GetById(1, 99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetByUserId

        [Fact]
        public async Task GetByUserId_ReturnsOk()
        {
            var userId = Guid.NewGuid();
            var posts = new List<ClubPostResponseDto>
            {
                new ClubPostResponseDto { PostId = 1, UserId = userId }
            };

            _mockClubPostService.Setup(s => s.GetByUserIdAsync(userId))
                                .ReturnsAsync(posts);

            var result = await _controller.GetByUserId(1, userId);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region Create

        [Fact]
        public async Task Create_ReturnsCreated_WhenSuccess()
        {
            var dto = new CreateClubPostDto { ClubId = 1, UserId = Guid.NewGuid(), Title = "New Post" };
            var created = new ClubPostResponseDto { PostId = 1, Title = "New Post" };

            _mockClubPostService.Setup(s => s.CreateAsync(dto, null))
                                .ReturnsAsync(created);

            var result = await _controller.Create(1, dto, null);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenServiceThrows()
        {
            var dto = new CreateClubPostDto { ClubId = 1, UserId = Guid.NewGuid(), Title = "Bad" };

            _mockClubPostService.Setup(s => s.CreateAsync(dto, null))
                                .ThrowsAsync(new Exception("Creation failed"));

            var result = await _controller.Create(1, dto, null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region Update

        [Fact]
        public async Task Update_ReturnsOk_WhenSuccess()
        {
            var dto = new UpdateClubPostDto { Title = "Updated" };
            var updated = new ClubPostResponseDto { PostId = 1, Title = "Updated" };

            _mockClubPostService.Setup(s => s.UpdateAsync(1, dto, null))
                                .ReturnsAsync(updated);

            var result = await _controller.Update(1, 1, dto, null);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenNull()
        {
            var dto = new UpdateClubPostDto { Title = "Missing" };

            _mockClubPostService.Setup(s => s.UpdateAsync(99, dto, null))
                                .ReturnsAsync((ClubPostResponseDto?)null);

            var result = await _controller.Update(1, 99, dto, null);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region Delete

        [Fact]
        public async Task Delete_ReturnsOk_WhenFound()
        {
            _mockClubPostService.Setup(s => s.DeleteAsync(1))
                                .ReturnsAsync(true);

            var result = await _controller.Delete(1, 1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            _mockClubPostService.Setup(s => s.DeleteAsync(99))
                                .ReturnsAsync(false);

            var result = await _controller.Delete(1, 99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region UploadEditorImage

        [Fact]
        public async Task UploadEditorImage_ReturnsBadRequest_WhenNoFile()
        {
            var result = await _controller.UploadEditorImage(1, null!);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadEditorImage_ReturnsOk_WhenSuccess()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(100);
            mockFile.Setup(f => f.FileName).Returns("image.png");

            _mockFileStorageService.Setup(s => s.SaveFileAsync(mockFile.Object, "clubposts"))
                                   .ReturnsAsync("https://storage.example.com/image.png");

            var result = await _controller.UploadEditorImage(1, mockFile.Object);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion
    }
}
