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
using Xunit;

namespace UNIC.BusinessLogic.Test.Services
{
    public class ClubRoleServiceTest
    {
        private readonly Mock<IClubRoleRepository> _mockRoleRepo;
        private readonly ClubRoleService _clubRoleService;

        public ClubRoleServiceTest()
        {
            _mockRoleRepo = new Mock<IClubRoleRepository>();
            _clubRoleService = new ClubRoleService(_mockRoleRepo.Object);
        }

        #region GetMethods

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenRoleNotFound()
        {
            _mockRoleRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ClubRole?)null);
            var result = await _clubRoleService.GetByIdAsync(1);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedDto_WhenFound()
        {
            var role = new ClubRole 
            { 
                ClubRoleId = 1, RoleName = "Admin", 
                ClubMembers = new List<UserClubRole> { new UserClubRole() } 
            };
            _mockRoleRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(role);
            
            var result = await _clubRoleService.GetByIdAsync(1);
            
            Assert.NotNull(result);
            Assert.Equal("Admin", result.RoleName);
            Assert.Equal(1, result.MemberCount);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedDtos()
        {
            var roles = new List<ClubRole> { new ClubRole { ClubRoleId = 1, RoleName = "Member" } };
            _mockRoleRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(roles);
            
            var result = await _clubRoleService.GetAllAsync();
            
            Assert.Single(result);
            Assert.Equal("Member", result.First().RoleName);
        }

        [Fact]
        public async Task GetPoliciesByRoleAsync_ShouldReturnPolicies()
        {
            var policies = new List<Policy> { new Policy { Id = 1 } };
            _mockRoleRepo.Setup(r => r.GetPoliciesByRoleAsync(1)).ReturnsAsync(policies);
            
            var result = await _clubRoleService.GetPoliciesByRoleAsync(1);
            
            Assert.Single(result);
        }

        #endregion

        #region CreateAsync

        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenRoleNameExists()
        {
            var dto = new CreateClubRoleDto { RoleName = "Admin" };
            _mockRoleRepo.Setup(r => r.RoleNameExistsAsync("Admin")).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _clubRoleService.CreateAsync(dto));
            Assert.Contains("already exists", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateRoleAndPolicies_WhenValid()
        {
            var dto = new CreateClubRoleDto { RoleName = "NewRole", policies = new List<int> { 1, 2 } };
            var createdRole = new ClubRole { ClubRoleId = 10, RoleName = "NewRole" };
            
            _mockRoleRepo.Setup(r => r.RoleNameExistsAsync("NewRole")).ReturnsAsync(false);
            _mockRoleRepo.Setup(r => r.CreateAsync(It.IsAny<ClubRole>())).ReturnsAsync(createdRole);
            _mockRoleRepo.Setup(r => r.SetPoliciesAsync(10, dto.policies)).Returns(Task.CompletedTask);
            _mockRoleRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(createdRole);

            var result = await _clubRoleService.CreateAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("NewRole", result.RoleName);
            _mockRoleRepo.Verify(r => r.CreateAsync(It.IsAny<ClubRole>()), Times.Once);
            _mockRoleRepo.Verify(r => r.SetPoliciesAsync(10, dto.policies), Times.Once);
        }

        #endregion

        #region UpdateAsync

        [Fact]
        public async Task UpdateAsync_ShouldReturnNull_WhenRoleNotFound()
        {
            _mockRoleRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ClubRole?)null);
            var result = await _clubRoleService.UpdateAsync(1, new UpdateClubRoleDto());
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenNewRoleNameExists()
        {
            var role = new ClubRole { ClubRoleId = 1, RoleName = "OldRole" };
            var dto = new UpdateClubRoleDto { RoleName = "TakenRole" };
            
            _mockRoleRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(role);
            _mockRoleRepo.Setup(r => r.RoleNameExistsAsync("TakenRole")).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _clubRoleService.UpdateAsync(1, dto));
            Assert.Contains("already exists", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateRoleAndPolicies_WhenValid()
        {
            var role = new ClubRole { ClubRoleId = 1, RoleName = "OldRole" };
            var dto = new UpdateClubRoleDto { RoleName = "NewRole", Description = "Desc", Level = 2, PolicyIds = new List<int> { 3 } };
            
            _mockRoleRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(role);
            _mockRoleRepo.Setup(r => r.RoleNameExistsAsync("NewRole")).ReturnsAsync(false);
            _mockRoleRepo.Setup(r => r.UpdateAsync(role)).ReturnsAsync(true);
            _mockRoleRepo.Setup(r => r.SetPoliciesAsync(1, dto.PolicyIds)).Returns(Task.CompletedTask);

            var result = await _clubRoleService.UpdateAsync(1, dto);

            Assert.NotNull(result);
            Assert.Equal("NewRole", role.RoleName);
            Assert.Equal("Desc", role.Description);
            Assert.Equal(2, role.Level);
            _mockRoleRepo.Verify(r => r.UpdateAsync(role), Times.Once);
            _mockRoleRepo.Verify(r => r.SetPoliciesAsync(1, dto.PolicyIds), Times.Once);
        }

        [Fact]
        public async Task UpdatePoliciesAsync_ShouldCallRepository()
        {
            var policyIds = new List<int> { 1, 2 };
            _mockRoleRepo.Setup(r => r.SetPoliciesAsync(1, policyIds)).Returns(Task.CompletedTask);
            
            await _clubRoleService.UpdatePoliciesAsync(1, policyIds);
            
            _mockRoleRepo.Verify(r => r.SetPoliciesAsync(1, policyIds), Times.Once);
        }

        #endregion

        #region DeleteAsync

        [Fact]
        public async Task DeleteAsync_ShouldReturnRepoResult()
        {
            _mockRoleRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);
            var result = await _clubRoleService.DeleteAsync(1);
            Assert.True(result);
        }

        #endregion
    }
}
