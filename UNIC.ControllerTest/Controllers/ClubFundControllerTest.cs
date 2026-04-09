using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Moq;
using Presentation.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ControllerTest.Controllers
{
    public class ClubFundControllerTest
    {
        private readonly Mock<IClubFundService> _fundService;
        private readonly Mock<IClubMemberService> _memberService;
        private readonly Mock<IPayOSService> _payOSService;
        private readonly Mock<IWebHostEnvironment> _environment;
        private readonly ClubFundController _controller;
        private readonly Guid _userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        public ClubFundControllerTest()
        {
            _fundService = new Mock<IClubFundService>();
            _memberService = new Mock<IClubMemberService>();
            _payOSService = new Mock<IPayOSService>();
            _environment = new Mock<IWebHostEnvironment>();

            _controller = new ClubFundController(
                _fundService.Object,
                _memberService.Object,
                _payOSService.Object,
                _environment.Object);

            var http = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = http };
            SetAuthenticatedUser(_userId);
        }

        private void SetAuthenticatedUser(Guid userId, bool isAdmin = false)
        {
            var claims = new List<Claim> { new Claim("UserId", userId.ToString()) };
            if (isAdmin)
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            _controller.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }

        #region CreateFund

        [Fact]
        public async Task CreateFund_ReturnsOk_WhenSuccess()
        {
            var dto = new CreateFundDto { ClubId = 1, FundName = "Q1", InitialAmount = 0 };
            var response = new FundResponseDto { FundId = 1, FundName = "Q1", ClubId = 1 };

            _fundService.Setup(s => s.CreateFundAsync(_userId, It.IsAny<CreateFundDto>()))
                .ReturnsAsync(response);

            var result = await _controller.CreateFund(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task CreateFund_Returns403_WhenUnauthorizedAccess()
        {
            _fundService.Setup(s => s.CreateFundAsync(_userId, It.IsAny<CreateFundDto>()))
                .ThrowsAsync(new UnauthorizedAccessException("no"));

            var result = await _controller.CreateFund(1, new CreateFundDto { FundName = "x" });

            Assert.IsType<ObjectResult>(result);
            var obj = (ObjectResult)result;
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task CreateFund_ReturnsBadRequest_WhenArgumentException()
        {
            _fundService.Setup(s => s.CreateFundAsync(_userId, It.IsAny<CreateFundDto>()))
                .ThrowsAsync(new ArgumentException("bad"));

            var result = await _controller.CreateFund(1, new CreateFundDto { FundName = "x" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region GetMyClubs

        [Fact]
        public async Task GetMyClubs_ReturnsOk()
        {
            _memberService.Setup(m => m.GetMyClubsAsync(_userId))
                .ReturnsAsync(Array.Empty<ClubMemberResponseDto>());

            var result = await _controller.GetMyClubs();

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region GetFundCapabilities

        [Fact]
        public async Task GetFundCapabilities_Returns403_WhenCannotAccessClub()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 5)).ReturnsAsync(false);

            var result = await _controller.GetFundCapabilities(5);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task GetFundCapabilities_ReturnsOk_WhenMember()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 5)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundCapabilitiesAsync(_userId, 5))
                .ReturnsAsync(new FundCapabilitiesDto { ClubId = 5, CanViewFunds = true });

            var result = await _controller.GetFundCapabilities(5);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetFundCapabilities_ReturnsUnauthorized_WhenServiceThrows()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 5)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundCapabilitiesAsync(_userId, 5))
                .ThrowsAsync(new UnauthorizedAccessException());

            var result = await _controller.GetFundCapabilities(5);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        #endregion

        #region GetFund / GetFundsByClub

        [Fact]
        public async Task GetFund_ReturnsNotFound_WhenMissing()
        {
            _fundService.Setup(s => s.GetFundByIdAsync(9)).ReturnsAsync((FundResponseDto?)null);

            var result = await _controller.GetFund(9);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetFund_Returns403_WhenCannotAccessClub()
        {
            _fundService.Setup(s => s.GetFundByIdAsync(1)).ReturnsAsync(new FundResponseDto
            {
                FundId = 1,
                ClubId = 99
            });
            _memberService.Setup(m => m.IsMemberAsync(_userId, 99)).ReturnsAsync(false);

            var result = await _controller.GetFund(1);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task GetFund_ReturnsOk_WhenAdminWithoutMembership()
        {
            SetAuthenticatedUser(_userId, isAdmin: true);
            _fundService.Setup(s => s.GetFundByIdAsync(1)).ReturnsAsync(new FundResponseDto { FundId = 1, ClubId = 99 });

            var result = await _controller.GetFund(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetFundsByClub_ReturnsBadRequest_WhenPageInvalid()
        {
            var result = await _controller.GetFundsByClub(1, 0, 10);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetFundsByClub_ReturnsBadRequest_WhenPageSizeInvalid()
        {
            var result = await _controller.GetFundsByClub(1, 1, 200);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetFundsByClub_Returns403_WhenNotMember()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 2)).ReturnsAsync(false);

            var result = await _controller.GetFundsByClub(2, 1, 10);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task GetFundsByClub_ReturnsOk_WhenAllowed()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 2)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundsByClubIdPagedAsync(2, 1, 10))
                .ReturnsAsync(new PagedResultDto<FundResponseDto>
                {
                    Items = Array.Empty<FundResponseDto>(),
                    PageNumber = 1,
                    PageSize = 10,
                    TotalCount = 0
                });

            var result = await _controller.GetFundsByClub(2, 1, 10);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region Contribute / status / dev simulate

        [Fact]
        public async Task Contribute_Returns403_WhenNotMember()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(false);

            var result = await _controller.Contribute(1, new ContributeRequestDto { FundId = 1, Amount = 5000 }, CancellationToken.None);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task Contribute_ReturnsNotFound_WhenFundMissing()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundByIdAsync(2)).ReturnsAsync((FundResponseDto?)null);

            var result = await _controller.Contribute(1, new ContributeRequestDto { FundId = 2, Amount = 5000 }, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Contribute_Returns403_WhenFundWrongClub()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundByIdAsync(2)).ReturnsAsync(new FundResponseDto
            {
                FundId = 2,
                ClubId = 99
            });

            var result = await _controller.Contribute(1, new ContributeRequestDto { FundId = 2, Amount = 5000 }, CancellationToken.None);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task Contribute_ReturnsOk_WhenSuccess()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundByIdAsync(2)).ReturnsAsync(new FundResponseDto { FundId = 2, ClubId = 1 });
            _fundService.Setup(s => s.CreateContributionAsync(_userId, It.IsAny<ContributeRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ContributeResponseDto { TransactionId = 1, CheckoutUrl = "u" });

            var result = await _controller.Contribute(1, new ContributeRequestDto { FundId = 2, Amount = 5000 }, CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetContributionPaymentStatus_ReturnsNotFound_WhenNull()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetContributionPaymentStatusAsync(_userId, 1, 9))
                .ReturnsAsync((ContributionPaymentStatusDto?)null);

            var result = await _controller.GetContributionPaymentStatus(1, 9);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetPayOsContributionReturn_ReturnsNotFound_WhenNull()
        {
            _fundService.Setup(s => s.GetContributionPaymentStatusByOrderCodeAsync(_userId, 100))
                .ReturnsAsync((ContributionPaymentStatusDto?)null);

            var result = await _controller.GetPayOsContributionReturn(100);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetPayOsContributionReturn_Returns403_WhenCannotAccessClub()
        {
            _fundService.Setup(s => s.GetContributionPaymentStatusByOrderCodeAsync(_userId, 100))
                .ReturnsAsync(new ContributionPaymentStatusDto { ClubId = 7 });
            _memberService.Setup(m => m.IsMemberAsync(_userId, 7)).ReturnsAsync(false);

            var result = await _controller.GetPayOsContributionReturn(100);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task SimulatePayOsPaidForDevelopment_ReturnsNotFound_WhenNotDevelopment()
        {
            _environment.Setup(e => e.EnvironmentName).Returns(Environments.Production);

            var result = await _controller.SimulatePayOsPaidForDevelopment(1, 1);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SimulatePayOsPaidForDevelopment_ReturnsOk_WhenDev()
        {
            _environment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.TryCompleteOwnPendingContributionForDevelopmentAsync(_userId, 1, 2))
                .ReturnsAsync(true);

            var result = await _controller.SimulatePayOsPaidForDevelopment(1, 2);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region ApproveFund

        [Fact]
        public async Task ApproveFund_ReturnsOk_WhenSuccess()
        {
            _fundService.Setup(s => s.ApproveFundAsync(_userId, It.IsAny<ApproveFundDto>()))
                .ReturnsAsync(true);

            var result = await _controller.ApproveFund(new ApproveFundDto { FundId = 1, Action = "APPROVE" });

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ApproveFund_Returns403_WhenUnauthorized()
        {
            _fundService.Setup(s => s.ApproveFundAsync(_userId, It.IsAny<ApproveFundDto>()))
                .ThrowsAsync(new UnauthorizedAccessException());

            var result = await _controller.ApproveFund(new ApproveFundDto { FundId = 1, Action = "APPROVE" });

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        #endregion

        #region PayOSWebhook

        [Fact]
        public async Task PayOSWebhook_ReturnsBadRequest_WhenBodyEmpty()
        {
            _controller.HttpContext!.Request.Body = new MemoryStream();

            var result = await _controller.PayOSWebhookClubScoped(CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PayOSWebhook_ReturnsOk_WhenNotSuccessCode()
        {
            SetJsonBody("{\"code\":\"01\",\"success\":true}");

            var result = await _controller.PayOSWebhookClubScoped(CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            _fundService.Verify(s => s.ProcessPayOSPaymentSuccessAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task PayOSWebhook_ReturnsBadRequest_WhenMissingSignature()
        {
            SetJsonBody("{\"code\":\"00\",\"success\":true,\"data\":{}}");

            var result = await _controller.PayOSWebhookClubScoped(CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PayOSWebhook_ReturnsBadRequest_WhenSignatureInvalid()
        {
            SetJsonBody("{\"code\":\"00\",\"success\":true,\"data\":{\"orderCode\":5},\"signature\":\"sig\"}");
            _payOSService.Setup(p => p.VerifyWebhookSignature("sig", It.IsAny<System.Text.Json.JsonElement>())).Returns(false);

            var result = await _controller.PayOSWebhookClubScoped(CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PayOSWebhook_ReturnsOk_WhenProcessed()
        {
            SetJsonBody("{\"code\":\"00\",\"success\":true,\"data\":{\"orderCode\":5},\"signature\":\"good\"}");
            _payOSService.Setup(p => p.VerifyWebhookSignature("good", It.IsAny<System.Text.Json.JsonElement>())).Returns(true);
            _fundService.Setup(s => s.ProcessPayOSPaymentSuccessAsync(5)).ReturnsAsync(true);

            var result = await _controller.PayOSWebhookClubScoped(CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            _fundService.Verify(s => s.ProcessPayOSPaymentSuccessAsync(5), Times.Once);
        }

        private void SetJsonBody(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            _controller.HttpContext!.Request.Body = new MemoryStream(bytes);
        }

        #endregion

        #region GetHistory / GetFundLocation

        [Fact]
        public async Task GetHistory_ReturnsBadRequest_WhenPageInvalid()
        {
            var result = await _controller.GetHistory(1, null, null, 0, 10);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetHistory_ReturnsNotFound_WhenFundMissing()
        {
            _fundService.Setup(s => s.GetFundByIdAsync(3)).ReturnsAsync((FundResponseDto?)null);

            var result = await _controller.GetHistory(3, null, null, 1, 10);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetHistory_Returns403_WhenCannotAccessClub()
        {
            _fundService.Setup(s => s.GetFundByIdAsync(3)).ReturnsAsync(new FundResponseDto { FundId = 3, ClubId = 8 });
            _memberService.Setup(m => m.IsMemberAsync(_userId, 8)).ReturnsAsync(false);

            var result = await _controller.GetHistory(3, null, null, 1, 10);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task GetHistory_ReturnsOk_WhenSuccess()
        {
            _fundService.Setup(s => s.GetFundByIdAsync(3)).ReturnsAsync(new FundResponseDto { FundId = 3, ClubId = 8 });
            _memberService.Setup(m => m.IsMemberAsync(_userId, 8)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundHistoryPagedAsync(3, null, null, _userId, 1, 10))
                .ReturnsAsync(new PagedResultDto<FundTransactionResponseDto>
                {
                    Items = Array.Empty<FundTransactionResponseDto>(),
                    PageNumber = 1,
                    PageSize = 10,
                    TotalCount = 0
                });

            var result = await _controller.GetHistory(3, null, null, 1, 10);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetFundLocation_ReturnsNotFound_WhenFundMissing()
        {
            _fundService.Setup(s => s.GetFundByIdAsync(4)).ReturnsAsync((FundResponseDto?)null);

            var result = await _controller.GetFundLocation(4);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetFundLocation_ReturnsOk_WhenAllowed()
        {
            _fundService.Setup(s => s.GetFundByIdAsync(4)).ReturnsAsync(new FundResponseDto { FundId = 4, ClubId = 2 });
            _memberService.Setup(m => m.IsMemberAsync(_userId, 2)).ReturnsAsync(true);

            var result = await _controller.GetFundLocation(4);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion
    }
}
