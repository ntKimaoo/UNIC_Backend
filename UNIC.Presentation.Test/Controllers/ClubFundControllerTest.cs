using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Moq;
using System;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using Presentation.Controllers;
using Xunit;

namespace UNIC.Presentation.Test.Controllers
{
    public class ClubFundControllerTest
    {
        private readonly Mock<IClubFundService> _mockService;
        private readonly Mock<IClubMemberService> _mockClubMemberService;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly ClubFundController _controller;

        public ClubFundControllerTest()
        {
            _mockService = new Mock<IClubFundService>();
            _mockClubMemberService = new Mock<IClubMemberService>();
            var mockPayOSService = new Mock<IPayOSService>();
            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            _controller = new ClubFundController(_mockService.Object, _mockClubMemberService.Object, mockPayOSService.Object, _mockEnv.Object);
        }

        private void SetupUser(Guid userId)
        {
            var claims = new[] { new Claim("UserId", userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        #region GetHistory

        [Fact]
        public async Task GetHistory_ReturnsOk_WhenSuccess()
        {
            SetupUser(Guid.NewGuid());
            _mockService.Setup(s => s.GetFundHistoryPagedAsync(1, null, null, It.IsAny<Guid>(), 1, 10))
                .ReturnsAsync(new PagedResultDto<FundTransactionResponseDto>
                {
                    Items = new List<FundTransactionResponseDto> { new() },
                    PageNumber = 1,
                    PageSize = 10,
                    TotalCount = 1,
                    TotalPages = 1,
                    HasPreviousPage = false,
                    HasNextPage = false
                });
            _mockService.Setup(s => s.GetFundByIdAsync(1))
                .ReturnsAsync(new FundResponseDto { FundId = 1, ClubId = 1 });

            var result = await _controller.GetHistory(1, null, null, 1, 10);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetHistory_ReturnsBadRequest_WhenServiceThrows()
        {
            SetupUser(Guid.NewGuid());
            _mockService.Setup(s => s.GetFundByIdAsync(99))
                .ReturnsAsync(new FundResponseDto { FundId = 99, ClubId = 1 });
            _mockService.Setup(s => s.GetFundHistoryPagedAsync(99, null, null, It.IsAny<Guid>(), 1, 10))
                .ThrowsAsync(new Exception("Fund not found"));

            var result = await _controller.GetHistory(99, null, null, 1, 10);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region GetPayOsContributionReturn

        [Fact]
        public async Task GetPayOsContributionReturn_ReturnsOk_WhenFound()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId);
            _mockService.Setup(s => s.GetContributionPaymentStatusByOrderCodeAsync(userId, 18))
                .ReturnsAsync(new ContributionPaymentStatusDto
                {
                    ClubId = 5,
                    TransactionId = 18,
                    FundId = 2,
                    Status = "APPROVED",
                    IsPaid = true,
                    Message = "OK"
                });
            _mockClubMemberService.Setup(s => s.IsMemberAsync(userId, 5)).ReturnsAsync(true);

            var result = await _controller.GetPayOsContributionReturn(18);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetPayOsContributionReturn_ReturnsNotFound_WhenServiceReturnsNull()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId);
            _mockService.Setup(s => s.GetContributionPaymentStatusByOrderCodeAsync(userId, 99))
                .ReturnsAsync((ContributionPaymentStatusDto?)null);

            var result = await _controller.GetPayOsContributionReturn(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetContributionPaymentStatus

        [Fact]
        public async Task GetContributionPaymentStatus_ReturnsOk_WhenFound()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId);
            const int clubId = 3;
            _mockClubMemberService.Setup(s => s.IsMemberAsync(userId, clubId)).ReturnsAsync(true);
            _mockService.Setup(s => s.GetContributionPaymentStatusAsync(userId, clubId, 42))
                .ReturnsAsync(new ContributionPaymentStatusDto
                {
                    TransactionId = 42,
                    FundId = 1,
                    Status = "PENDING",
                    IsPaid = false,
                    Message = "Đang chờ"
                });

            var result = await _controller.GetContributionPaymentStatus(clubId, 42);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetContributionPaymentStatus_ReturnsNotFound_WhenServiceReturnsNull()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId);
            const int clubId = 3;
            _mockClubMemberService.Setup(s => s.IsMemberAsync(userId, clubId)).ReturnsAsync(true);
            _mockService.Setup(s => s.GetContributionPaymentStatusAsync(userId, clubId, 99))
                .ReturnsAsync((ContributionPaymentStatusDto?)null);

            var result = await _controller.GetContributionPaymentStatus(clubId, 99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetContributionPaymentStatus_Returns403_WhenNotClubMember()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId);
            const int clubId = 3;
            _mockClubMemberService.Setup(s => s.IsMemberAsync(userId, clubId)).ReturnsAsync(false);

            var result = await _controller.GetContributionPaymentStatus(clubId, 42);

            Assert.IsType<ObjectResult>(result);
            var obj = (ObjectResult)result;
            Assert.Equal(403, obj.StatusCode);
        }

        #endregion

        #region SimulatePayOsPaidForDevelopment

        [Fact]
        public async Task SimulatePayOsPaid_ReturnsNotFound_WhenNotDevelopment()
        {
            _mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);
            var userId = Guid.NewGuid();
            SetupUser(userId);
            var result = await _controller.SimulatePayOsPaidForDevelopment(3, 1);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SimulatePayOsPaid_ReturnsOk_WhenDevelopmentAndSuccess()
        {
            _mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            var userId = Guid.NewGuid();
            SetupUser(userId);
            const int clubId = 3;
            _mockClubMemberService.Setup(s => s.IsMemberAsync(userId, clubId)).ReturnsAsync(true);
            _mockService.Setup(s => s.TryCompleteOwnPendingContributionForDevelopmentAsync(userId, clubId, 5))
                .ReturnsAsync(true);

            var result = await _controller.SimulatePayOsPaidForDevelopment(clubId, 5);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion
    }
}
