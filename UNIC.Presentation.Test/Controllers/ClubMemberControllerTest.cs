using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Presentation.Controllers;
using Xunit;

namespace UNIC.Presentation.Test.Controllers
{
    public class ClubMemberControllerTest
    {
        private readonly Mock<IClubMemberService> _mockService;
        private readonly ClubMemberController _controller;

        public ClubMemberControllerTest()
        {
            _mockService = new Mock<IClubMemberService>();
            //_controller = new ClubMemberController(_mockService.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        private void SetupUser(Guid userId)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
        }

        #region GetMembers

        [Fact]
        public async Task GetMembers_ReturnsOk()
        {
            _mockService.Setup(s => s.GetMembersByClubAsync(1))
                .ReturnsAsync(new List<ClubMemberResponseDto> { new() });

            var result = await _controller.GetMembers(1);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region GetMember

        [Fact]
        public async Task GetMember_ReturnsOk_WhenFoundAndMatchingClub()
        {
            _mockService.Setup(s => s.GetMemberByIdAsync(1))
                .ReturnsAsync(new ClubMemberResponseDto { ClubMemberId = 1, ClubId = 10 });

            var result = await _controller.GetMember(10, 1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetMember_ReturnsNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetMemberByIdAsync(99))
                .ReturnsAsync((ClubMemberResponseDto?)null);

            var result = await _controller.GetMember(10, 99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetMember_ReturnsNotFound_WhenClubMismatch()
        {
            _mockService.Setup(s => s.GetMemberByIdAsync(1))
                .ReturnsAsync(new ClubMemberResponseDto { ClubMemberId = 1, ClubId = 5 });

            var result = await _controller.GetMember(10, 1);  // ClubId 10 != 5

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region AddMember

        [Fact]
        public async Task AddMember_ReturnsCreated_WhenSuccess()
        {
            SetupUser(Guid.NewGuid());
            var dto = new AddUserToClubDto { UserId = Guid.NewGuid() };
            _mockService.Setup(s => s.AddUserToClubAsync(1, dto, It.IsAny<Guid?>()))
                .ReturnsAsync(new ClubMemberResponseDto { ClubMemberId = 1 });

            var result = await _controller.AddMember(1, dto);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task AddMember_ReturnsNotFound_WhenKeyNotFound()
        {
            SetupUser(Guid.NewGuid());
            var dto = new AddUserToClubDto { UserId = Guid.NewGuid() };
            _mockService.Setup(s => s.AddUserToClubAsync(1, dto, It.IsAny<Guid?>()))
                .ThrowsAsync(new KeyNotFoundException("User not found"));

            var result = await _controller.AddMember(1, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task AddMember_ReturnsConflict_WhenAlreadyMember()
        {
            SetupUser(Guid.NewGuid());
            var dto = new AddUserToClubDto { UserId = Guid.NewGuid() };
            _mockService.Setup(s => s.AddUserToClubAsync(1, dto, It.IsAny<Guid?>()))
                .ThrowsAsync(new InvalidOperationException("Already a member"));

            var result = await _controller.AddMember(1, dto);

            Assert.IsType<ConflictObjectResult>(result);
        }

        #endregion

        #region UpdateMemberRole

        [Fact]
        public async Task UpdateMemberRole_ReturnsOk_WhenSuccess()
        {
            var dto = new UpdateMemberRoleDto { ClubRoleId = 2 };
            _mockService.Setup(s => s.UpdateMemberRoleAsync(1, dto))
                .ReturnsAsync(new ClubMemberResponseDto { ClubMemberId = 1, ClubId = 10 });

            var result = await _controller.UpdateMemberRole(10, 1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateMemberRole_ReturnsNotFound_WhenNull()
        {
            var dto = new UpdateMemberRoleDto();
            _mockService.Setup(s => s.UpdateMemberRoleAsync(99, dto))
                .ReturnsAsync((ClubMemberResponseDto?)null);

            var result = await _controller.UpdateMemberRole(10, 99, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region RemoveMember

        [Fact]
        public async Task RemoveMember_ReturnsOk_WhenSuccess()
        {
            _mockService.Setup(s => s.GetMemberByIdAsync(1))
                .ReturnsAsync(new ClubMemberResponseDto { ClubMemberId = 1, ClubId = 10 });
            _mockService.Setup(s => s.RemoveMemberAsync(1)).ReturnsAsync(true);

            var result = await _controller.RemoveMember(10, 1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task RemoveMember_ReturnsNotFound_WhenMemberNotFound()
        {
            _mockService.Setup(s => s.GetMemberByIdAsync(99))
                .ReturnsAsync((ClubMemberResponseDto?)null);

            var result = await _controller.RemoveMember(10, 99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task RemoveMember_Returns500_WhenRemoveFails()
        {
            _mockService.Setup(s => s.GetMemberByIdAsync(1))
                .ReturnsAsync(new ClubMemberResponseDto { ClubMemberId = 1, ClubId = 10 });
            _mockService.Setup(s => s.RemoveMemberAsync(1)).ReturnsAsync(false);

            var result = await _controller.RemoveMember(10, 1);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        #endregion

        #region GetClubsByUser

        [Fact]
        public async Task GetClubsByUser_ReturnsOk_WhenValidUserId()
        {
            var userId = Guid.NewGuid();
            _mockService.Setup(s => s.GetMyClubsAsync(userId))
                .ReturnsAsync(new List<ClubMemberResponseDto> { new() });

            var result = await _controller.GetClubsByUser(userId);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetClubsByUser_ReturnsBadRequest_WhenEmptyGuid()
        {
            var result = await _controller.GetClubsByUser(Guid.Empty);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion
    }
}
