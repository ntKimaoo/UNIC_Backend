using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UNIC.BusinessLogic.DTOs;
using UNIC.BusinessLogic.Services.Interface;
using UNIC.Presentation.Controllers;
using Xunit;

namespace UNIC.Presentation.Test.Controllers
{
    public class DepartmentControllerTest
    {
        private readonly Mock<IDepartmentService> _mockService;
        private readonly DepartmentController _controller;

        public DepartmentControllerTest()
        {
            _mockService = new Mock<IDepartmentService>();
            _controller = new DepartmentController(_mockService.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetAllDepartmentsAsync())
                .ReturnsAsync(new List<DepartmentResponseDto> { new() });

            var result = await _controller.GetAll();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetAll_ReturnsNotFound_WhenEmpty()
        {
            _mockService.Setup(s => s.GetAllDepartmentsAsync())
                .ReturnsAsync(new List<DepartmentResponseDto>());

            var result = await _controller.GetAll();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetDepartmentById_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.GetDepartmentByIdAsync(1))
                .ReturnsAsync(new DepartmentResponseDto { DepartmentId = 1 });

            var result = await _controller.GetDepartmentById(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetDepartmentById_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetDepartmentByIdAsync(99))
                .ReturnsAsync((DepartmentResponseDto?)null);

            var result = await _controller.GetDepartmentById(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateDepartment_ReturnsOk_WhenFound()
        {
            var dto = new DepartmentResponseDto();
            _mockService.Setup(s => s.UpdateDepartmentAsync(1, dto)).ReturnsAsync(true);

            var result = await _controller.UpdateDepartment(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateDepartment_ReturnsNotFound_WhenMissing()
        {
            var dto = new DepartmentResponseDto();
            _mockService.Setup(s => s.UpdateDepartmentAsync(99, dto)).ReturnsAsync(false);

            var result = await _controller.UpdateDepartment(99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateDepartment_ReturnsCreated()
        {
            var request = new CreateDepartmentDto { Name = "IT" };
            _mockService.Setup(s => s.CreateDepartmentAsync(request))
                .ReturnsAsync(new DepartmentResponseDto { DepartmentId = 1 });

            var result = await _controller.CreateDepartment(request);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task DeleteDepartment_ReturnsOk_WhenFound()
        {
            _mockService.Setup(s => s.DeleteDepartmentAsync(1)).ReturnsAsync(true);

            var result = await _controller.DeleteDepartment(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteDepartment_ReturnsNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.DeleteDepartmentAsync(99)).ReturnsAsync(false);

            var result = await _controller.DeleteDepartment(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
