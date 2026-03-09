using BusinessLogic.DTOs;
using BusinessLogic.Services.Implementation;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.BusinessLogic.Test.Services
{
    public class ClubMemberServiceTest
    {
        private readonly Mock<IClubMemberRepository> _mockMemberRepo;
        private readonly Mock<IClubRepository> _mockClubRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly ClubMemberService _clubMemberService;

        public ClubMemberServiceTest()
        {
            _mockMemberRepo = new Mock<IClubMemberRepository>();
            _mockClubRepo = new Mock<IClubRepository>();
            _mockUserRepo = new Mock<IUserRepository>();

            _clubMemberService = new ClubMemberService(_mockMemberRepo.Object, _mockClubRepo.Object, _mockUserRepo.Object);
        }

        #region GetMethods

        [Fact]
        public async Task GetMembersByClubAsync_ShouldReturnMappedDtos()
        {
            // Arrange
            var members = new List<UserClubRole>
            {
                new UserClubRole { ClubMemberId = 1, ClubId = 1, UserId = Guid.NewGuid() }
            };
            _mockMemberRepo.Setup(r => r.GetMembersByClubIdAsync(1)).ReturnsAsync(members);

            // Act
            var result = await _clubMemberService.GetMembersByClubAsync(1);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result.First().ClubId);
        }

        [Fact]
        public async Task GetMemberByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            _mockMemberRepo.Setup(r => r.GetMemberByIdAsync(1)).ReturnsAsync((UserClubRole?)null);
            var result = await _clubMemberService.GetMemberByIdAsync(1);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetMemberByIdAsync_ShouldReturnMappedDto_WhenFound()
        {
            var userId = Guid.NewGuid();
            var member = new UserClubRole 
            { 
                ClubMemberId = 1, ClubId = 1, UserId = userId, 
                User = new User { FullName = "Test" } 
            };
            _mockMemberRepo.Setup(r => r.GetMemberByIdAsync(1)).ReturnsAsync(member);
            
            var result = await _clubMemberService.GetMemberByIdAsync(1);
            
            Assert.NotNull(result);
            Assert.Equal("Test", result.FullName);
        }

        [Fact]
        public async Task GetMyClubsAsync_ShouldReturnMappedDtos()
        {
            var userId = Guid.NewGuid();
            var memberships = new List<UserClubRole>
            {
                new UserClubRole { ClubMemberId = 1, ClubId = 1, UserId = userId }
            };
            _mockMemberRepo.Setup(r => r.GetClubsByUserIdAsync(userId)).ReturnsAsync(memberships);

            var result = await _clubMemberService.GetMyClubsAsync(userId);

            Assert.Single(result);
            Assert.Equal(userId, result.First().UserId);
        }

        #endregion

        #region AddUserToClubAsync

        [Fact]
        public async Task AddUserToClubAsync_ShouldThrowException_WhenClubNotFound()
        {
            var dto = new AddUserToClubDto { UserId = Guid.NewGuid() };
            _mockClubRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Club?)null);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _clubMemberService.AddUserToClubAsync(1, dto, null));
            Assert.Contains("not found", ex.Message);
        }

        [Fact]
        public async Task AddUserToClubAsync_ShouldThrowException_WhenUserNotFound()
        {
            var dto = new AddUserToClubDto { UserId = Guid.NewGuid() };
            _mockClubRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Club());
            _mockUserRepo.Setup(r => r.GetByIdAsync(dto.UserId)).ReturnsAsync((User?)null);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _clubMemberService.AddUserToClubAsync(1, dto, null));
            Assert.Contains("not found", ex.Message);
        }

        [Fact]
        public async Task AddUserToClubAsync_ShouldThrowException_WhenAlreadyMember()
        {
            var dto = new AddUserToClubDto { UserId = Guid.NewGuid() };
            _mockClubRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Club());
            _mockUserRepo.Setup(r => r.GetByIdAsync(dto.UserId)).ReturnsAsync(new User());
            _mockMemberRepo.Setup(r => r.IsMemberAsync(dto.UserId, 1)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _clubMemberService.AddUserToClubAsync(1, dto, null));
            Assert.Contains("already a member", ex.Message);
        }

        [Fact]
        public async Task AddUserToClubAsync_ShouldReturnDto_WhenSuccessful()
        {
            var dto = new AddUserToClubDto { UserId = Guid.NewGuid(), ClubRoleId = 2 };
            var assignedBy = Guid.NewGuid();
            var createdMember = new UserClubRole { ClubMemberId = 10, UserId = dto.UserId, ClubId = 1, ClubRoleId = 2 };

            _mockClubRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Club());
            _mockUserRepo.Setup(r => r.GetByIdAsync(dto.UserId)).ReturnsAsync(new User());
            _mockMemberRepo.Setup(r => r.IsMemberAsync(dto.UserId, 1)).ReturnsAsync(false);
            _mockMemberRepo.Setup(r => r.AddMemberAsync(It.IsAny<UserClubRole>())).ReturnsAsync(createdMember);
            _mockMemberRepo.Setup(r => r.GetMemberByIdAsync(10)).ReturnsAsync(createdMember);

            var result = await _clubMemberService.AddUserToClubAsync(1, dto, assignedBy);

            Assert.NotNull(result);
            Assert.Equal(10, result.ClubMemberId);
            Assert.Equal(2, result.ClubRoleId);
            _mockMemberRepo.Verify(r => r.AddMemberAsync(It.IsAny<UserClubRole>()), Times.Once);
        }

        #endregion

        #region Update&Remove

        [Fact]
        public async Task UpdateMemberRoleAsync_ShouldReturnNull_WhenMemberNotFound()
        {
            _mockMemberRepo.Setup(r => r.GetMemberByIdAsync(1)).ReturnsAsync((UserClubRole?)null);
            var result = await _clubMemberService.UpdateMemberRoleAsync(1, new UpdateMemberRoleDto());
            Assert.Null(result);
        }

        //[Fact]
        //public async Task UpdateMemberRoleAsync_ShouldReturnDto_WhenSuccessful()
        //{
        //    var member = new UserClubRole { ClubMemberId = 1, ClubRoleId = 2 };
        //    var dto = new UpdateMemberRoleDto { ClubRoleId = 3 };

        //    _mockMemberRepo.Setup(r => r.GetMemberByIdAsync(1)).ReturnsAsync(member);
        //    _mockMemberRepo.Setup(r => r.UpdateMemberAsync(member)).Returns(Task.CompletedTask);

        //    var result = await _clubMemberService.UpdateMemberRoleAsync(1, dto);

        //    Assert.NotNull(result);
        //    Assert.Equal(3, result.ClubRoleId); // Updated
        //    _mockMemberRepo.Verify(r => r.UpdateMemberAsync(member), Times.Once);
        //}

        [Fact]
        public async Task RemoveMemberAsync_ShouldReturnRepoResult()
        {
            _mockMemberRepo.Setup(r => r.RemoveMemberAsync(1)).ReturnsAsync(true);
            var result = await _clubMemberService.RemoveMemberAsync(1);
            Assert.True(result);
        }

        #endregion
    }
}
