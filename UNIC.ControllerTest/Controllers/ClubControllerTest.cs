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
    public class ClubControllerTest
    {
        private readonly Mock<IClubService> _mockClubService;
        private readonly Mock<IClubRoleService> _mockClubRoleService;
        private readonly ClubController _controller;

        public ClubControllerTest()
        {
            _mockClubService = new Mock<IClubService>();
            _mockClubRoleService = new Mock<IClubRoleService>();
            _controller = new ClubController(_mockClubService.Object, _mockClubRoleService.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #region GetAll

        [Fact]
        public async Task GetAll_ReturnsOk_WithListOfClubs()
        {
            var clubs = new List<ClubResponseDto>
            {
                new ClubResponseDto { ClubId = 1, ClubName = "Club A" },
                new ClubResponseDto { ClubId = 2, ClubName = "Club B" }
            };

            _mockClubService.Setup(s => s.GetAllAsync())
                            .ReturnsAsync(clubs);

            var result = await _controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithEmptyList()
        {
            _mockClubService.Setup(s => s.GetAllAsync())
                            .ReturnsAsync(new List<ClubResponseDto>());

            var result = await _controller.GetAll();

            Assert.IsType<OkObjectResult>(result);
        }
        [Fact]
        public async Task GetAll_WhenException_Returns500()
        {
            _mockClubService.Setup(s => s.GetAllAsync())
                            .ThrowsAsync(new Exception("Error"));

            var result = await _controller.GetAll();

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }
        #endregion

        #region GetActiveClubs

        [Fact]
        public async Task GetActiveClubs_ReturnsOk_WithData()
        {
            var clubs = new List<ClubResponseDto>
    {
        new ClubResponseDto { ClubId = 1, ClubName = "Club A" }
    };

            _mockClubService.Setup(s => s.GetActiveClubsAsync())
                            .ReturnsAsync(clubs);

            var result = await _controller.GetActiveClubs();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }
        [Fact]
        public async Task GetActiveClubs_ReturnsOk_WithEmptyList()
        {
            var clubs = new List<ClubResponseDto>();

            _mockClubService.Setup(s => s.GetActiveClubsAsync())
                            .ReturnsAsync(clubs);

            var result = await _controller.GetActiveClubs();

            Assert.IsType<OkObjectResult>(result);
        }
        [Fact]
        public async Task GetActiveClubs_WhenException_Returns500()
        {
            _mockClubService.Setup(s => s.GetActiveClubsAsync())
                            .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.GetActiveClubs();

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }
        #endregion

        #region GetPublicClubs

        [Fact]
        public async Task GetPublicClubs_ReturnsOk_WithPublicClubs()
        {
            var clubs = new List<ClubResponseDto>
            {
                new ClubResponseDto { ClubId = 1, ClubName = "Public Club", IsPublic = true }
            };

            _mockClubService.Setup(s => s.GetPublicClubsAsync())
                            .ReturnsAsync(clubs);

            var result = await _controller.GetPublicClubs();

            Assert.IsType<OkObjectResult>(result);
        }
        [Fact]
        public async Task GetPublicClubs_ReturnsOk_WithEmptyList()
        {
            var clubs = new List<ClubResponseDto>();

            _mockClubService.Setup(s => s.GetPublicClubsAsync())
                            .ReturnsAsync(clubs);

            var result = await _controller.GetPublicClubs();

            Assert.IsType<OkObjectResult>(result);
        }
        [Fact]
        public async Task GetPublicClubs_WhenException_Returns500()
        {
            _mockClubService.Setup(s => s.GetPublicClubsAsync())
                            .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.GetPublicClubs();

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }
        #endregion

        #region GetById

        [Fact]
        public async Task GetById_WhenClubExists_ReturnsOk()
        {
            var club = new ClubResponseDto
            {
                ClubId = 1,
                ClubName = "Club A"
            };

            _mockClubService.Setup(s => s.GetByIdAsync(1))
                            .ReturnsAsync(club);

            var result = await _controller.GetById(1);

            var okResult = Assert.IsType<OkObjectResult>(result);

            var value = okResult.Value!;
            var type = value.GetType();

            Assert.True((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.NotNull(type.GetProperty("data")!.GetValue(value));
        }

        [Fact]
        public async Task GetById_WhenNotFound_Returns404()
        {
            _mockClubService.Setup(s => s.GetByIdAsync(99))
                            .ReturnsAsync((ClubResponseDto?)null);

            var result = await _controller.GetById(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);

            var value = notFound.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("Club not found",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }
        [Fact]
        public async Task GetById_WhenException_Returns500()
        {
            _mockClubService.Setup(s => s.GetByIdAsync(1))
                            .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.GetById(1);

            var objectResult = Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);

            var value = objectResult.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("DB error",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }
        #endregion

        #region Create

        [Fact]
        public async Task Create_WhenModelStateInvalid_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("ClubName", "Required");

            var dto = new CreateClubDto();

            var result = await _controller.Create(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);

            var value = badRequest.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("Invalid data",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }

        [Fact]
        public async Task Create_WhenValid_ReturnsCreated()
        {
            var dto = new CreateClubDto
            {
                ClubName = "New Club"
            };

            var club = new ClubResponseDto
            {
                ClubId = 1,
                ClubName = "New Club"
            };

            _mockClubService.Setup(s => s.CreateAsync(dto))
                            .ReturnsAsync(club);

            var result = await _controller.Create(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);

            Assert.Equal(nameof(_controller.GetById), createdResult.ActionName);
            Assert.Equal(1, createdResult.RouteValues!["id"]);

            var value = createdResult.Value!;
            var type = value.GetType();

            Assert.True((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("Club created successfully",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }

        [Fact]
        public async Task Create_WhenInvalidOperationException_ReturnsBadRequest()
        {
            var dto = new CreateClubDto { ClubName = "Club A" };

            _mockClubService.Setup(s => s.CreateAsync(dto))
                            .ThrowsAsync(new InvalidOperationException("Club already exists"));

            var result = await _controller.Create(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);

            var value = badRequest.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("Club already exists",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }
        [Fact]
        public async Task Create_WhenException_Returns500()
        {
            var dto = new CreateClubDto { ClubName = "Club A" };

            _mockClubService.Setup(s => s.CreateAsync(dto))
                            .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.Create(dto);

            var objectResult = Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);

            var value = objectResult.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("An error occurred while creating the club",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }
        #endregion

        #region Update
        [Fact]
        public async Task Update_WhenModelInvalid_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("ClubName", "Required");

            var dto = new UpdateClubDto();

            var result = await _controller.Update(1, dto);

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
            var dto = new UpdateClubDto { ClubName = "Updated Club" };
            var updated = new ClubResponseDto { ClubId = 1, ClubName = "Updated Club" };

            _mockClubService.Setup(s => s.UpdateAsync(1, dto))
                            .ReturnsAsync(updated);

            var result = await _controller.Update(1, dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenClubNotExists()
        {
            var dto = new UpdateClubDto { ClubName = "Ghost Club" };

            _mockClubService.Setup(s => s.UpdateAsync(99, dto))
                            .ReturnsAsync((ClubResponseDto?)null);

            var result = await _controller.Update(99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenInvalidOperation()
        {
            var dto = new UpdateClubDto { ClubName = "Bad Club" };

            _mockClubService.Setup(s => s.UpdateAsync(1, dto))
                            .ThrowsAsync(new InvalidOperationException("Update constraint violated"));

            var result = await _controller.Update(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Update_Returns500_WhenUnexpectedException()
        {
            var dto = new UpdateClubDto { ClubName = "Error Club" };

            _mockClubService.Setup(s => s.UpdateAsync(1, dto))
                            .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.Update(1, dto);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region ChangeStatus

        [Fact]
        public async Task ChangeStatus_ReturnsOk_WhenSuccess()
        {
            _mockClubService.Setup(s => s.ChangeStatusClub(1))
                            .Returns(Task.CompletedTask);

            var result = await _controller.changStatus(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }
        [Fact]
        public async Task ChangeStatus_Returns500_WhenUnexpectedException()
        {
            _mockClubService.Setup(s => s.ChangeStatusClub(1))
                            .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.changStatus(1);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region SoftDelete

        [Fact]
        public async Task SoftDelete_ReturnsOk_WhenSuccess()
        {
            _mockClubService.Setup(s => s.SoftDeleteAsync(1))
                            .ReturnsAsync(true);

            var result = await _controller.SoftDelete(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task SoftDelete_ReturnsNotFound_WhenClubNotExists()
        {
            _mockClubService.Setup(s => s.SoftDeleteAsync(99))
                            .ReturnsAsync(false);

            var result = await _controller.SoftDelete(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        [Fact]
        public async Task SoftDelete_WhenException_Returns500()
        {
            _mockClubService.Setup(s => s.SoftDeleteAsync(1))
                            .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.SoftDelete(1);

            var objectResult = Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);

            var value = objectResult.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("An error occurred while updating the club",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }
        #endregion

        #region Delete

        [Fact]
        public async Task Delete_ReturnsOk_WhenSuccess()
        {
            _mockClubService.Setup(s => s.DeleteAsync(1))
                            .ReturnsAsync(true);

            var result = await _controller.Delete(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenClubNotExists()
        {
            _mockClubService.Setup(s => s.DeleteAsync(99))
                            .ReturnsAsync(false);

            var result = await _controller.Delete(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        [Fact]
        public async Task Delete_WhenException_Returns500()
        {
            _mockClubService.Setup(s => s.DeleteAsync(1))
                            .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.Delete(1);

            var objectResult = Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);

            var value = objectResult.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("An error occurred while updating the club",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }
        #endregion

        #region GetClubStructure

        [Fact]
        public async Task GetClubStructure_ReturnsOk_WhenClubExists()
        {
            var club = new ClubResponseDto { ClubId = 1, ClubName = "Club A" };
            var structure = new ClubStructureResponseDto
            {
                StandaloneRoles = new List<ClubStructureRoleDto>(),
                Departments = new List<ClubStructureDepartmentDto>()
            };

            _mockClubService.Setup(s => s.GetByIdAsync(1))
                            .ReturnsAsync(club);
            _mockClubRoleService.Setup(s => s.GetClubStructureAsync(1))
                                .ReturnsAsync(structure);

            var result = await _controller.GetClubStructure(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetClubStructure_ReturnsNotFound_WhenClubNotExists()
        {
            _mockClubService.Setup(s => s.GetByIdAsync(99))
                            .ReturnsAsync((ClubResponseDto?)null);

            var result = await _controller.GetClubStructure(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        [Fact]
        public async Task GetClubStructure_WhenException_Returns500()
        {
            _mockClubService.Setup(s => s.GetByIdAsync(1))
                            .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.GetClubStructure(1);

            var objectResult = Assert.IsType<ObjectResult>(result);

            Assert.Equal(500, objectResult.StatusCode);

            var value = objectResult.Value!;
            var type = value.GetType();

            Assert.False((bool)type.GetProperty("success")!.GetValue(value)!);
            Assert.Equal("An error occurred while updating the club",
                type.GetProperty("message")!.GetValue(value)?.ToString());
        }
        #endregion
    }
}
