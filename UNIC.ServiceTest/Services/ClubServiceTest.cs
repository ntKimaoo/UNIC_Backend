using BusinessLogic.DTOs;
using BusinessLogic.Services.Implementation;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using FluentAssertions.Common;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ServiceTest.Services
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
        #region GetById
        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenClubNotFound()
        {
            // Arrange
            _mockClubRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
                               .ReturnsAsync((Club?)null);

            // Act
            var result = await _clubService.GetByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnClubDto_WhenClubExists()
        {
            // Arrange
            var club = new Club { ClubId = 1, ClubName = "Test Club" };
            _mockClubRepository.Setup(repo => repo.GetByIdAsync(1))
                               .ReturnsAsync(club);

            // Act
            var result = await _clubService.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ClubId);
            Assert.Equal("Test Club", result.ClubName);
        }
        #endregion
        #region GetAll

        [Fact]
        public async Task GetAllAsync_ShouldReturnListOfClubs()
        {
            // Arrange
            var clubs = new List<Club>
            {
                new Club { ClubId = 1, ClubName = "Club 1" },
                new Club { ClubId = 2, ClubName = "Club 2" }
            };
            _mockClubRepository.Setup(repo => repo.GetAllAsync())
                               .ReturnsAsync(clubs);

            // Act
            var result = await _clubService.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }
        #endregion
        #region GetActive
        [Fact]
        public async Task GetActiveClubsAsync_WhenDataExists_ReturnsDtoList()
        {
            var clubs = new List<Club>
            {
                new Club { ClubId = 1, ClubName = "Club 1",IsActive=true,IsDeleted=false },
            };
            _mockClubRepository.Setup(repo => repo.GetActiveClubsAsync())
                               .ReturnsAsync(clubs);


            var result = await _clubService.GetActiveClubsAsync();

            Assert.NotNull(result);
            Assert.Equal(1, result.Count());

        }
        #endregion
        #region GetPublic
        [Fact]
        public async Task GetPublicClubsAsync_WhenDataExists_ReturnsDtoList()
        {
            var clubs = new List<Club>
            {
                new Club { ClubId = 1, ClubName = "Club 1",IsPublic=true },
            };
            _mockClubRepository.Setup(repo => repo.GetPublicClubsAsync())
                               .ReturnsAsync(clubs);


            var result = await _clubService.GetPublicClubsAsync();

            Assert.NotNull(result);
            Assert.Equal(1,result.Count());
           
        }
        #endregion
        #region Create
        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenClubNameExists()
        {
            // Arrange
            var uid = Guid.NewGuid();
            var createDto = new CreateClubDto { ClubName = "Existing Club" };

            _mockClubRepository
                .Setup(repo => repo.ClubNameExistsAsync(createDto.ClubName))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _clubService.CreateAsync(uid, createDto));
        }


        [Fact]
        public async Task CreateAsync_ShouldReturnClubDto_WhenSuccessful()
        {
            // Arrange
            var uid = Guid.NewGuid();

            var createDto = new CreateClubDto
            {
                ClubName = "New Club",
                Status = "Active"
            };

            var createdClub = new Club
            {
                ClubId = 1,
                ClubName = "New Club",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _mockClubRepository
                .Setup(repo => repo.ClubNameExistsAsync(createDto.ClubName))
                .ReturnsAsync(false);

            _mockClubRepository
                .Setup(repo => repo.CreateAsync(uid, It.IsAny<Club>()))
                .ReturnsAsync(createdClub);

            // Act
            var result = await _clubService.CreateAsync(uid, createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ClubId);
            Assert.Equal("New Club", result.ClubName);

            _mockClubRepository.Verify(repo =>
                repo.CreateAsync(uid, It.IsAny<Club>()),
                Times.Once);
        }

        #endregion
        #region Update
        [Fact]
        public async Task UpdateAsync_ShouldReturnNull_WhenClubNotFound()
        {
            // Arrange
            _mockClubRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
                               .ReturnsAsync((Club?)null);

            // Act
            var result = await _clubService.UpdateAsync(99, new UpdateClubDto());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenNewNameExists()
        {
            // Arrange
            var existingClub = new Club { ClubId = 1, ClubName = "Old Name" };
            var updateDto = new UpdateClubDto { ClubName = "Taken Name" };
            
            _mockClubRepository.Setup(repo => repo.GetByIdAsync(1))
                               .ReturnsAsync(existingClub);
            _mockClubRepository.Setup(repo => repo.ClubNameExistsAsync(updateDto.ClubName))
                               .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _clubService.UpdateAsync(1, updateDto));
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnUpdatedClubDto_WhenSuccessful()
        {
            // Arrange
            var existingClub = new Club { ClubId = 1, ClubName = "Old Name" };
            var updateDto = new UpdateClubDto { ClubName = "New Name", Description = "Updated Desc" };
            
            _mockClubRepository.Setup(repo => repo.GetByIdAsync(1))
                               .ReturnsAsync(existingClub);
            _mockClubRepository.Setup(repo => repo.ClubNameExistsAsync(updateDto.ClubName))
                               .ReturnsAsync(false);
            _mockClubRepository.Setup(repo => repo.UpdateAsync(It.IsAny<Club>()))
                               .ReturnsAsync(true);

            // Act
            var result = await _clubService.UpdateAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Name", result.ClubName);
            Assert.Equal("Updated Desc", result.Description);
            _mockClubRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Club>()), Times.Once);
        }
        #endregion
        #region Delete
        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenSuccessful()
        {
            // Arrange
            _mockClubRepository.Setup(repo => repo.DeleteAsync(1))
                               .ReturnsAsync(true);

            // Act
            var result = await _clubService.DeleteAsync(1);

            // Assert
            Assert.True(result);
            _mockClubRepository.Verify(repo => repo.DeleteAsync(1), Times.Once);
        }
        #endregion
        #region SoftDelete
        [Fact]
        public async Task SoftDeleteAsync_ShouldReturnTrue_WhenSuccessful()
        {
            // Arrange
            _mockClubRepository.Setup(repo => repo.SoftDeleteAsync(1))
                               .ReturnsAsync(true);

            // Act
            var result = await _clubService.SoftDeleteAsync(1);

            // Assert
            Assert.True(result);
        }
        #endregion
        #region ChangeStatus
        [Fact]
        public async Task ChangeStatusClub_ShouldCallRepositoryOnce()
        {
            _mockClubRepository.Setup(r => r.ChangeStatusClub(1))
                     .Returns(Task.CompletedTask);

            await _clubService.ChangeStatusClub(1);

            _mockClubRepository.Verify(r => r.ChangeStatusClub(1), Times.Once);
        }
        #endregion
    }
}
