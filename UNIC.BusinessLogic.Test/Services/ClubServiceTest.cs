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
    public class ClubServiceTest
    {
        private readonly Mock<IClubRepository> _mockClubRepository;
        private readonly ClubService _clubService;

        public ClubServiceTest()
        {
            _mockClubRepository = new Mock<IClubRepository>();
            _clubService = new ClubService(_mockClubRepository.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            _mockClubRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
                               .ReturnsAsync((Club?)null);

            // Act
            var result = await _clubService.GetByIdAsync(1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnDto_WhenExists()
        {
            // Arrange
            var clubId = 1;
            var club = new Club { ClubId = clubId, ClubName = "IT Club", Status = "Active" };
            _mockClubRepository.Setup(repo => repo.GetByIdAsync(clubId))
                               .ReturnsAsync(club);

            // Act
            var result = await _clubService.GetByIdAsync(clubId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("IT Club", result.ClubName);
            Assert.Equal(clubId, result.ClubId);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllClubs()
        {
            // Arrange
            var clubs = new List<Club>
            {
                new Club { ClubId = 1, ClubName = "Club 1" },
                new Club { ClubId = 2, ClubName = "Club 2" }
            };
            _mockClubRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(clubs);

            // Act
            var result = await _clubService.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetActiveClubsAsync_ShouldReturnActiveClubs()
        {
            // Arrange
            var clubs = new List<Club> { new Club { ClubId = 1, Status = "Active" } };
            _mockClubRepository.Setup(repo => repo.GetActiveClubsAsync()).ReturnsAsync(clubs);

            // Act
            var result = await _clubService.GetActiveClubsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetPublicClubsAsync_ShouldReturnPublicClubs()
        {
            // Arrange
            var clubs = new List<Club> { new Club { ClubId = 1, IsPublic = true } };
            _mockClubRepository.Setup(repo => repo.GetPublicClubsAsync()).ReturnsAsync(clubs);

            // Act
            var result = await _clubService.GetPublicClubsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenNameExists()
        {
            // Arrange
            var dto = new CreateClubDto { ClubName = "Existing Name" };
            _mockClubRepository.Setup(repo => repo.ClubNameExistsAsync(dto.ClubName)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _clubService.CreateAsync(dto));
            Assert.Equal("Club name already exists", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnDto_WhenCreated()
        {
            // Arrange
            var dto = new CreateClubDto { ClubName = "New Club" };
            var createdClub = new Club { ClubId = 1, ClubName = dto.ClubName, Status = "Active" };
            
            _mockClubRepository.Setup(repo => repo.ClubNameExistsAsync(dto.ClubName)).ReturnsAsync(false);
            _mockClubRepository.Setup(repo => repo.CreateAsync(It.IsAny<Club>())).ReturnsAsync(createdClub);

            // Act
            var result = await _clubService.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Club", result.ClubName);
            _mockClubRepository.Verify(repo => repo.CreateAsync(It.IsAny<Club>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnNull_WhenClubNotFound()
        {
            // Arrange
            _mockClubRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Club?)null);

            // Act
            var result = await _clubService.UpdateAsync(1, new UpdateClubDto());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenNewNameExistsForOtherClub()
        {
            // Arrange
            var club = new Club { ClubId = 1, ClubName = "Old Name" };
            var dto = new UpdateClubDto { ClubName = "Taken Name" };
            
            _mockClubRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(club);
            _mockClubRepository.Setup(repo => repo.ClubNameExistsAsync(dto.ClubName)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _clubService.UpdateAsync(1, dto));
            Assert.Equal("Club name already exists", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_ShouldAllowSameNameUpdate_WhenNameIsItsOwn()
        {
            // Arrange
            var club = new Club { ClubId = 1, ClubName = "Same Name" };
            var dto = new UpdateClubDto { ClubName = "Same Name" };
            
            _mockClubRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(club);
            _mockClubRepository.Setup(repo => repo.ClubNameExistsAsync(dto.ClubName)).ReturnsAsync(true); // Exists because it's its own
            _mockClubRepository.Setup(repo => repo.UpdateAsync(It.IsAny<Club>())).ReturnsAsync(true);

            // Act
            var result = await _clubService.UpdateAsync(1, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Same Name", result.ClubName);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateFieldsAndReturnDto_WhenValid()
        {
            // Arrange
            var club = new Club { ClubId = 1, ClubName = "Old Name" };
            var dto = new UpdateClubDto 
            { 
                ClubName = "New Name",
                ShortName = "NN",
                Description = "Desc",
                FoundedDate = DateTime.Now,
                Status = "Inactive",
                IsPublic = true,
                LogoUrl = "logo.png",
                CoverImageUrl = "cover.png",
                Email = "email@test.com",
                PhoneNumber = "123",
                FacebookUrl = "fb",
                WebsiteUrl = "web",
                Address = "addr",
                IsActive = true
            };
            
            _mockClubRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(club);
            _mockClubRepository.Setup(repo => repo.ClubNameExistsAsync(dto.ClubName)).ReturnsAsync(false);
            _mockClubRepository.Setup(repo => repo.UpdateAsync(club)).ReturnsAsync(true);

            // Act
            var result = await _clubService.UpdateAsync(1, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Name", result.ClubName);
            Assert.Equal("NN", result.ShortName);
            Assert.Equal("Inactive", result.Status);
            Assert.True(result.IsPublic);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnNull_WhenUpdateFailsInRepo()
        {
            // Arrange
            var club = new Club { ClubId = 1, ClubName = "Old Name" };
            var dto = new UpdateClubDto { ClubName = "New Name" };
            
            _mockClubRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(club);
            _mockClubRepository.Setup(repo => repo.ClubNameExistsAsync(dto.ClubName)).ReturnsAsync(false);
            _mockClubRepository.Setup(repo => repo.UpdateAsync(club)).ReturnsAsync(false); // Repo fails

            // Act
            var result = await _clubService.UpdateAsync(1, dto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnRepositoryResult()
        {
            // Arrange
            _mockClubRepository.Setup(repo => repo.DeleteAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _clubService.DeleteAsync(1);

            // Assert
            Assert.True(result);
            _mockClubRepository.Verify(repo => repo.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task SoftDeleteAsync_ShouldReturnRepositoryResult()
        {
            // Arrange
            _mockClubRepository.Setup(repo => repo.SoftDeleteAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _clubService.SoftDeleteAsync(1);

            // Assert
            Assert.True(result);
            _mockClubRepository.Verify(repo => repo.SoftDeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task ChangeStatusClub_ShouldCallRepository()
        {
            // Arrange
            _mockClubRepository.Setup(repo => repo.ChangeStatusClub(1)).Returns(Task.CompletedTask);

            // Act
            await _clubService.ChangeStatusClub(1);

            // Assert
            _mockClubRepository.Verify(repo => repo.ChangeStatusClub(1), Times.Once);
        }
    }
}
