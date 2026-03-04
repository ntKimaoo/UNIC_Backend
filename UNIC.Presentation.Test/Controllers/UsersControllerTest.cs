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
    public class UsersControllerTest
    {
        private readonly Mock<IUserService> _mockService;
        private readonly UsersController _controller;

        public UsersControllerTest()
        {
            _mockService = new Mock<IUserService>();
            _controller = new UsersController(_mockService.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetAllUsersAsync())
                .ReturnsAsync(new List<UserResponseDto> { new() });

            var result = await _controller.GetAll();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetAll_ReturnsNotFound_WhenEmpty()
        {
            _mockService.Setup(s => s.GetAllUsersAsync())
                .ReturnsAsync(new List<UserResponseDto>());

            var result = await _controller.GetAll();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.GetUserByIdAsync(id))
                .ReturnsAsync(new UserResponseDto { UserId = id });

            var result = await _controller.GetById(id);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenNull()
        {
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.GetUserByIdAsync(id))
                .ReturnsAsync((UserResponseDto?)null);

            var result = await _controller.GetById(id);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WhenSuccess()
        {
            var request = new CreateUserDto { Email = "new@test.com", FullName = "Alice", Password = "pass123" };
            var created = new UserResponseDto { UserId = Guid.NewGuid() };
            _mockService.Setup(s => s.CreateUserAsync(request)).ReturnsAsync(created);

            var result = await _controller.Create(request);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenServiceThrows()
        {
            var request = new CreateUserDto { Email = "dup@test.com", FullName = "Dup", Password = "pass123" };
            _mockService.Setup(s => s.CreateUserAsync(request))
                .ThrowsAsync(new Exception("Email already exists"));

            var result = await _controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsOk_WhenFound()
        {
            var id = Guid.NewGuid();
            var request = new UpdateUserDto { FullName = "New Name" };
            _mockService.Setup(s => s.UpdateUserAsync(id, request)).ReturnsAsync(true);

            var result = await _controller.Update(id, request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenMissing()
        {
            var id = Guid.NewGuid();
            var request = new UpdateUserDto { FullName = "X" };
            _mockService.Setup(s => s.UpdateUserAsync(id, request)).ReturnsAsync(false);

            var result = await _controller.Update(id, request);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenServiceThrows()
        {
            var id = Guid.NewGuid();
            var request = new UpdateUserDto { FullName = "X" };
            _mockService.Setup(s => s.UpdateUserAsync(id, request))
                .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.Update(id, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsOk_WhenFound()
        {
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.DeleteUserAsync(id)).ReturnsAsync(true);

            var result = await _controller.Delete(id);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.DeleteUserAsync(id)).ReturnsAsync(false);

            var result = await _controller.Delete(id);

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
