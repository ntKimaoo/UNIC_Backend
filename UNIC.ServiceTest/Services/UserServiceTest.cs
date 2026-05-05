using BusinessLogic.DTOs;
using BusinessLogic.Services.Implementation;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using BusinessLogic.Services.Interface;
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
        private readonly Mock<IAttendanceService> _mockAttendanceService;
        private readonly UserService _userService;

        public UserServiceTest()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockAttendanceService = new Mock<IAttendanceService>();
            _userService = new UserService(_mockUserRepository.Object, _mockAttendanceService.Object);
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

        #region GetPagedUsersAsync

        [Fact]
        public async Task GetPagedUsersAsync_ReturnsPagedResult()
        {
            var users = new List<User>
            {
                new User { UserId = Guid.NewGuid(), Email = "a@test.com", FullName = "A" },
                new User { UserId = Guid.NewGuid(), Email = "b@test.com", FullName = "B" }
            };

            _mockUserRepository.Setup(r => r.GetPagedAsync(1, 10))
                               .ReturnsAsync((users.AsEnumerable(), 20));

            var result = await _userService.GetPagedUsersAsync(1, 10);

            Assert.Equal(2, result.Items.Count());
            Assert.Equal(20, result.TotalCount);
            Assert.Equal(2, result.TotalPages);
            Assert.False(result.HasPreviousPage);
            Assert.True(result.HasNextPage);
        }

        [Fact]
        public async Task GetPagedUsersAsync_ReturnsEmpty_WhenNoUsers()
        {
            _mockUserRepository.Setup(r => r.GetPagedAsync(1, 10))
                               .ReturnsAsync((Enumerable.Empty<User>(), 0));

            var result = await _userService.GetPagedUsersAsync(1, 10);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
            Assert.Equal(0, result.TotalPages);
        }

        [Fact]
        public async Task GetPagedUsersAsync_HasPreviousPage_WhenPage2()
        {
            var users = new List<User> { new User { UserId = Guid.NewGuid(), Email = "a@t.com", FullName = "A" } };
            _mockUserRepository.Setup(r => r.GetPagedAsync(2, 10))
                               .ReturnsAsync((users.AsEnumerable(), 15));

            var result = await _userService.GetPagedUsersAsync(2, 10);

            Assert.True(result.HasPreviousPage);
            Assert.False(result.HasNextPage);
        }

        #endregion

        #region GetAllClubsById

        [Fact]
        public async Task GetAllClubsById_ReturnsClubs()
        {
            var userId = Guid.NewGuid();
            var clubs = new List<Club> { new Club { ClubId = 1, ClubName = "Test" } };
            _mockUserRepository.Setup(r => r.GetAllClubByUser(userId)).ReturnsAsync(clubs);

            var result = await _userService.GetAllClubsById(userId);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetAllClubsById_ReturnsEmpty_WhenNoClubs()
        {
            var userId = Guid.NewGuid();
            _mockUserRepository.Setup(r => r.GetAllClubByUser(userId)).ReturnsAsync(new List<Club>());

            var result = await _userService.GetAllClubsById(userId);

            Assert.Empty(result);
        }

        #endregion

        #region GetUserDepartmentsInClubAsync

        [Fact]
        public async Task GetUserDepartmentsInClubAsync_ReturnsNull_WhenNotMember()
        {
            var userId = Guid.NewGuid();
            _mockUserRepository.Setup(r => r.GetAllClubByUser(userId))
                               .ReturnsAsync(new List<Club>());

            var result = await _userService.GetUserDepartmentsInClubAsync(userId, 1);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserDepartmentsInClubAsync_ReturnsDepartments_WhenMember()
        {
            var userId = Guid.NewGuid();
            var clubs = new List<Club> { new Club { ClubId = 1 } };
            _mockUserRepository.Setup(r => r.GetAllClubByUser(userId)).ReturnsAsync(clubs);

            var dept = new Department { DepartmentId = 10, DepartmentName = "IT", ClubRoles = new List<ClubRole>() };
            var deptData = new List<(Department Department, ClubRole? DepartmentRole, int MemberCount)>
            {
                (dept, null, 5)
            };
            _mockUserRepository.Setup(r => r.GetDepartmentsByUserAndClubAsync(userId, 1)).ReturnsAsync(deptData);

            var result = await _userService.GetUserDepartmentsInClubAsync(userId, 1);

            Assert.NotNull(result);
            Assert.Single(result!);
            var first = result!.First();
            Assert.Equal(10, first.DepartmentId);
            Assert.Null(first.DepartmentRole);
        }

        [Fact]
        public async Task GetUserDepartmentsInClubAsync_IncludesRole_WhenPresent()
        {
            var userId = Guid.NewGuid();
            var clubs = new List<Club> { new Club { ClubId = 1 } };
            _mockUserRepository.Setup(r => r.GetAllClubByUser(userId)).ReturnsAsync(clubs);

            var dept = new Department { DepartmentId = 10, DepartmentName = "IT", ClubRoles = new List<ClubRole>() };
            var role = new ClubRole { ClubRoleId = 1, RoleName = "Lead", Level = 2, Description = "Dept Lead" };
            var deptData = new List<(Department Department, ClubRole? DepartmentRole, int MemberCount)>
            {
                (dept, role, 3)
            };
            _mockUserRepository.Setup(r => r.GetDepartmentsByUserAndClubAsync(userId, 1)).ReturnsAsync(deptData);

            var result = await _userService.GetUserDepartmentsInClubAsync(userId, 1);

            Assert.NotNull(result);
            var first = result!.First();
            Assert.NotNull(first.DepartmentRole);
            Assert.Equal("Lead", first.DepartmentRole!.RoleName);
        }

        #endregion

        #region UpdateUserAsync_AllFields

        [Fact]
        public async Task UpdateUserAsync_UpdatesAllFields()
        {
            var userId = Guid.NewGuid();
            var user = new User { UserId = userId, FullName = "Old" };
            var request = new UpdateUserDto
            {
                FullName = "New",
                PhoneNumber = "0912345678",
                DateOfBirth = new DateOnly(2000, 1, 1),
                Gender = "Male",
                Address = "123 Street",
                Avatar = "http://img.com/a.jpg",
                Major = "CS",
                StudentId = "S001"
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUserRepository.Setup(r => r.StudentIdExistsAsync("S001")).ReturnsAsync(false);
            _mockUserRepository.Setup(r => r.UpdateAsync(user)).ReturnsAsync(true);

            var result = await _userService.UpdateUserAsync(userId, request);

            Assert.True(result);
            Assert.Equal("New", user.FullName);
            Assert.Equal("0912345678", user.PhoneNumber);
            Assert.Equal("Male", user.Gender);
            Assert.Equal("CS", user.Major);
            Assert.Equal("S001", user.StudentId);
        }

        [Fact]
        public async Task UpdateUserAsync_SkipsStudentIdCheck_WhenSameAsExisting()
        {
            var userId = Guid.NewGuid();
            var user = new User { UserId = userId, StudentId = "SAME-ID" };
            var request = new UpdateUserDto { StudentId = "SAME-ID" };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUserRepository.Setup(r => r.UpdateAsync(user)).ReturnsAsync(true);

            var result = await _userService.UpdateUserAsync(userId, request);

            Assert.True(result);
            _mockUserRepository.Verify(r => r.StudentIdExistsAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region CreateUserAsync_Additional

        [Fact]
        public async Task CreateUserAsync_WithoutStudentId()
        {
            var request = new CreateUserDto
            {
                Email = "new@test.com",
                Password = "pwd",
                FullName = "No Student"
            };

            _mockUserRepository.Setup(r => r.EmailExistsAsync(request.Email)).ReturnsAsync(false);
            _mockUserRepository.Setup(r => r.CreateAsync(It.IsAny<User>()))
                               .ReturnsAsync((User u) => u);

            var result = await _userService.CreateUserAsync(request);

            Assert.NotNull(result);
            Assert.Equal("new@test.com", result.Email);
            _mockUserRepository.Verify(r => r.StudentIdExistsAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion
    }
}
