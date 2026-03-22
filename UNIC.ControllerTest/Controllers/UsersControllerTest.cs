using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ControllerTest.Controllers
{
    public class UsersControllerTest
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IClubRoleService> _mockClubRoleService;
        private readonly Mock<IFileStorageService> _mockFileStorageService;
        private readonly UsersController _controller;

        public UsersControllerTest()
        {
            _mockUserService = new Mock<IUserService>();
            _mockClubRoleService = new Mock<IClubRoleService>();
            _mockFileStorageService = new Mock<IFileStorageService>();
            _controller = new UsersController(
                _mockUserService.Object,
                _mockClubRoleService.Object,
                _mockFileStorageService.Object
            );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region GetAll

        [Fact]
        public async Task GetAll_ReturnsOk_WhenValid()
        {
            var paged = new PagedResultDto<UserResponseDto>
            {
                Items = new List<UserResponseDto> { new UserResponseDto { Email = "test@test.com" } },
                TotalCount = 1, PageNumber = 1, PageSize = 10
            };

            _mockUserService.Setup(s => s.GetPagedUsersAsync(1, 10))
                            .ReturnsAsync(paged);

            var result = await _controller.GetAll(1, 10);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetAll_ReturnsBadRequest_WhenInvalidPage()
        {
            var result = await _controller.GetAll(0, 10);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetAll_ReturnsBadRequest_WhenPageSizeTooLarge()
        {
            var result = await _controller.GetAll(1, 200);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region GetById

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var userId = Guid.NewGuid();
            var user = new UserResponseDto { UserId = userId, Email = "test@test.com" };

            _mockUserService.Setup(s => s.GetUserByIdAsync(userId))
                            .ReturnsAsync(user);

            var result = await _controller.GetById(userId);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenNull()
        {
            _mockUserService.Setup(s => s.GetUserByIdAsync(It.IsAny<Guid>()))
                            .ReturnsAsync((UserResponseDto?)null);

            var result = await _controller.GetById(Guid.NewGuid());

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region Create

        [Fact]
        public async Task Create_ReturnsCreated_WhenSuccess()
        {
            var request = new CreateUserDto { FullName = "Test", Email = "new@test.com", Password = "pwd" };
            var createdUser = new UserResponseDto { UserId = Guid.NewGuid(), Email = "new@test.com" };

            _mockUserService.Setup(s => s.CreateUserAsync(request))
                            .ReturnsAsync(createdUser);

            var result = await _controller.Create(request);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenServiceThrows()
        {
            var request = new CreateUserDto { FullName = "Test", Email = "exists@test.com", Password = "pwd" };

            _mockUserService.Setup(s => s.CreateUserAsync(request))
                            .ThrowsAsync(new Exception("Email already exists."));

            var result = await _controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region Update

        [Fact]
        public async Task Update_ReturnsOk_WhenFound()
        {
            var userId = Guid.NewGuid();
            var request = new UpdateUserDto { FullName = "New Name" };

            _mockUserService.Setup(s => s.UpdateUserAsync(userId, request))
                            .ReturnsAsync(true);

            var result = await _controller.Update(userId, request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenMissing()
        {
            var userId = Guid.NewGuid();
            var request = new UpdateUserDto { FullName = "New Name" };

            _mockUserService.Setup(s => s.UpdateUserAsync(userId, request))
                            .ReturnsAsync(false);

            var result = await _controller.Update(userId, request);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenServiceThrows()
        {
            var userId = Guid.NewGuid();
            var request = new UpdateUserDto { StudentId = "exists" };

            _mockUserService.Setup(s => s.UpdateUserAsync(userId, request))
                            .ThrowsAsync(new Exception("Student ID already exists."));

            var result = await _controller.Update(userId, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region Delete

        [Fact]
        public async Task Delete_ReturnsOk_WhenFound()
        {
            var userId = Guid.NewGuid();

            _mockUserService.Setup(s => s.DeleteUserAsync(userId))
                            .ReturnsAsync(true);

            var result = await _controller.Delete(userId);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            _mockUserService.Setup(s => s.DeleteUserAsync(It.IsAny<Guid>()))
                            .ReturnsAsync(false);

            var result = await _controller.Delete(Guid.NewGuid());

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetAllClub

        [Fact]
        public async Task GetAllClub_ReturnsOk_WhenFound()
        {
            var userId = Guid.NewGuid();
            var user = new UserResponseDto { UserId = userId };
            var clubs = new List<Club> { new Club { ClubId = 1, ClubName = "Test Club" } };

            _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockUserService.Setup(s => s.GetAllClubsById(userId)).ReturnsAsync(clubs);

            var result = await _controller.GetAllClub(userId);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetAllClub_ReturnsNotFound_WhenUserMissing()
        {
            _mockUserService.Setup(s => s.GetUserByIdAsync(It.IsAny<Guid>()))
                            .ReturnsAsync((UserResponseDto?)null);

            var result = await _controller.GetAllClub(Guid.NewGuid());

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetAllClub_ReturnsNotFound_WhenNoClubs()
        {
            var userId = Guid.NewGuid();
            var user = new UserResponseDto { UserId = userId };

            _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockUserService.Setup(s => s.GetAllClubsById(userId)).ReturnsAsync(new List<Club>());

            var result = await _controller.GetAllClub(userId);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetManagedClubs

        [Fact]
        public async Task GetManagedClubs_ReturnsOk()
        {
            var userId = Guid.NewGuid();

            _mockClubRoleService.Setup(s => s.GetManagedClubsAsync(userId))
                                .ReturnsAsync(new List<ClubResponseDto>());

            var result = await _controller.GetManagedClubs(userId);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion
    }
}
