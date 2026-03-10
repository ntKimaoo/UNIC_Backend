using BusinessLogic.DTOs;
using DataAccess.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.BusinessLogic.DTOs;
using UNIC.BusinessLogic.Services.Implementation;
using UNIC.DataAccess.Repositories.Interface;
using Xunit;

namespace UNIC.BusinessLogic.Test.Services
{
    public class DepartmentServiceTest
    {
        private readonly Mock<IDepartmentRepository> _mockRepo;
        private readonly DepartmentService _departmentService;

        public DepartmentServiceTest()
        {
            _mockRepo = new Mock<IDepartmentRepository>();
            _departmentService = new DepartmentService(_mockRepo.Object);
        }

        #region CreateDepartmentAsync

        [Fact]
        public async Task CreateDepartmentAsync_ShouldReturnMappedDto()
        {
            var request = new CreateDepartmentDto { Name = "IT Dept", Description = "Tech stuff", ClubId = 1 };
            var createdEntity = new Department { DepartmentId = 1, DepartmentName = "IT Dept", Description = "Tech stuff" };

            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Department>())).ReturnsAsync(createdEntity);

            var result = await _departmentService.CreateDepartmentAsync(request);

            Assert.NotNull(result);
            Assert.Equal(1, result.DepartmentId);
            Assert.Equal("IT Dept", result.Name);
            _mockRepo.Verify(r => r.CreateAsync(It.IsAny<Department>()), Times.Once);
        }

        #endregion

        #region DeleteDepartmentAsync

        [Fact]
        public async Task DeleteDepartmentAsync_ShouldReturnRepoResult()
        {
            _mockRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);
            var result = await _departmentService.DeleteDepartmentAsync(1);
            Assert.True(result);
        }

        #endregion

        #region GetAllDepartmentsAsync

        [Fact]
        public async Task GetAllDepartmentsAsync_ShouldReturnMappedDtos()
        {
            var entities = new List<Department> { new Department { DepartmentId = 1 } };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(entities);

            var result = await _departmentService.GetAllDepartmentsAsync();

            Assert.Single(result);
            Assert.Equal(1, result.First().DepartmentId);
        }

        #endregion

        #region GetDepartmentByIdAsync

        [Fact]
        public async Task GetDepartmentByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Department?)null);
            var result = await _departmentService.GetDepartmentByIdAsync(1);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetDepartmentByIdAsync_ShouldReturnMappedDto_WhenFound()
        {
            var entity = new Department { DepartmentId = 1, DepartmentName = "HR" };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

            var result = await _departmentService.GetDepartmentByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("HR", result.Name);
        }

        #endregion

        #region UpdateDepartmentAsync

        [Fact]
        public async Task UpdateDepartmentAsync_ShouldReturnFalse_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Department?)null);
            var result = await _departmentService.UpdateDepartmentAsync(1, new DepartmentResponseDto());
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateDepartmentAsync_ShouldReturnFalse_WhenIdsDontMatch()
        {
            var entity = new Department { DepartmentId = 1 };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

            var dto = new DepartmentResponseDto { DepartmentId = 2 };
            var result = await _departmentService.UpdateDepartmentAsync(1, dto);
            
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateDepartmentAsync_ShouldUpdateAndReturnTrue_WhenValid()
        {
            var entity = new Department { DepartmentId = 1, DepartmentName = "Old Name" };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
            _mockRepo.Setup(r => r.UpdateAsync(entity)).ReturnsAsync(true);

            var dto = new DepartmentResponseDto { DepartmentId = 1, Name = "New Name", Description = "Desc" };
            var result = await _departmentService.UpdateDepartmentAsync(1, dto);
            
            Assert.True(result);
            Assert.Equal("New Name", entity.DepartmentName);
            Assert.Equal("Desc", entity.Description);
            _mockRepo.Verify(r => r.UpdateAsync(entity), Times.Once);
        }

        #endregion
    }
}
