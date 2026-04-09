using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using UNIC.BusinessLogic.DTOs;
using UNIC.BusinessLogic.Services.Interface;
using UNIC.Presentation.Controllers;
using DataAccess.Models;
using Xunit;
using UNIC.DataAccess.Models;

namespace UNIC.ControllerTest.Controllers
{
    public class DepartmentControllerTest
    {
        private readonly Mock<IDepartmentService> _mockDepartmentService;
        private readonly DepartmentController _controller;

        public DepartmentControllerTest()
        {
            _mockDepartmentService = new Mock<IDepartmentService>();
            _controller = new DepartmentController(_mockDepartmentService.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #region GetDepartmentsByClub

        [Fact]
        public async Task GetDepartmentsByClub_ReturnsOk_WithListOfDepartments_WhenFound()
        {
            // Arrange
            int clubId = 1;
            var departments = new List<DepartmentResponseDto>
            {
                new DepartmentResponseDto { DepartmentId = 1, Name = "Dep 1" },
                new DepartmentResponseDto { DepartmentId = 2, Name = "Dep 2" }
            };

            _mockDepartmentService.Setup(s => s.GetDepartmentsByClubIdAsync(clubId))
                .ReturnsAsync(departments);

            // Act
            var result = await _controller.GetDepartmentsByClub(clubId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            using var json = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
            var root = json.RootElement;
            Assert.True(root.GetProperty("success").GetBoolean());
            Assert.Equal(2, root.GetProperty("data").GetArrayLength());

            _mockDepartmentService.Verify(s => s.GetDepartmentsByClubIdAsync(clubId), Times.Once);
        }

        [Fact]
        public async Task GetDepartmentsByClub_ReturnsNotFound_WhenEmpty()
        {
            // Arrange
            int clubId = 1;

            _mockDepartmentService.Setup(s => s.GetDepartmentsByClubIdAsync(clubId))
                .ReturnsAsync(new List<DepartmentResponseDto>());

            // Act
            var result = await _controller.GetDepartmentsByClub(clubId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        #endregion

        #region GetDepartmentById

        [Fact]
        public async Task GetDepartmentById_ReturnsOk_WhenDepartmentExists_AndBelongsToClub()
        {
            // Arrange
            int clubId = 1;
            int departmentId = 1;
            var department = new DepartmentResponseDto { DepartmentId = departmentId, ClubId = clubId, Name = "Dep 1" };

            _mockDepartmentService.Setup(s => s.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync(department);

            // Act
            var result = await _controller.GetDepartmentById(clubId, departmentId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            using var json = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
            var root = json.RootElement;
            Assert.True(root.GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task GetDepartmentById_ReturnsNotFound_WhenDepartmentIsNull()
        {
            // Arrange
            int clubId = 1;
            int departmentId = 1;

            _mockDepartmentService.Setup(s => s.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync((DepartmentResponseDto)null);

            // Act
            var result = await _controller.GetDepartmentById(clubId, departmentId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task GetDepartmentById_ReturnsNotFound_WhenDepartmentDoesNotBelongToClub()
        {
            // Arrange
            int clubId = 1;
            int departmentId = 1;
            var department = new DepartmentResponseDto { DepartmentId = departmentId, ClubId = 2, Name = "Dep 1" };

            _mockDepartmentService.Setup(s => s.GetDepartmentByIdAsync(departmentId))
                .ReturnsAsync(department);

            // Act
            var result = await _controller.GetDepartmentById(clubId, departmentId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        #endregion

        #region CreateDepartment

        [Fact]
        public async Task CreateDepartment_ReturnsCreatedAtAction_WhenSuccessful()
        {
            // Arrange
            int clubId = 1;
            var request = new CreateDepartmentDto { Name = "New Dep", Description = "Desc" };
            var createdDto = new DepartmentResponseDto { DepartmentId = 1, ClubId = clubId, Name = "New Dep", Description = "Desc" };

            _mockDepartmentService.Setup(s => s.CreateDepartmentAsync(clubId, request))
                .ReturnsAsync(createdDto);

            // Act
            var result = await _controller.CreateDepartment(clubId, request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
            Assert.Equal(nameof(DepartmentController.GetDepartmentById), createdResult.ActionName);
        }

        [Fact]
        public async Task CreateDepartment_ReturnsBadRequest_WhenModelStateInvalid()
        {
            // Arrange
            int clubId = 1;
            var request = new CreateDepartmentDto();
            _controller.ModelState.AddModelError("Name", "Required");

            // Act
            var result = await _controller.CreateDepartment(clubId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }


        [Fact]
        public async Task CreateDepartment_ReturnsStatusCode500_WhenGenericExceptionThrown()
        {
            // Arrange
            int clubId = 1;
            var request = new CreateDepartmentDto { Name = "New Dep" };

            _mockDepartmentService.Setup(s => s.CreateDepartmentAsync(clubId, request))
                .ThrowsAsync(new Exception("Database context closed"));

            // Act
            var result = await _controller.CreateDepartment(clubId, request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        #region UpdateDepartment

        [Fact]
        public async Task UpdateDepartment_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            int clubId = 1;
            int id = 1;
            var request = new UpdateDepartmentDto { Name = "Updated" };
            var updatedDto = new DepartmentResponseDto { DepartmentId = id, ClubId = clubId, Name = "Updated" };

            _mockDepartmentService.Setup(s => s.UpdateDepartmentAsync(clubId, id, request))
                .ReturnsAsync(updatedDto);

            // Act
            var result = await _controller.UpdateDepartment(clubId, id, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task UpdateDepartment_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Required");

            // Act
            var result = await _controller.UpdateDepartment(1, 1, new UpdateDepartmentDto());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task UpdateDepartment_ReturnsNotFound_WhenServiceReturnsNull()
        {
            // Arrange
            int clubId = 1;
            int id = 1;
            var request = new UpdateDepartmentDto { Name = "Updated" };

            _mockDepartmentService.Setup(s => s.UpdateDepartmentAsync(clubId, id, request))
                .ReturnsAsync((DepartmentResponseDto)null);

            // Act
            var result = await _controller.UpdateDepartment(clubId, id, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }


        [Fact]
        public async Task UpdateDepartment_Returns500_WhenExceptionThrown()
        {
            // Arrange
            int clubId = 1;
            int id = 1;
            var request = new UpdateDepartmentDto { Name = "Updated" };

            _mockDepartmentService.Setup(s => s.UpdateDepartmentAsync(clubId, id, request))
                .ThrowsAsync(new Exception("Error"));

            // Act
            var result = await _controller.UpdateDepartment(clubId, id, request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        #endregion

        #region DeleteDepartment

        [Fact]
        public async Task DeleteDepartment_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            int clubId = 1;
            int id = 1;

            _mockDepartmentService.Setup(s => s.DeleteDepartmentAsync(clubId, id))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteDepartment(clubId, id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task DeleteDepartment_ReturnsNotFound_WhenServiceReturnsFalse()
        {
            // Arrange
            int clubId = 1;
            int id = 1;

            _mockDepartmentService.Setup(s => s.DeleteDepartmentAsync(clubId, id))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteDepartment(clubId, id);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        #endregion

        #region GetDepartmentMembers

        [Fact]
        public async Task GetDepartmentMembers_ReturnsOk_WhenMembersFound()
        {
            // Arrange
            int clubId = 1;
            int departmentId = 1;
            var members = new List<DepartmentMemberDto>
            {
                new DepartmentMemberDto { UserId = Guid.NewGuid(), FullName = "User 1" }
            };

            _mockDepartmentService.Setup(s => s.GetDepartmentMembersAsync(clubId, departmentId))
                .ReturnsAsync(members);

            // Act
            var result = await _controller.GetDepartmentMembers(clubId, departmentId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetDepartmentMembers_ReturnsNotFound_WhenMembersNull()
        {
            // Arrange
            int clubId = 1;
            int departmentId = 1;

            _mockDepartmentService.Setup(s => s.GetDepartmentMembersAsync(clubId, departmentId))
                .ReturnsAsync((IEnumerable<DepartmentMemberDto>)null);

            // Act
            var result = await _controller.GetDepartmentMembers(clubId, departmentId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        #endregion

        #region AddMemberToDepartment

        [Fact]
        public async Task AddMemberToDepartment_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            int clubId = 1;
            int departmentId = 1;
            Guid userId = Guid.NewGuid();
            var returnedModel = new UserClubRoleDepartment();

            _mockDepartmentService.Setup(s => s.AddMemberTodepartment(clubId, userId, departmentId))
                .ReturnsAsync(returnedModel);

            // Act
            var result = await _controller.AddMemberToDepartment(clubId, departmentId, userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        #endregion

        #region RemoveMemberFromDepartment

        [Fact]
        public async Task RemoveMemberFromDepartment_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            int clubId = 1;
            int departmentId = 1;
            Guid userId = Guid.NewGuid();
            var returnedModel = new UserClubRoleDepartment();

            _mockDepartmentService.Setup(s => s.RemoveMemberFromDepartment(clubId, userId, departmentId))
                .ReturnsAsync(returnedModel);

            // Act
            var result = await _controller.RemoveMemberFromDepartment(clubId, departmentId, userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        #endregion
    }
}
