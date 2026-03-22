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

        #endregion

        #region GetActiveClubs

        [Fact]
        public async Task GetActiveClubs_ReturnsOk_WithActiveClubs()
        {
            var clubs = new List<ClubResponseDto>
            {
                new ClubResponseDto { ClubId = 1, ClubName = "Active Club", IsActive = true }
            };

            _mockClubService.Setup(s => s.GetActiveClubsAsync())
                            .ReturnsAsync(clubs);

            var result = await _controller.GetActiveClubs();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
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

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        #endregion

        #region GetById

        [Fact]
        public async Task GetById_ReturnsOk_WhenClubExists()
        {
            var club = new ClubResponseDto { ClubId = 1, ClubName = "Club A" };

            _mockClubService.Setup(s => s.GetByIdAsync(1))
                            .ReturnsAsync(club);

            var result = await _controller.GetById(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenClubNotExists()
        {
            _mockClubService.Setup(s => s.GetByIdAsync(99))
                            .ReturnsAsync((ClubResponseDto?)null);

            var result = await _controller.GetById(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region Create

        [Fact]
        public async Task Create_ReturnsCreated_WhenSuccess()
        {
            var dto = new CreateClubDto { ClubName = "New Club" };
            var created = new ClubResponseDto { ClubId = 1, ClubName = "New Club" };

            _mockClubService.Setup(s => s.CreateAsync(dto))
                            .ReturnsAsync(created);

            var result = await _controller.Create(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenInvalidOperation()
        {
            var dto = new CreateClubDto { ClubName = "Duplicate Club" };

            _mockClubService.Setup(s => s.CreateAsync(dto))
                            .ThrowsAsync(new InvalidOperationException("Club with this name already exists"));

            var result = await _controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_Returns500_WhenUnexpectedException()
        {
            var dto = new CreateClubDto { ClubName = "Error Club" };

            _mockClubService.Setup(s => s.CreateAsync(dto))
                            .ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.Create(dto);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region Update

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
        public async Task ChangeStatus_ReturnsBadRequest_WhenInvalidOperation()
        {
            _mockClubService.Setup(s => s.ChangeStatusClub(99))
                            .ThrowsAsync(new InvalidOperationException("Club not found"));

            var result = await _controller.changStatus(99);

            Assert.IsType<BadRequestObjectResult>(result);
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

        #endregion
    }
}
