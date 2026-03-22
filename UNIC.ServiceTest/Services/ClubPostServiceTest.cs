using BusinessLogic.DTOs;
using BusinessLogic.Services.Implementation;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class ClubPostServiceTest
    {
        private readonly Mock<IClubPostRepository> _mockPostRepo;
        private readonly ClubPostService _clubPostService;

        public ClubPostServiceTest()
        {
            _mockPostRepo = new Mock<IClubPostRepository>();
            _clubPostService = new ClubPostService(_mockPostRepo.Object);
        }

        #region Get Methods

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenPostNotFound()
        {
            _mockPostRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ClubPost?)null);
            var result = await _clubPostService.GetByIdAsync(1);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedDto_WhenPostFound()
        {
            var post = new ClubPost { PostId = 1, Title = "Test Title", Club = new Club { ClubName = "Test Club" } };
            _mockPostRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(post);
            
            var result = await _clubPostService.GetByIdAsync(1);
            
            Assert.NotNull(result);
            Assert.Equal("Test Title", result.Title);
            Assert.Equal("Test Club", result.ClubName);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedDtos()
        {
            var posts = new List<ClubPost> { new ClubPost { PostId = 1 } };
            _mockPostRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(posts);
            var result = await _clubPostService.GetAllAsync();
            Assert.Single(result);
        }

        [Fact]
        public async Task GetByClubIdAsync_ShouldReturnMappedDtos()
        {
            var posts = new List<ClubPost> { new ClubPost { PostId = 1, ClubId = 2 } };
            _mockPostRepo.Setup(r => r.GetByClubIdAsync(2)).ReturnsAsync(posts);
            var result = await _clubPostService.GetByClubIdAsync(2);
            Assert.Single(result);
            Assert.Equal(2, result.First().ClubId);
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnMappedDtos()
        {
            var userId = Guid.NewGuid();
            var posts = new List<ClubPost> { new ClubPost { PostId = 1, UserId = userId } };
            _mockPostRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(posts);
            var result = await _clubPostService.GetByUserIdAsync(userId);
            Assert.Single(result);
            Assert.Equal(userId, result.First().UserId);
        }

        #endregion

        #region CreateAsync

        [Fact]
        public async Task CreateAsync_ShouldCreatePublishedPost_WhenNoImageProvided()
        {
            // Arrange
            var dto = new CreateClubPostDto { ClubId = 1, UserId = Guid.NewGuid(), Title = "Test" };
            var post = new ClubPost { PostId = 10, Title = "Test", Status = "PUBLISHED" };
            
            _mockPostRepo.Setup(r => r.CreateAsync(It.IsAny<ClubPost>())).ReturnsAsync(post);
            _mockPostRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(post); // For reload

            // Act
            var result = await _clubPostService.CreateAsync(dto, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("PUBLISHED", result.Status);
            _mockPostRepo.Verify(r => r.CreateAsync(It.Is<ClubPost>(p => p.Status == "PUBLISHED")), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreatePendingPostAndEnqueue_WhenImageProvided()
        {
            // Arrange
            var dto = new CreateClubPostDto { ClubId = 1, UserId = Guid.NewGuid(), Title = "Test Image" };
            var post = new ClubPost { PostId = 10, Title = "Test", Status = "PENDING" };
            
            _mockPostRepo.Setup(r => r.CreateAsync(It.IsAny<ClubPost>())).ReturnsAsync(post);
            _mockPostRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(post);

            var mockFile = new Mock<IFormFile>();
            var content = "dummy image data";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<System.Threading.CancellationToken>()))
                    .Callback<Stream, System.Threading.CancellationToken>((stream, token) =>
                    {
                        ms.CopyTo(stream);
                    })
                    .Returns(Task.CompletedTask);
            mockFile.Setup(f => f.FileName).Returns("test.png");

            // Act
            var result = await _clubPostService.CreateAsync(dto, mockFile.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("PENDING", result.Status);
            _mockPostRepo.Verify(r => r.CreateAsync(It.Is<ClubPost>(p => p.Status == "PENDING")), Times.Once);
        }

        #endregion

        #region UpdateAsync

        [Fact]
        public async Task UpdateAsync_ShouldReturnNull_WhenPostNotFound()
        {
            _mockPostRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ClubPost?)null);
            var result = await _clubPostService.UpdateAsync(1, new UpdateClubPostDto(), null);
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateFieldsAndPublish_WhenNoImageProvided()
        {
            // Arrange
            var post = new ClubPost { PostId = 1, Title = "Old Title", Status = "PUBLISHED" };
            var dto = new UpdateClubPostDto { Title = "New Title", Caption = "Cap", Content = "Cont", Status = "DRAFT", ImageUrl = "url" };
            
            _mockPostRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(post);
            _mockPostRepo.Setup(r => r.UpdateAsync(post)).ReturnsAsync(true);

            // Act
            var result = await _clubPostService.UpdateAsync(1, dto, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Title", post.Title);
            Assert.Equal("Cap", post.Caption);
            Assert.Equal("Cont", post.Content);
            Assert.Equal("url", post.ImageUrl);
            Assert.Equal("DRAFT", post.Status);
            _mockPostRepo.Verify(r => r.UpdateAsync(post), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateStatusToPendingAndEnqueue_WhenImageProvided()
        {
            // Arrange
            var post = new ClubPost { PostId = 1, Title = "Title", Status = "PUBLISHED" };
            var dto = new UpdateClubPostDto { Title = "New Title" };
            
            _mockPostRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(post);
            _mockPostRepo.Setup(r => r.UpdateAsync(post)).ReturnsAsync(true);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<System.Threading.CancellationToken>()))
                    .Returns(Task.CompletedTask);
            mockFile.Setup(f => f.FileName).Returns("test2.png");

            // Act
            var result = await _clubPostService.UpdateAsync(1, dto, mockFile.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("PENDING", post.Status);
            _mockPostRepo.Verify(r => r.UpdateAsync(post), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnNull_WhenUpdateFailsInRepo()
        {
            var post = new ClubPost { PostId = 1 };
            _mockPostRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(post);
            _mockPostRepo.Setup(r => r.UpdateAsync(post)).ReturnsAsync(false);

            var result = await _clubPostService.UpdateAsync(1, new UpdateClubPostDto(), null);
            Assert.Null(result);
        }

        #endregion

        #region DeleteAsync

        [Fact]
        public async Task DeleteAsync_ShouldReturnRepoResult()
        {
            _mockPostRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);
            var result = await _clubPostService.DeleteAsync(1);
            Assert.True(result);
        }

        #endregion
    }
}
