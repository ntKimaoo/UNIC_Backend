using BusinessLogic.DTOs;
using BusinessLogic.Services.Implementation;
using DataAccess.Models;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.BusinessLogic.DTOs;
using UNIC.BusinessLogic.Services.Implementation;
using UNIC.DataAccess.Models;
using Xunit;
using DataAccess.Repositories.Interface;
using DataAccess.Repositories.Implementation;
using UNIC.DataAccess.Repositories.Interface;

namespace UNIC.ServiceTest.Services
{
    public class DepartmentServiceTest
    {
        private readonly Mock<IDepartmentRepository> _mockDepartmentRepository;
        private readonly Mock<IClubRepository> _mockClubRepository;
        private readonly Mock<IClubRoleRepository> _mockClubRoleRepository;
        private readonly Mock<IClubMemberRepository> _mockClubMemberRepository;
        private readonly DepartmentService _departmentService;

        public DepartmentServiceTest()
        {
            _mockDepartmentRepository = new Mock<IDepartmentRepository>();
            _mockClubRepository = new Mock<IClubRepository>();
            _mockClubRoleRepository = new Mock<IClubRoleRepository>();
            _mockClubMemberRepository = new Mock<IClubMemberRepository>();

            _departmentService = new DepartmentService(
                _mockDepartmentRepository.Object,
                _mockClubRepository.Object,
                _mockClubRoleRepository.Object,
                _mockClubMemberRepository.Object
            );
        }

        #region CreateDepartmentAsync

        [Fact]
        public async Task CreateDepartmentAsync_ShouldThrowException_WhenClubNotFound()
        {
            // Arrange
            int clubId = 1;
            var request = new CreateDepartmentDto { Name = "IT" };
            _mockClubRepository.Setup(r => r.ExistsAsync(clubId)).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _departmentService.CreateDepartmentAsync(clubId, request));
        }

        [Fact]
        public async Task CreateDepartmentAsync_ShouldThrowException_WhenNameExists()
        {
            // Arrange
            int clubId = 1;
            var request = new CreateDepartmentDto { Name = "IT" };
            _mockClubRepository.Setup(r => r.ExistsAsync(clubId)).ReturnsAsync(true);
            _mockDepartmentRepository.Setup(r => r.DepartmentNameExistsInClubAsync(request.Name, clubId, null))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _departmentService.CreateDepartmentAsync(clubId, request));
        }

        [Fact]
        public async Task CreateDepartmentAsync_ShouldSucceed_AndLinkManagerRole()
        {
            // Arrange
            int clubId = 1;
            var request = new CreateDepartmentDto { Name = "IT", Description = "IT Dept", ManagerRoleLevel = 2 };
            _mockClubRepository.Setup(r => r.ExistsAsync(clubId)).ReturnsAsync(true);
            _mockDepartmentRepository.Setup(r => r.DepartmentNameExistsInClubAsync(request.Name, clubId, null))
                .ReturnsAsync(false);

            var createdDept = new Department { DepartmentId = 10, DepartmentName = "IT", ClubId = clubId };
            _mockDepartmentRepository.Setup(r => r.CreateAsync(It.IsAny<Department>()))
                .ReturnsAsync(createdDept);

            var createdRole = new ClubRole { ClubRoleId = 100, RoleName = "IT 's Manager", Level = 2 };
            _mockClubRoleRepository.Setup(r => r.CreateAsync(It.IsAny<ClubRole>()))
                .ReturnsAsync(createdRole);

            // Act
            var result = await _departmentService.CreateDepartmentAsync(clubId, request);

            // Assert
            result.Should().NotBeNull();
            result.DepartmentId.Should().Be(10);
            result.ManagerRoleId.Should().Be(100);
            result.ManagerRole.Should().NotBeNull();
            result.ManagerRole.RoleName.Should().Be("IT 's Manager");

            _mockDepartmentRepository.Verify(r => r.CreateAsync(It.Is<Department>(d => d.DepartmentName == "IT")), Times.Once);
            _mockClubRoleRepository.Verify(r => r.CreateAsync(It.Is<ClubRole>(cr => cr.Level == 2 && cr.DepartmentId == 10)), Times.Once);
            _mockDepartmentRepository.Verify(r => r.UpdateAsync(It.Is<Department>(d => d.ManagerRoleId == 100)), Times.Once);
        }

        #endregion

        #region DeleteDepartmentAsync

        [Fact]
        public async Task DeleteDepartmentAsync_ShouldReturnFalse_WhenNotFound()
        {
            // Arrange
            _mockDepartmentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Department?)null);

            // Act
            var result = await _departmentService.DeleteDepartmentAsync(1, 1);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteDepartmentAsync_ShouldReturnFalse_WhenWrongClub()
        {
            // Arrange
            var dept = new Department { DepartmentId = 1, ClubId = 2 };
            _mockDepartmentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dept);

            // Act
            var result = await _departmentService.DeleteDepartmentAsync(1, 1);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteDepartmentAsync_ShouldReturnTrue_WhenSuccessful()
        {
            // Arrange
            var dept = new Department { DepartmentId = 1, ClubId = 1 };
            _mockDepartmentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dept);
            _mockDepartmentRepository.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _departmentService.DeleteDepartmentAsync(1, 1);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region Getters (All, ByClub, ById)

        [Fact]
        public async Task GetAllDepartmentsAsync_ShouldReturnList()
        {
            // Arrange
            var depts = new List<Department> { new Department { DepartmentId = 1, DepartmentName = "D1" } };
            _mockDepartmentRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(depts);

            // Act
            var result = await _departmentService.GetAllDepartmentsAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().Name.Should().Be("D1");
        }

        [Fact]
        public async Task GetDepartmentsByClubIdAsync_ShouldReturnList()
        {
            // Arrange
            var depts = new List<Department> { new Department { DepartmentId = 1, ClubId = 1, DepartmentName = "D1" } };
            _mockDepartmentRepository.Setup(r => r.GetByClubIdAsync(1)).ReturnsAsync(depts);

            // Act
            var result = await _departmentService.GetDepartmentsByClubIdAsync(1);

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetDepartmentByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            _mockDepartmentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Department?)null);

            // Act
            var result = await _departmentService.GetDepartmentByIdAsync(1);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetDepartmentByIdAsync_ShouldReturnDto_WhenFound()
        {
            // Arrange
            var dept = new Department { DepartmentId = 1, DepartmentName = "D1" };
            _mockDepartmentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dept);

            // Act
            var result = await _departmentService.GetDepartmentByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("D1");
        }

        #endregion

        #region UpdateDepartmentAsync

        [Fact]
        public async Task UpdateDepartmentAsync_ShouldReturnNull_WhenNotFoundOrWrongClub()
        {
            // Arrange
            _mockDepartmentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Department?)null);

            // Act
            var result1 = await _departmentService.UpdateDepartmentAsync(1, 1, new UpdateDepartmentDto());
            
            var dept = new Department { DepartmentId = 1, ClubId = 2 };
            _mockDepartmentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dept);
            var result2 = await _departmentService.UpdateDepartmentAsync(1, 1, new UpdateDepartmentDto());

            // Assert
            result1.Should().BeNull();
            result2.Should().BeNull();
        }

        [Fact]
        public async Task UpdateDepartmentAsync_ShouldThrowException_WhenNewNameExists()
        {
            // Arrange
            var dept = new Department { DepartmentId = 1, ClubId = 1, DepartmentName = "Old" };
            _mockDepartmentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dept);
            _mockDepartmentRepository.Setup(r => r.DepartmentNameExistsInClubAsync("New", 1, 1)).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _departmentService.UpdateDepartmentAsync(1, 1, new UpdateDepartmentDto { Name = "New" }));
        }

        [Fact]
        public async Task UpdateDepartmentAsync_ShouldReturnNull_WhenRepoUpdateFails()
        {
            // Arrange
            var dept = new Department { DepartmentId = 1, ClubId = 1 };
            _mockDepartmentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dept);
            _mockDepartmentRepository.Setup(r => r.UpdateAsync(dept)).ReturnsAsync(false);

            // Act
            var result = await _departmentService.UpdateDepartmentAsync(1, 1, new UpdateDepartmentDto());

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateDepartmentAsync_ShouldSucceed_AndMapFields()
        {
            // Arrange
            var dept = new Department { DepartmentId = 1, ClubId = 1, DepartmentName = "Old", Description = "Old Desc" };
            _mockDepartmentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dept);
            _mockDepartmentRepository.Setup(r => r.UpdateAsync(It.IsAny<Department>())).ReturnsAsync(true);
            _mockDepartmentRepository.Setup(r => r.DepartmentNameExistsInClubAsync("New", 1, 1)).ReturnsAsync(false);

            var request = new UpdateDepartmentDto { Name = "New", Description = "New Desc", ManagerRoleId = 500 };

            // Act
            var result = await _departmentService.UpdateDepartmentAsync(1, 1, request);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("New");
            result.Description.Should().Be("New Desc");
            result.ManagerRoleId.Should().Be(500);
            
            dept.DepartmentName.Should().Be("New");
            dept.Description.Should().Be("New Desc");
            dept.ManagerRoleId.Should().Be(500);
        }

        #endregion

        #region GetDepartmentMembersAsync

        [Fact]
        public async Task GetDepartmentMembersAsync_ShouldReturnNull_WhenDeptNotFoundOrWrongClub()
        {
            // Arrange
            _mockDepartmentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Department?)null);

            // Act
            var result1 = await _departmentService.GetDepartmentMembersAsync(1, 1);

            var dept = new Department { DepartmentId = 1, ClubId = 2 };
            _mockDepartmentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dept);
            var result2 = await _departmentService.GetDepartmentMembersAsync(1, 1);

            // Assert
            result1.Should().BeNull();
            result2.Should().BeNull();
        }

        [Fact]
        public async Task GetDepartmentMembersAsync_ShouldMapMembersCorrectly()
        {
            // Arrange
            var dept = new Department { DepartmentId = 10, ClubId = 1 };
            _mockDepartmentRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(dept);

            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();
            var userId3 = Guid.NewGuid();

            var members = new List<UserClubRole>
            {
                // Member 1: Has User, Has Role in current department
                new UserClubRole 
                { 
                    ClubMemberId = 1, UserId = userId1, Status = "Active", JoinDate = DateTime.Now,
                    User = new User { FullName = "U1", Email = "E1", Avatar = "A1", StudentId = "S1" },
                    RoleAssignments = new List<UserClubRoleAssignment> { new UserClubRoleAssignment { ClubRole = new ClubRole { ClubRoleId = 100, RoleName = "R1", Description = "D1", Level = 1, DepartmentId = 10 } } }
                },
                // Member 2: No User (null Check), Role in different department
                new UserClubRole
                {
                    ClubMemberId = 2, UserId = userId2, Status = "Pending", JoinDate = DateTime.Now,
                    User = null,
                    RoleAssignments = new List<UserClubRoleAssignment> { new UserClubRoleAssignment { ClubRole = new ClubRole { ClubRoleId = 101, RoleName = "R2", DepartmentId = 20 } } }
                },
                // Member 3: Has User, Role is null
                new UserClubRole
                {
                    ClubMemberId = 3, UserId = userId3, Status = "Active",
                    User = new User { FullName = "U3", Email = "E3" },
                    RoleAssignments = new List<UserClubRoleAssignment>()
                }
            };

            _mockDepartmentRepository.Setup(r => r.GetMembersWithRolesByDepartmentAsync(1, 10)).ReturnsAsync(members);

            // Act
            var result = await _departmentService.GetDepartmentMembersAsync(1, 10);

            // Assert
            result.Should().NotBeNull();
            var list = result!.ToList();
            list.Should().HaveCount(3);

            // Assert Member 1
            list[0].FullName.Should().Be("U1");
            list[0].DepartmentRoles.Should().NotBeEmpty();
            list[0].DepartmentRoles.First().RoleName.Should().Be("R1");

            // Assert Member 2
            list[1].FullName.Should().Be(string.Empty);
            list[1].Email.Should().Be(string.Empty);
            list[1].DepartmentRoles.Should().BeEmpty();

            // Assert Member 3
            list[2].FullName.Should().Be("U3");
            list[2].DepartmentRoles.Should().BeEmpty();
        }

        #endregion

        #region Add/Remove Member
        [Fact]
        public async Task AddMemberTodepartment_ShouldThrowException_WhenUserNotClubMember()
        {
            var clubId = 1;
            var clubMemberId = 1;
            var departmentId = 10;

            _mockClubMemberRepository
                .Setup(r => r.GetMemberByIdAsync(clubMemberId))
                .ReturnsAsync((UserClubRole?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _departmentService.AddMemberToDepartment(clubId, clubMemberId, departmentId));
        }
        [Fact]
        public async Task AddMemberTodepartment_ShouldSucceed()
        {
            // Arrange
            var clubId = 1;
            var clubMemberId = 50;
            var departmentId = 10;

            var clubMember = new UserClubRole
            {
                ClubMemberId = clubMemberId,
                ClubId = clubId 
            };

            _mockClubMemberRepository
                .Setup(r => r.GetMemberByIdAsync(clubMemberId))
                .ReturnsAsync(clubMember);

            var created = new UserClubRoleDepartment
            {
                ClubMemberId = clubMemberId,
                DepartmentId = departmentId
            };

            _mockDepartmentRepository
                .Setup(r => r.AddMemberTodepartment(It.IsAny<UserClubRoleDepartment>()))
                .ReturnsAsync(created);

            // Act
            var result = await _departmentService.AddMemberToDepartment(clubId, clubMemberId, departmentId);

            // Assert
            result.ClubMemberId.Should().Be(clubMemberId);
            result.DepartmentId.Should().Be(departmentId);

            _mockDepartmentRepository.Verify(r =>
                r.AddMemberTodepartment(It.Is<UserClubRoleDepartment>(m =>
                    m.ClubMemberId == clubMemberId &&
                    m.DepartmentId == departmentId)),
                Times.Once);
        }

        [Fact]
        public async Task RemoveMemberFromDepartment_ShouldThrowException_WhenUserNotClubMember()
        {
            // Arrange
            var clubId = 1;
            var clubMemberId = 1; 
            var departmentId = 10;

            _mockClubMemberRepository
                .Setup(r => r.GetMemberByIdAsync(clubMemberId))
                .ReturnsAsync((UserClubRole?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _departmentService.RemoveMemberFromDepartment(clubId, clubMemberId, departmentId));
        }

        [Fact]
        public async Task RemoveMemberFromDepartment_ShouldSucceed()
        {
            // Arrange
            var clubId = 1;
            var clubMemberId = 50;
            var departmentId = 10;

            var clubMember = new UserClubRole
            {
                ClubMemberId = clubMemberId,
                ClubId = clubId 
            };

            _mockClubMemberRepository
                .Setup(r => r.GetMemberByIdAsync(clubMemberId))
                .ReturnsAsync(clubMember);

            var deleted = new UserClubRoleDepartment
            {
                ClubMemberId = clubMemberId,
                DepartmentId = departmentId
            };

            _mockDepartmentRepository
                .Setup(r => r.RemoveMemberFromDepartment(It.IsAny<UserClubRoleDepartment>()))
                .ReturnsAsync(deleted);

            // Act
            var result = await _departmentService.RemoveMemberFromDepartment(clubId, clubMemberId, departmentId);

            // Assert
            result.ClubMemberId.Should().Be(clubMemberId);
            result.DepartmentId.Should().Be(departmentId);

            _mockDepartmentRepository.Verify(r =>
                r.RemoveMemberFromDepartment(It.Is<UserClubRoleDepartment>(m =>
                    m.ClubMemberId == clubMemberId &&
                    m.DepartmentId == departmentId)),
                Times.Once);
        }

        #endregion
    }
}
