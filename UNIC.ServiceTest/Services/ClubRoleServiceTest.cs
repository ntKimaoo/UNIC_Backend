using BusinessLogic.DTOs;
using BusinessLogic.Services.Implementation;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.DataAccess.Models;
using UNIC.DataAccess.Repositories.Interface;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class ClubRoleServiceTest
    {
        private readonly Mock<IClubRoleRepository> _mockClubRoleRepository;
        private readonly Mock<IDepartmentRepository> _mockDepartmentRepository;
        private readonly ClubRoleService _clubRoleService;

        public ClubRoleServiceTest()
        {
            _mockClubRoleRepository = new Mock<IClubRoleRepository>();
            _mockDepartmentRepository = new Mock<IDepartmentRepository>();
            
            _clubRoleService = new ClubRoleService(
                _mockClubRoleRepository.Object,
                _mockDepartmentRepository.Object
            );
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenRoleNotFound()
        {
            // Arrange
            _mockClubRoleRepository.Setup(repo => repo.GetByIdAsync(99, 1))
                                   .ReturnsAsync((ClubRole?)null);

            // Act
            var result = await _clubRoleService.GetByIdAsync(99, 1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenRoleNameExists()
        {
            // Arrange
            var createDto = new CreateClubRoleDto { RoleName = "Existing Role" };
            _mockClubRoleRepository.Setup(repo => repo.RoleNameExistsAsync(createDto.RoleName, 1))
                                   .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _clubRoleService.CreateAsync(createDto, 1));
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnRoleDto_WhenSuccessful()
        {
            // Arrange
            var createDto = new CreateClubRoleDto { RoleName = "New Role", policies = new List<int>() };
            var createdRole = new ClubRole { ClubRoleId = 1, RoleName = "New Role", ClubId = 1 };
            
            _mockClubRoleRepository.Setup(repo => repo.RoleNameExistsAsync(createDto.RoleName, 1))
                                   .ReturnsAsync(false);
            _mockClubRoleRepository.Setup(repo => repo.CreateAsync(It.IsAny<ClubRole>()))
                                   .ReturnsAsync(createdRole);
            _mockClubRoleRepository.Setup(repo => repo.GetByIdAsync(1, 1))
                                   .ReturnsAsync(createdRole);

            // Act
            var result = await _clubRoleService.CreateAsync(createDto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Role", result.RoleName);
            _mockClubRoleRepository.Verify(repo => repo.CreateAsync(It.IsAny<ClubRole>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnNull_WhenRoleNotFound()
        {
            // Arrange
            _mockClubRoleRepository.Setup(repo => repo.GetByIdAsync(99, 1))
                                   .ReturnsAsync((ClubRole?)null);

            // Act
            var result = await _clubRoleService.UpdateAsync(99, new UpdateClubRoleDto(), 1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteDepartmentAndRoles_WhenRoleIsManager()
        {
            // Arrange
            var managerRoleId = 1;
            var department = new Department { DepartmentId = 10, ManagerRoleId = managerRoleId };
            var deptRoles = new List<ClubRole> { new ClubRole { ClubRoleId = 2 } }; // Other role in dept

            _mockDepartmentRepository.Setup(repo => repo.GetByManagerRoleIdAsync(managerRoleId))
                                     .ReturnsAsync(department);
            _mockClubRoleRepository.Setup(repo => repo.GetByDepartmentIdAsync(department.DepartmentId))
                                   .ReturnsAsync(deptRoles);

            // Act
            var result = await _clubRoleService.DeleteAsync(managerRoleId);

            // Assert
            Assert.True(result);
            _mockDepartmentRepository.Verify(repo => repo.UpdateAsync(It.Is<Department>(d => d.ManagerRoleId == null)), Times.Once);
            _mockClubRoleRepository.Verify(repo => repo.DeleteAsync(2), Times.Once);
            _mockDepartmentRepository.Verify(repo => repo.DeleteAsync(10), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteRole_WhenRoleIsNotManager()
        {
            // Arrange
            var roleId = 1;
            _mockDepartmentRepository.Setup(repo => repo.GetByManagerRoleIdAsync(roleId))
                                     .ReturnsAsync((Department?)null);
            _mockClubRoleRepository.Setup(repo => repo.DeleteAsync(roleId))
                                   .ReturnsAsync(true);

            // Act
            var result = await _clubRoleService.DeleteAsync(roleId);

            // Assert
            Assert.True(result);
            _mockClubRoleRepository.Verify(repo => repo.DeleteAsync(roleId), Times.Once);
            _mockDepartmentRepository.Verify(repo => repo.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task AssignRoleAsync_ShouldUpdate_WhenUserRoleExists()
        {
            // Arrange
            var dto = new AssignClubRoleDto { UserId = Guid.NewGuid(), ClubId = 1, ClubRoleId = 2 };
            var existingUserRole = new UserClubRole { UserId = dto.UserId, ClubId = dto.ClubId, ClubRoleId = 1 };

            _mockClubRoleRepository.Setup(repo => repo.GetUserClubRoleAsync(dto.UserId, dto.ClubId))
                                   .ReturnsAsync(existingUserRole);
            _mockClubRoleRepository.Setup(repo => repo.UpdateUserClubRoleAsync(existingUserRole))
                                   .ReturnsAsync(true);

            // Act
            var result = await _clubRoleService.AssignRoleAsync(dto);

            // Assert
            Assert.True(result);
            Assert.Equal(2, existingUserRole.ClubRoleId);
            _mockClubRoleRepository.Verify(repo => repo.UpdateUserClubRoleAsync(existingUserRole), Times.Once);
        }
        
        [Fact]
        public async Task GetClubStructureAsync_ShouldReturnGroupedRoles()
        {
            // Arrange
            var clubId = 1;
            var roles = new List<ClubRole>
            {
                new ClubRole { ClubRoleId = 1, RoleName = "Standalone Role", DepartmentId = null },
                new ClubRole { ClubRoleId = 2, RoleName = "Manager Role", DepartmentId = 10 },
                new ClubRole { ClubRoleId = 3, RoleName = "Member Role", DepartmentId = 10 }
            };
            var departments = new List<Department>
            {
                new Department { DepartmentId = 10, DepartmentName = "IT Dept", ManagerRoleId = 2 }
            };

            _mockClubRoleRepository.Setup(repo => repo.GetAllAsync(clubId)).ReturnsAsync(roles);
            _mockDepartmentRepository.Setup(repo => repo.GetByClubIdAsync(clubId)).ReturnsAsync(departments);

            // Act
            var result = await _clubRoleService.GetClubStructureAsync(clubId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.StandaloneRoles);
            Assert.Single(result.Departments);
            
            var dept = result.Departments.First();
            Assert.Equal("IT Dept", dept.DepartmentName);
            Assert.NotNull(dept.Manager);
            Assert.Equal(2, dept.Manager.ClubRoleId);
            Assert.Single(dept.Roles); // Member Role
            Assert.Equal(3, dept.Roles.First().ClubRoleId);
        }
    }
}
