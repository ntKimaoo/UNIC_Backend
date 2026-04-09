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
using DataAccess.Models;

namespace UNIC.ControllerTest.Controllers
{
    public class ClubRoleControllerTest
    {
        private readonly Mock<IClubRoleService> _mockService;
        private readonly ClubRoleController _controller;

        public ClubRoleControllerTest()
        {
            _mockService = new Mock<IClubRoleService>();
            _controller = new ClubRoleController(_mockService.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #region GetAll

        [Fact]
        public async Task GetAll_ReturnsOk_WithListOfRoles()
        {
            var roles = new List<ClubRoleResponseDto>
            {
                new ClubRoleResponseDto { ClubRoleId = 1, RoleName = "President" },
                new ClubRoleResponseDto { ClubRoleId = 2, RoleName = "Secretary" }
            };

            _mockService.Setup(s => s.GetAllAsync(1))
                        .ReturnsAsync(roles);

            var result = await _controller.GetAll(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithEmptyList()
        {
            _mockService.Setup(s => s.GetAllAsync(1))
                        .ReturnsAsync(new List<ClubRoleResponseDto>());

            var result = await _controller.GetAll(1);

            Assert.IsType<OkObjectResult>(result);
        }
        [Fact]
        public async Task GetAllRoles_WhenException_Returns500()
        {
            _mockService.Setup(s => s.GetAllAsync(1))
                                .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.GetAll(1);

            var objectResult = Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);

            var value = objectResult.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("An error occurred while updating the club",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }
        #endregion

        #region GetById

        [Fact]
        public async Task GetById_ReturnsOk_WhenRoleExists()
        {
            var role = new ClubRoleResponseDto { ClubRoleId = 1, RoleName = "President", clubId = 1 };

            _mockService.Setup(s => s.GetByIdAsync(1, 1))
                        .ReturnsAsync(role);

            var result = await _controller.GetById(1, 1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenRoleNotExists()
        {
            _mockService.Setup(s => s.GetByIdAsync(99, 1))
                        .ReturnsAsync((ClubRoleResponseDto?)null);

            var result = await _controller.GetById(99, 1);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        [Fact]
        public async Task GetById_WhenException_Returns500()
        {
            _mockService.Setup(s => s.GetByIdAsync(1, 1))
                                .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.GetById(1, 1);

            var objectResult = Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);

            var value = objectResult.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("An error occurred while updating the club",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }
        #endregion

        #region Create

        [Fact]
        public async Task Create_ReturnsCreated_WhenSuccess()
        {
            var dto = new CreateClubRoleDto { RoleName = "Treasurer" };
            var created = new ClubRoleResponseDto { ClubRoleId = 3, RoleName = "Treasurer", clubId = 1 };

            _mockService.Setup(s => s.CreateAsync(dto, 1))
                        .ReturnsAsync(created);

            var result = await _controller.Create(dto, 1);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
        }

        [Fact]
        public async Task Create_WhenModelStateInvalid_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("Name", "Required");

            var dto = new CreateClubRoleDto();

            var result = await _controller.Create(dto, 1);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);

            var value = badRequest.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("Invalid data",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }

        [Fact]
        public async Task Create_Returns500_WhenUnexpectedException()
        {
            var dto = new CreateClubRoleDto { RoleName = "Error Role" };

            _mockService.Setup(s => s.CreateAsync(dto, 1))
                        .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.Create(dto, 1);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region Update

        [Fact]
        public async Task Update_WhenModelStateInvalid_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("Name", "Required");

            var dto = new UpdateClubRoleDto();

            var result = await _controller.Update(1, dto, 10);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);

            var value = badRequest.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("Invalid data",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }
        [Fact]
        public async Task Update_ReturnsOk_WhenSuccess()
        {
            var dto = new UpdateClubRoleDto { RoleName = "Vice President" };
            var updated = new ClubRoleResponseDto { ClubRoleId = 1, RoleName = "Vice President", clubId = 1 };

            _mockService.Setup(s => s.UpdateAsync(1, dto, 1))
                        .ReturnsAsync(updated);

            var result = await _controller.Update(1, dto, 1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenRoleNotExists()
        {
            var dto = new UpdateClubRoleDto { RoleName = "Ghost Role" };

            _mockService.Setup(s => s.UpdateAsync(99, dto, 1))
                        .ReturnsAsync((ClubRoleResponseDto?)null);

            var result = await _controller.Update(99, dto, 1);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Update_Returns500_WhenUnexpectedException()
        {
            var dto = new UpdateClubRoleDto { RoleName = "Error Role" };

            _mockService.Setup(s => s.UpdateAsync(1, dto, 1))
                        .ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.Update(1, dto, 1);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region UpdatePolicies

        [Fact]
        public async Task UpdatePolicies_ReturnsOk_WhenSuccess()
        {
            var policyIds = new List<int> { 1, 2 };

            _mockService.Setup(s => s.UpdatePoliciesAsync(1, policyIds))
                        .Returns(Task.CompletedTask);

            var result = await _controller.UpdatePolicies(1, policyIds, 1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task UpdatePolicies_WhenModelStateInvalid_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("policyIds", "Required");

            var result = await _controller.UpdatePolicies(1, new List<int>(), 10);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);

            var value = badRequest.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("Invalid data",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }

        [Fact]
        public async Task UpdatePolicies_Returns500_WhenUnexpectedException()
        {
            var policyIds = new List<int> { 1 };

            _mockService.Setup(s => s.UpdatePoliciesAsync(1, policyIds))
                        .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.UpdatePolicies(1, policyIds, 1);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region Delete

        [Fact]
        public async Task Delete_ReturnsOk_WhenSuccess()
        {
            _mockService.Setup(s => s.DeleteAsync(1))
                        .ReturnsAsync(true);

            var result = await _controller.Delete(1, 1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenRoleNotExists()
        {
            _mockService.Setup(s => s.DeleteAsync(99))
                        .ReturnsAsync(false);

            var result = await _controller.Delete(1, 99);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        [Fact]
        public async Task Delete_WhenException_Returns500()
        {
            _mockService.Setup(s => s.DeleteAsync(1))
                                .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.Delete(1, 1);

            var objectResult = Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);

            var value = objectResult.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("An error occurred",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }
        #endregion

        #region AssignRole

        [Fact]
        public async Task AssignRole_ReturnsOk_WhenSuccess()
        {
            var dto = new AssignClubRoleDto
            {
                UserId = Guid.NewGuid(),
                ClubId = 1,
                ClubRoleId = 2
            };

            _mockService.Setup(s => s.AssignRoleAsync(dto))
                        .ReturnsAsync(true);

            var result = await _controller.AssignRole(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task AssignRole_ReturnsBadRequest_WhenCannotAssign()
        {
            var dto = new AssignClubRoleDto
            {
                UserId = Guid.NewGuid(),
                ClubId = 1,
                ClubRoleId = 99
            };

            _mockService.Setup(s => s.AssignRoleAsync(dto))
                        .ReturnsAsync(false);

            var result = await _controller.AssignRole(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }
        [Fact]
        public async Task AssignRole_WhenException_Returns500()
        {
            var dto = new AssignClubRoleDto
            {
                UserId = Guid.NewGuid(),
                ClubRoleId = 1
            };

            _mockService.Setup(s => s.AssignRoleAsync(dto))
                                .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.AssignRole(dto);

            var objectResult = Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);

            var value = objectResult.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("An error occurred",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }
        #endregion

        #region GetUserClubRole

        [Fact]
        public async Task GetUserClubRole_ReturnsOk_WhenFound()
        {
            var userId = Guid.NewGuid();

            _mockService.Setup(s => s.GetUserClubRoleAsync(userId, 1))
                        .ReturnsAsync(new UserClubRole());

            var result = await _controller.GetUserClubRole(userId, 1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetUserClubRole_ReturnsNotFound_WhenNotFound()
        {
            var userId = Guid.NewGuid();

            _mockService.Setup(s => s.GetUserClubRoleAsync(userId, 1))
                        .ReturnsAsync((UserClubRole?)null);

            var result = await _controller.GetUserClubRole(userId, 1);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        [Fact]
        public async Task GetUserClubRole_WhenException_Returns500()
        {
            var userId = Guid.NewGuid();

            _mockService.Setup(s => s.GetUserClubRoleAsync(userId, 1))
                                .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.GetUserClubRole(userId, 1);

            var objectResult = Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);

            var value = objectResult.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("An error occurred",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }
        #endregion
    }
}
