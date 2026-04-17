using BusinessLogic.DTOs;
using BusinessLogic.Services.Implementation;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using FluentAssertions.Common;
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
        #region getById
        [Fact]
        public async Task GetByIdAsync_WhenFound_ReturnsDto()
        {
            var clubRole = new ClubRole
            {
                ClubRoleId = 1,
                RoleName = "Admin"
            };

            _mockClubRoleRepository.Setup(r => r.GetByIdAsync(1, 1))
                     .ReturnsAsync(clubRole);

            var result = await _clubRoleService.GetByIdAsync(1, 1);

            Assert.NotNull(result);
            Assert.Equal("Admin", result.RoleName);
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
        #endregion
        #region GetAll
        [Fact]
        public async Task GetAllAsync_WhenDataExists_ReturnsDtoList()
        {
            var roles = new List<ClubRole>
    {
        new ClubRole { ClubRoleId = 1, RoleName = "Admin" },
        new ClubRole { ClubRoleId = 2, RoleName = "Member" }
    };

            _mockClubRoleRepository.Setup(r => r.GetAllAsync(10))
                     .ReturnsAsync(roles);

            var result = await _clubRoleService.GetAllAsync(10);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, r => r.RoleName == "Admin");
        }

        #endregion
        #region Create
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
        #endregion
        #region Update
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
        public async Task UpdateAsync_WhenRoleNameExists_ThrowsException()
        {
            var role = new ClubRole { ClubRoleId = 1, RoleName = "Old" };

            var dto = new UpdateClubRoleDto
            {
                RoleName = "New"
            };

            _mockClubRoleRepository.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(role);
            _mockClubRoleRepository.Setup(r => r.RoleNameExistsAsync(dto.RoleName, 1))
                     .ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _clubRoleService.UpdateAsync(1, dto, 1)
            );
        }
        [Fact]
        public async Task UpdateAsync_WhenSameRoleName_DoesNotCheckDuplicate()
        {
            var role = new ClubRole { ClubRoleId = 1, RoleName = "Same" };

            var dto = new UpdateClubRoleDto
            {
                RoleName = "Same"
            };

            _mockClubRoleRepository.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(role);
            _mockClubRoleRepository.Setup(r => r.UpdateAsync(It.IsAny<ClubRole>()))
                     .ReturnsAsync(true);
            _mockClubRoleRepository.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(role);

            var result = await _clubRoleService.UpdateAsync(1, dto, 1);

            Assert.NotNull(result);

            _mockClubRoleRepository.Verify(r => r.RoleNameExistsAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }
        [Fact]
        public async Task UpdateAsync_WithoutPolicies_DoesNotCallSetPolicies()
        {
            var role = new ClubRole { ClubRoleId = 1, RoleName = "Old" };

            var dto = new UpdateClubRoleDto
            {
                Description = "New Desc"
            };

            _mockClubRoleRepository.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(role);
            _mockClubRoleRepository.Setup(r => r.UpdateAsync(It.IsAny<ClubRole>()))
                     .ReturnsAsync(true);
            _mockClubRoleRepository.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(role);

            var result = await _clubRoleService.UpdateAsync(1, dto, 1);

            Assert.NotNull(result);

            _mockClubRoleRepository.Verify(r => r.SetPoliciesAsync(It.IsAny<int>(), It.IsAny<List<int>>()), Times.Never);
        }
        [Fact]
        public async Task UpdateAsync_WithPolicies_CallsSetPolicies()
        {
            var role = new ClubRole { ClubRoleId = 1, RoleName = "Old" };

            var dto = new UpdateClubRoleDto
            {
                PolicyIds = new List<int> { 1, 2 }
            };

            _mockClubRoleRepository.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(role);
            _mockClubRoleRepository.Setup(r => r.UpdateAsync(It.IsAny<ClubRole>()))
                     .ReturnsAsync(true);
            _mockClubRoleRepository.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(role);

            await _clubRoleService.UpdateAsync(1, dto, 1);

            _mockClubRoleRepository.Verify(r => r.SetPoliciesAsync(1, dto.PolicyIds), Times.Once);
        }
        [Fact]
        public async Task UpdateAsync_WhenDepartmentIdNull_ShouldSetNull()
        {
            var role = new ClubRole { ClubRoleId = 1, DepartmentId = 5 };

            var dto = new UpdateClubRoleDto
            {
                DepartmentId = null
            };

            _mockClubRoleRepository.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(role);
            _mockClubRoleRepository.Setup(r => r.UpdateAsync(It.IsAny<ClubRole>()))
                     .ReturnsAsync(true);
            _mockClubRoleRepository.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(role);

            await _clubRoleService.UpdateAsync(1, dto, 1);

            Assert.Null(role.DepartmentId);
        }

        #endregion

        #region UpdatePolicy
        [Fact]
        public async Task UpdatePoliciesAsync_Should_Call_SetPoliciesAsync()
        {
            // Arrange
            int clubRoleId = 1;
            var policyIds = new List<int> { 1, 2, 3 };

            _mockClubRoleRepository
                .Setup(x => x.SetPoliciesAsync(clubRoleId, policyIds))
                .Returns(Task.CompletedTask);

            // Act
            await _clubRoleService.UpdatePoliciesAsync(clubRoleId, policyIds);

            // Assert
            _mockClubRoleRepository.Verify(
                x => x.SetPoliciesAsync(clubRoleId, policyIds),
                Times.Once
            );
        }
        #endregion
        #region Delete
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
        #endregion

        #region GetClubStructure
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
        #endregion
        #region AssignRole
        [Fact]
        public async Task AssignRoleAsync_WhenUserRoleNotExists_ShouldCreateNew()
        {
            // Arrange
            var dto = new AssignClubRoleDto
            {
                UserId = Guid.NewGuid(),
                ClubId = 100,
                ClubRoleIds = new List<int> { 5 }
            };

            _mockClubRoleRepository
                .Setup(x => x.GetUserClubRoleAsync(dto.UserId, dto.ClubId))
                .ReturnsAsync((UserClubRole)null);

            _mockClubRoleRepository
                .Setup(x => x.AddUserClubRoleAsync(It.IsAny<UserClubRole>()))
                .ReturnsAsync(true);

            // Act
            var result = await _clubRoleService.AssignRoleAsync(dto);

            // Assert
            Assert.True(result);

            _mockClubRoleRepository.Verify(x =>
                x.AddUserClubRoleAsync(It.Is<UserClubRole>(u =>
                    u.UserId == dto.UserId &&
                    u.ClubId == dto.ClubId &&
                    
                    u.Status == "ACTIVE"
                )),
                Times.Once);

            _mockClubRoleRepository.Verify(x =>
                x.UpdateUserClubRoleAsync(It.IsAny<UserClubRole>()),
                Times.Never);
        }
        #endregion
        #region GetUserClubRole
        [Fact]
        public async Task GetUserClubRoleAsync_ShouldReturnDataFromRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            int clubId = 1;

            var expected = new UserClubRole
            {
                UserId = userId,
                ClubId = clubId,
                
            };

            _mockClubRoleRepository
                .Setup(x => x.GetUserClubRoleAsync(userId, clubId))
                .ReturnsAsync(expected);

            // Act
            var result = await _clubRoleService.GetUserClubRoleAsync(userId, clubId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected, result);

            _mockClubRoleRepository.Verify(x =>
                x.GetUserClubRoleAsync(userId, clubId),
                Times.Once);
        }
        #endregion
        #region ManagedClub
        [Fact]
        public async Task GetManagedClubsAsync_WhenClubsExist_ShouldReturnMappedDtos()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var clubs = new List<Club>
    {
        new Club
        {
            ClubId = 1,
            ClubName = "Club A",
            Description = "Desc A"
        },
        new Club
        {
            ClubId = 2,
            ClubName = "Club B",
            Description = "Desc B"
        }
    };

            _mockClubRoleRepository
                .Setup(x => x.GetManagedClubsAsync(userId))
                .ReturnsAsync(clubs);

            // Act
            var result = await _clubRoleService.GetManagedClubsAsync(userId);

            // Assert
            Assert.Equal(2, result.Count);

            Assert.Equal("Club A", result[0].ClubName);
            Assert.Equal("Club B", result[1].ClubName);

            _mockClubRoleRepository.Verify(x =>
                x.GetManagedClubsAsync(userId), Times.Once);
        }
        #endregion
    }
}
