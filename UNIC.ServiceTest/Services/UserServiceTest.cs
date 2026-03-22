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

namespace UNIC.ServiceTest.Services
{
    public class UserServiceTest
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly UserService _userService;

        public UserServiceTest()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _userService = new UserService(_mockUserRepository.Object);
        }

        [Fact]
        public async Task GetAllUsersAsync_ShouldReturnAllUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserId = Guid.NewGuid(), Email = "user1@test.com", FullName = "User 1" },
                new User { UserId = Guid.NewGuid(), Email = "user2@test.com", FullName = "User 2" }
            };

            _mockUserRepository.Setup(repo => repo.GetAllAsync())
                               .ReturnsAsync(users);

            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, u => u.Email == "user1@test.com");
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnNull_WhenUserNotFound()
        {
            // Arrange
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>()))
                               .ReturnsAsync((User?)null);

            // Act
            var result = await _userService.GetUserByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnUser_WhenExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { UserId = userId, Email = "test@test.com", FullName = "Test User" };
            
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
                               .ReturnsAsync(user);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test@test.com", result.Email);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldThrowException_WhenEmailExists()
        {
            // Arrange
            var request = new CreateUserDto { Email = "existing@test.com", Password = "pwd", FullName = "Test" };
            _mockUserRepository.Setup(repo => repo.EmailExistsAsync(request.Email))
                               .ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _userService.CreateUserAsync(request));
            Assert.Equal("Email already exists.", ex.Message);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldThrowException_WhenStudentIdExists()
        {
            // Arrange
            var request = new CreateUserDto { Email = "new@test.com", StudentId = "12345", Password = "pwd", FullName = "Test" };
            _mockUserRepository.Setup(repo => repo.EmailExistsAsync(request.Email))
                               .ReturnsAsync(false);
            _mockUserRepository.Setup(repo => repo.StudentIdExistsAsync(request.StudentId))
                               .ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _userService.CreateUserAsync(request));
            Assert.Equal("Student ID already exists.", ex.Message);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldReturnUser_WhenSuccessful()
        {
            // Arrange
            var request = new CreateUserDto 
            { 
                Email = "new@test.com", 
                StudentId = "12345", 
                Password = "pwd", 
                FullName = "Test" 
            };
            
            var createdUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = request.Email,
                FullName = request.FullName,
                StudentId = request.StudentId,
                PasswordHash = "hashed-pwd",
                Status = "Active"
            };

            _mockUserRepository.Setup(repo => repo.EmailExistsAsync(request.Email)).ReturnsAsync(false);
            _mockUserRepository.Setup(repo => repo.StudentIdExistsAsync(request.StudentId)).ReturnsAsync(false);
            _mockUserRepository.Setup(repo => repo.CreateAsync(It.IsAny<User>())).ReturnsAsync(createdUser);

            // Act
            var result = await _userService.CreateUserAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Email, result.Email);
            Assert.Equal(request.StudentId, result.StudentId);
            _mockUserRepository.Verify(repo => repo.CreateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldReturnFalse_WhenUserNotFound()
        {
            // Arrange
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>()))
                               .ReturnsAsync((User?)null);

            // Act
            var result = await _userService.UpdateUserAsync(Guid.NewGuid(), new UpdateUserDto());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldThrowException_WhenStudentIdExistsForOtherUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { UserId = userId, StudentId = "old-id" };
            var request = new UpdateUserDto { StudentId = "new-id" };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUserRepository.Setup(repo => repo.StudentIdExistsAsync(request.StudentId)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _userService.UpdateUserAsync(userId, request));
            Assert.Equal("Student ID already exists.", ex.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldReturnTrue_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { UserId = userId, FullName = "Old Name" };
            var request = new UpdateUserDto { FullName = "New Name", Status = "Inactive" };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUserRepository.Setup(repo => repo.UpdateAsync(It.IsAny<User>())).ReturnsAsync(true);

            // Act
            var result = await _userService.UpdateUserAsync(userId, request);

            // Assert
            Assert.True(result);
            Assert.Equal("New Name", user.FullName);
            Assert.Equal("Inactive", user.Status);
            _mockUserRepository.Verify(repo => repo.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldReturnRepositoryResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserRepository.Setup(repo => repo.DeleteAsync(userId)).ReturnsAsync(true);

            // Act
            var result = await _userService.DeleteUserAsync(userId);

            // Assert
            Assert.True(result);
            _mockUserRepository.Verify(repo => repo.DeleteAsync(userId), Times.Once);
        }
    }
}
