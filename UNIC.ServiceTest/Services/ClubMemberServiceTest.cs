using BusinessLogic.DTOs;
using BusinessLogic.Services.Implementation;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.DataAccess.Repositories.Interface;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class ClubMemberServiceTest
    {
        private readonly Mock<IClubMemberRepository> _mockMemberRepo;
        private readonly Mock<IClubRepository> _mockClubRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IClubRoleRepository> _mockClubRoleRepo;
        private readonly Mock<IDepartmentRepository> _mockDeptRepo;
        private readonly Mock<IPolicyRepository> _mockPolicyRepo;
        private readonly ClubMemberService _service;

        private static readonly Guid _userId = Guid.NewGuid();
        private static readonly Guid _assignedBy = Guid.NewGuid();

        public ClubMemberServiceTest()
        {
            _mockMemberRepo = new Mock<IClubMemberRepository>();
            _mockClubRepo = new Mock<IClubRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockClubRoleRepo = new Mock<IClubRoleRepository>();
            _mockDeptRepo = new Mock<IDepartmentRepository>();
            _mockPolicyRepo = new Mock<IPolicyRepository>();

            _service = new ClubMemberService(
                _mockMemberRepo.Object,
                _mockClubRepo.Object,
                _mockUserRepo.Object,
                _mockClubRoleRepo.Object,
                _mockDeptRepo.Object,
                _mockPolicyRepo.Object);
        }

        private static UserClubRole CreateMember(int id = 1, int clubId = 1) => new()
        {
            ClubMemberId = id,
            UserId = _userId,
            ClubId = clubId,
            JoinDate = DateTime.UtcNow,
            Status = "ACTIVE",
            User = new User { UserId = _userId, FullName = "Test User", Email = "test@example.com" },
            RoleAssignments = new List<UserClubRoleAssignment>
            {
                new UserClubRoleAssignment { ClubRoleId = 10, ClubRole = new ClubRole { ClubRoleId = 10, RoleName = "Member" } }
            }
        };

        #region GetMembersByClubAsync (simple)

        [Fact]
        public async Task GetMembersByClubAsync_ReturnsMembers()
        {
            var members = new List<UserClubRole> { CreateMember() };
            _mockMemberRepo.Setup(r => r.GetMembersByClubIdAsync(1)).ReturnsAsync(members);

            var result = (await _service.GetMembersByClubAsync(1)).ToList();

            Assert.Single(result);
            Assert.Equal("Test User", result[0].FullName);
        }

        [Fact]
        public async Task GetMembersByClubAsync_ReturnsEmpty_WhenNoMembers()
        {
            _mockMemberRepo.Setup(r => r.GetMembersByClubIdAsync(99))
                           .ReturnsAsync(new List<UserClubRole>());

            var result = (await _service.GetMembersByClubAsync(99)).ToList();
            Assert.Empty(result);
        }

        #endregion

        #region GetMembersByClubAsync (paged)

        [Fact]
        public async Task GetMembersByClubAsync_Paged_ReturnsPagedResult()
        {
            var members = new List<UserClubRole> { CreateMember(), CreateMember(2) };
            _mockMemberRepo.Setup(r => r.GetMembersByClubIdAsync(1, 10, 1, null, null, null))
                           .ReturnsAsync((members, 2));

            var result = await _service.GetMembersByClubAsync(1, 10, 1, null, null, null);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(1, result.TotalPages);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task GetMembersByClubAsync_Paged_CalculatesTotalPages()
        {
            var members = new List<UserClubRole> { CreateMember() };
            _mockMemberRepo.Setup(r => r.GetMembersByClubIdAsync(1, 5, 1, null, null, null))
                           .ReturnsAsync((members, 12));

            var result = await _service.GetMembersByClubAsync(1, 5, 1, null, null, null);

            Assert.Equal(3, result.TotalPages);   // ceil(12/5) = 3
            Assert.True(result.HasNextPage);
            Assert.False(result.HasPreviousPage);
        }

        [Fact]
        public async Task GetMembersByClubAsync_Paged_Page2_HasPreviousPage()
        {
            _mockMemberRepo.Setup(r => r.GetMembersByClubIdAsync(1, 5, 2, null, null, null))
                           .ReturnsAsync((new List<UserClubRole>(), 12));

            var result = await _service.GetMembersByClubAsync(1, 5, 2, null, null, null);

            Assert.True(result.HasPreviousPage);
        }

        [Fact]
        public async Task GetMembersByClubAsync_NoPagination_ReturnsAll()
        {
            var members = new List<UserClubRole> { CreateMember() };
            _mockMemberRepo.Setup(r => r.GetMembersByClubIdAsync(1, null, null, null, null, null))
                           .ReturnsAsync((members, 1));

            var result = await _service.GetMembersByClubAsync(1, null, null, null, null, null);

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(0, result.PageSize);
            Assert.Equal(1, result.TotalPages);
        }

        #endregion

        #region GetMemberByIdAsync

        [Fact]
        public async Task GetMemberByIdAsync_ReturnsDto_WhenFound()
        {
            _mockMemberRepo.Setup(r => r.GetMemberByIdAsync(1)).ReturnsAsync(CreateMember());

            var result = await _service.GetMemberByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result!.ClubMemberId);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public async Task GetMemberByIdAsync_ReturnsNull_WhenNotFound()
        {
            _mockMemberRepo.Setup(r => r.GetMemberByIdAsync(99)).ReturnsAsync((UserClubRole?)null);

            var result = await _service.GetMemberByIdAsync(99);
            Assert.Null(result);
        }

        #endregion

        #region AddUserToClubAsync

        [Fact]
        public async Task AddUserToClubAsync_Success()
        {
            var club = new Club { ClubId = 1, ClubName = "Test Club" };
            var user = new User { UserId = _userId, FullName = "Test User", Email = "test@example.com" };
            var created = CreateMember();

            _mockClubRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(club);
            _mockUserRepo.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(user);
            _mockMemberRepo.Setup(r => r.IsMemberAsync(_userId, 1)).ReturnsAsync(false);
            _mockMemberRepo.Setup(r => r.AddMemberAsync(It.IsAny<UserClubRole>())).ReturnsAsync(created);
            _mockMemberRepo.Setup(r => r.GetMemberByIdAsync(1)).ReturnsAsync(created);

            var dto = new AddUserToClubDto { UserId = _userId, ClubRoleIds = new List<int> { 10 } };
            var result = await _service.AddUserToClubAsync(1, dto, _assignedBy);

            Assert.NotNull(result);
            Assert.Equal(_userId, result.UserId);
            _mockMemberRepo.Verify(r => r.AddMemberAsync(It.IsAny<UserClubRole>()), Times.Once);
        }

        [Fact]
        public async Task AddUserToClubAsync_ThrowsKeyNotFound_WhenClubNotExists()
        {
            _mockClubRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Club?)null);

            var dto = new AddUserToClubDto { UserId = _userId };
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.AddUserToClubAsync(99, dto, _assignedBy));

            Assert.Contains("Club", ex.Message);
        }

        [Fact]
        public async Task AddUserToClubAsync_ThrowsKeyNotFound_WhenUserNotExists()
        {
            _mockClubRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Club { ClubId = 1 });
            _mockUserRepo.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync((User?)null);

            var dto = new AddUserToClubDto { UserId = _userId };
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.AddUserToClubAsync(1, dto, _assignedBy));

            Assert.Contains("User", ex.Message);
        }

        [Fact]
        public async Task AddUserToClubAsync_ThrowsInvalidOperation_WhenAlreadyMember()
        {
            _mockClubRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Club { ClubId = 1 });
            _mockUserRepo.Setup(r => r.GetByIdAsync(_userId))
                         .ReturnsAsync(new User { UserId = _userId, FullName = "User", Email = "u@e.com" });
            _mockMemberRepo.Setup(r => r.IsMemberAsync(_userId, 1)).ReturnsAsync(true);

            var dto = new AddUserToClubDto { UserId = _userId };
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.AddUserToClubAsync(1, dto, _assignedBy));

            Assert.Contains("already a member", ex.Message);
        }

        #endregion

        #region UpdateMemberRoleAsync

        [Fact]
        public async Task UpdateMemberRoleAsync_ReturnsUpdated_WhenFound()
        {
            var member = CreateMember();
            _mockMemberRepo.Setup(r => r.GetMemberByIdAsync(1)).ReturnsAsync(member);
            _mockMemberRepo.Setup(r => r.UpdateMemberAsync(member)).ReturnsAsync(true);

            var dto = new UpdateMemberRoleDto { ClubRoleIds = new List<int> { 20 } };
            var result = await _service.UpdateMemberRoleAsync(1, dto);

            Assert.NotNull(result);
            _mockClubRoleRepo.Verify(r => r.SetMemberRolesAsync(1, It.IsAny<HashSet<int>>()), Times.Once);
        }

        [Fact]
        public async Task UpdateMemberRoleAsync_ReturnsNull_WhenNotFound()
        {
            _mockMemberRepo.Setup(r => r.GetMemberByIdAsync(99)).ReturnsAsync((UserClubRole?)null);

            var result = await _service.UpdateMemberRoleAsync(99, new UpdateMemberRoleDto { ClubRoleIds = new List<int> { 5 } });
            Assert.Null(result);
        }

        #endregion

        #region RemoveMemberAsync

        [Fact]
        public async Task RemoveMemberAsync_ReturnsTrue()
        {
            _mockMemberRepo.Setup(r => r.RemoveMemberAsync(1)).ReturnsAsync(true);
            Assert.True(await _service.RemoveMemberAsync(1));
        }

        [Fact]
        public async Task RemoveMemberAsync_ReturnsFalse()
        {
            _mockMemberRepo.Setup(r => r.RemoveMemberAsync(99)).ReturnsAsync(false);
            Assert.False(await _service.RemoveMemberAsync(99));
        }

        #endregion

        #region GetMyClubsAsync

        [Fact]
        public async Task GetMyClubsAsync_ReturnsMemberships()
        {
            var memberships = new List<UserClubRole> { CreateMember(), CreateMember(2, 2) };
            _mockMemberRepo.Setup(r => r.GetClubsByUserIdAsync(_userId)).ReturnsAsync(memberships);

            var result = (await _service.GetMyClubsAsync(_userId)).ToList();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetMyClubsAsync_ReturnsEmpty_WhenNoMemberships()
        {
            _mockMemberRepo.Setup(r => r.GetClubsByUserIdAsync(_userId))
                           .ReturnsAsync(new List<UserClubRole>());

            var result = (await _service.GetMyClubsAsync(_userId)).ToList();
            Assert.Empty(result);
        }

        #endregion

        #region IsMemberAsync

        [Fact]
        public async Task IsMemberAsync_ReturnsTrue_WhenMember()
        {
            _mockMemberRepo.Setup(r => r.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            Assert.True(await _service.IsMemberAsync(_userId, 1));
        }

        [Fact]
        public async Task IsMemberAsync_ReturnsFalse_WhenNotMember()
        {
            _mockMemberRepo.Setup(r => r.IsMemberAsync(_userId, 99)).ReturnsAsync(false);
            Assert.False(await _service.IsMemberAsync(_userId, 99));
        }

        #endregion
    }
}
