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
            var dto = new CreateFundDto { ClubId = 1, FundName = "Q1" };
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

        [Fact]
        public async Task CreateFund_ReturnsBadRequest_WhenUnexpectedException()
        {
            _fundService.Setup(s => s.CreateFundAsync(_userId, It.IsAny<CreateFundDto>()))
                .ThrowsAsync(new InvalidOperationException("db"));

            var result = await _controller.CreateFund(1, new CreateFundDto { FundName = "x" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateFund_ResolvesUserId_FromNameIdentifier_WhenUserIdClaimMissing()
        {
            _controller.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString())
            }, "Test"));

            _fundService.Setup(s => s.CreateFundAsync(_userId, It.IsAny<CreateFundDto>()))
                .ReturnsAsync(new FundResponseDto { FundId = 1, FundName = "Q", ClubId = 1 });

            await _controller.CreateFund(1, new CreateFundDto { FundName = "Q" });

            _fundService.Verify(s => s.CreateFundAsync(_userId, It.IsAny<CreateFundDto>()), Times.Once);
        }

        [Fact]
        public async Task CreateFund_ResolvesUserId_FromSubClaim_WhenOtherClaimsMissing()
        {
            _controller.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("sub", _userId.ToString())
            }, "Test"));

            _fundService.Setup(s => s.CreateFundAsync(_userId, It.IsAny<CreateFundDto>()))
                .ReturnsAsync(new FundResponseDto { FundId = 1, FundName = "Q", ClubId = 1 });

            await _controller.CreateFund(1, new CreateFundDto { FundName = "Q" });

            _fundService.Verify(s => s.CreateFundAsync(_userId, It.IsAny<CreateFundDto>()), Times.Once);
        }

        #endregion

        #region GetMyClubs

        [Fact]
        public async Task GetMyClubs_ThrowsUnauthorized_WhenNoUserClaim()
        {
            _controller.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity());

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetMyClubs());
        }

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

        #region GetFundReportSummary

        [Fact]
        public async Task GetFundReportSummary_Returns403_WhenNotMember()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 2)).ReturnsAsync(false);

            var result = await _controller.GetFundReportSummary(2, null, null);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task GetFundReportSummary_ReturnsOk_WhenMember()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 2)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetClubFundReportSummaryAsync(2, null, null))
                .ReturnsAsync(new ClubFundReportSummaryDto
                {
                    ClubId = 2,
                    ApprovedFundCount = 1,
                    TotalBalanceApprovedFunds = 50m
                });

            var result = await _controller.GetFundReportSummary(2, null, null);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetFundReportSummary_ReturnsBadRequest_WhenFromAfterTo()
        {
            var from = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            var result = await _controller.GetFundReportSummary(2, from, to);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
            _fundService.Verify(
                s => s.GetClubFundReportSummaryAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()),
                Times.Never);
        }

        #endregion

        #region GetFundCategories

        [Fact]
        public async Task GetFundCategories_Returns403_WhenNotMember()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 3)).ReturnsAsync(false);

            var result = await _controller.GetFundCategories(3);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task GetFundCategories_ReturnsOk_WhenMember()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 3)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundCategoriesForClubAsync(3))
                .ReturnsAsync(Array.Empty<FundCategoryResponseDto>());

            var result = await _controller.GetFundCategories(3);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetFundCategories_ReturnsUnauthorized_WhenServiceThrows()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 3)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundCategoriesForClubAsync(3))
                .ThrowsAsync(new UnauthorizedAccessException());

            var result = await _controller.GetFundCategories(3);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        #endregion

        #region GetClubFundTransactions

        [Fact]
        public async Task GetClubFundTransactions_Returns403_WhenNotMember()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 2)).ReturnsAsync(false);

            var result = await _controller.GetClubFundTransactions(2, null, null, null, null, null, 1, 20);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task GetClubFundTransactions_ReturnsBadRequest_WhenPageInvalid()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);

            var result = await _controller.GetClubFundTransactions(1, null, null, null, null, null, 0, 20);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetClubFundTransactions_ReturnsBadRequest_WhenPageSizeInvalid()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);

            var result = await _controller.GetClubFundTransactions(1, null, null, null, null, null, 1, 0);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetClubFundTransactions_ReturnsBadRequest_WhenFromAfterTo()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            var from = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

            var result = await _controller.GetClubFundTransactions(1, null, null, null, from, to, 1, 20);

            Assert.IsType<BadRequestObjectResult>(result);
            _fundService.Verify(
                s => s.GetClubFundTransactionsPagedAsync(
                    It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                    It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task GetClubFundTransactions_ReturnsUnauthorized_WhenUnauthorizedAccess()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 2)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetClubFundTransactionsPagedAsync(
                    2, null, null, null, _userId, null, null, 1, 20))
                .ThrowsAsync(new UnauthorizedAccessException());

            var result = await _controller.GetClubFundTransactions(2, null, null, null, null, null, 1, 20);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetClubFundTransactions_ReturnsBadRequest_WhenFundNotInClub()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundByIdAsync(5)).ReturnsAsync(new FundResponseDto
            {
                FundId = 5,
                ClubId = 99
            });

            var result = await _controller.GetClubFundTransactions(1, 5, null, null, null, null, 1, 20);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetClubFundTransactions_ReturnsNotFound_WhenFundMissing()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundByIdAsync(999)).ReturnsAsync((FundResponseDto?)null);

            var result = await _controller.GetClubFundTransactions(1, 999, null, null, null, null, 1, 20);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetClubFundTransactions_ReturnsOk_WhenMember()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 2)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetClubFundTransactionsPagedAsync(
                    2, null, null, null, _userId, null, null, 1, 20))
                .ReturnsAsync(new PagedResultDto<FundTransactionResponseDto>
                {
                    Items = Array.Empty<FundTransactionResponseDto>(),
                    PageNumber = 1,
                    PageSize = 20,
                    TotalCount = 0
                });

            var result = await _controller.GetClubFundTransactions(2, null, null, null, null, null, 1, 20);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetClubFundTransactions_ReturnsBadRequest_WhenGenericExceptionFromService()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetClubFundTransactionsPagedAsync(
                    1, null, null, null, _userId, null, null, 1, 20))
                .ThrowsAsync(new Exception("query"));

            var result = await _controller.GetClubFundTransactions(1, null, null, null, null, null, 1, 20);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetClubFundTransactions_ReturnsBadRequest_WhenArgumentExceptionFromService()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetClubFundTransactionsPagedAsync(
                    1, null, null, null, _userId, null, null, 1, 20))
                .ThrowsAsync(new ArgumentException("bad"));

            var result = await _controller.GetClubFundTransactions(1, null, null, null, null, null, 1, 20);

            Assert.IsType<BadRequestObjectResult>(result);
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
            var result = await _controller.GetFundsByClub(1, null, null, null, 0, 10);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetFundsByClub_ReturnsBadRequest_WhenPageSizeInvalid()
        {
            var result = await _controller.GetFundsByClub(1, null, null, null, 1, 200);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetFundsByClub_Returns403_WhenNotMember()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 2)).ReturnsAsync(false);

            var result = await _controller.GetFundsByClub(2, null, null, null, 1, 10);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task GetFundsByClub_ReturnsOk_WhenAllowed()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 2)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundsByClubIdPagedAsync(2, _userId, false, "APPROVED", "quy", "NAME_ASC", 1, 10))
                .ReturnsAsync(new PagedResultDto<FundResponseDto>
                {
                    Items = Array.Empty<FundResponseDto>(),
                    PageNumber = 1,
                    PageSize = 10,
                    TotalCount = 0
                });

            var result = await _controller.GetFundsByClub(2, "APPROVED", "quy", "NAME_ASC", 1, 10);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetFundsByClub_PassesIsSystemAdminTrue_WhenUserIsAdmin()
        {
            SetAuthenticatedUser(_userId, isAdmin: true);
            _memberService.Setup(m => m.IsMemberAsync(_userId, 2)).ReturnsAsync(false);
            _fundService.Setup(s => s.GetFundsByClubIdPagedAsync(2, _userId, true, null, null, null, 1, 10))
                .ReturnsAsync(new PagedResultDto<FundResponseDto>
                {
                    Items = Array.Empty<FundResponseDto>(),
                    PageNumber = 1,
                    PageSize = 10,
                    TotalCount = 0
                });

            var result = await _controller.GetFundsByClub(2, null, null, null, 1, 10);

            Assert.IsType<OkObjectResult>(result);
        }

        #endregion

        #region GetMyFunds

        [Fact]
        public async Task GetMyFunds_Returns403_WhenNotMember()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 2)).ReturnsAsync(false);

            var result = await _controller.GetMyFunds(2, null, null, null, null, 1, 10);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task GetMyFunds_ReturnsBadRequest_WhenMineTypeInvalid()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 2)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetMyFundsByClubIdPagedAsync(2, _userId, "WRONG", null, null, null, 1, 10))
                .ThrowsAsync(new ArgumentException("mineType invalid"));

            var result = await _controller.GetMyFunds(2, "WRONG", null, null, null, 1, 10);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetMyFunds_ReturnsOk_WhenAllowed()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 2)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetMyFundsByClubIdPagedAsync(2, _userId, "ALL", "APPROVED", "quy", "NEWEST", 1, 10))
                .ReturnsAsync(new PagedResultDto<FundResponseDto>
                {
                    Items = Array.Empty<FundResponseDto>(),
                    PageNumber = 1,
                    PageSize = 10,
                    TotalCount = 0
                });

            var result = await _controller.GetMyFunds(2, "ALL", "APPROVED", "quy", "NEWEST", 1, 10);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetMyFunds_ReturnsBadRequest_WhenPageSizeTooLarge()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 2)).ReturnsAsync(true);

            var result = await _controller.GetMyFunds(2, null, null, null, null, 1, 101);

            Assert.IsType<BadRequestObjectResult>(result);
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
        public async Task Contribute_Returns403_WhenServiceThrowsUnauthorized()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundByIdAsync(2)).ReturnsAsync(new FundResponseDto { FundId = 2, ClubId = 1 });
            _fundService.Setup(s => s.CreateContributionAsync(_userId, It.IsAny<ContributeRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException("no"));

            var result = await _controller.Contribute(1, new ContributeRequestDto { FundId = 2, Amount = 5000 }, CancellationToken.None);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task Contribute_ReturnsBadRequest_WhenArgumentExceptionFromService()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundByIdAsync(2)).ReturnsAsync(new FundResponseDto { FundId = 2, ClubId = 1 });
            _fundService.Setup(s => s.CreateContributionAsync(_userId, It.IsAny<ContributeRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("amount"));

            var result = await _controller.Contribute(1, new ContributeRequestDto { FundId = 2, Amount = 5000 }, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Contribute_ReturnsBadRequest_WhenInvalidOperationFromService()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundByIdAsync(2)).ReturnsAsync(new FundResponseDto { FundId = 2, ClubId = 1 });
            _fundService.Setup(s => s.CreateContributionAsync(_userId, It.IsAny<ContributeRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("pay"));

            var result = await _controller.Contribute(1, new ContributeRequestDto { FundId = 2, Amount = 5000 }, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Contribute_ReturnsBadRequest_WhenGenericExceptionFromService()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundByIdAsync(2)).ReturnsAsync(new FundResponseDto { FundId = 2, ClubId = 1 });
            _fundService.Setup(s => s.CreateContributionAsync(_userId, It.IsAny<ContributeRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("surprise"));

            var result = await _controller.Contribute(1, new ContributeRequestDto { FundId = 2, Amount = 5000 }, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result);
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
        public async Task GetPayOsContributionReturn_ReturnsOk_WhenSuccess()
        {
            _fundService.Setup(s => s.GetContributionPaymentStatusByOrderCodeAsync(_userId, 100))
                .ReturnsAsync(new ContributionPaymentStatusDto
                {
                    ClubId = 3,
                    TransactionId = 100,
                    FundId = 1,
                    Status = "PENDING",
                    Amount = 10000m
                });
            _memberService.Setup(m => m.IsMemberAsync(_userId, 3)).ReturnsAsync(true);

            var result = await _controller.GetPayOsContributionReturn(100);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetPayOsContributionReturn_ReturnsUnauthorized_WhenServiceThrows()
        {
            _fundService.Setup(s => s.GetContributionPaymentStatusByOrderCodeAsync(_userId, 55))
                .ThrowsAsync(new UnauthorizedAccessException());

            var result = await _controller.GetPayOsContributionReturn(55);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetContributionPaymentStatus_Returns403_WhenNotMember()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(false);

            var result = await _controller.GetContributionPaymentStatus(1, 9);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task GetContributionPaymentStatus_ReturnsOk_WhenFound()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetContributionPaymentStatusAsync(_userId, 1, 9))
                .ReturnsAsync(new ContributionPaymentStatusDto
                {
                    ClubId = 1,
                    TransactionId = 9,
                    FundId = 2,
                    Status = "APPROVED",
                    Amount = 5000m,
                    IsPaid = true
                });

            var result = await _controller.GetContributionPaymentStatus(1, 9);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetContributionPaymentStatus_ReturnsUnauthorized_WhenServiceThrows()
        {
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetContributionPaymentStatusAsync(_userId, 1, 9))
                .ThrowsAsync(new UnauthorizedAccessException());

            var result = await _controller.GetContributionPaymentStatus(1, 9);

            Assert.IsType<UnauthorizedObjectResult>(result);
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

        [Fact]
        public async Task SimulatePayOsPaidForDevelopment_Returns403_WhenNotMember()
        {
            _environment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(false);

            var result = await _controller.SimulatePayOsPaidForDevelopment(1, 2);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task SimulatePayOsPaidForDevelopment_ReturnsBadRequest_WhenServiceReturnsFalse()
        {
            _environment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.TryCompleteOwnPendingContributionForDevelopmentAsync(_userId, 1, 2))
                .ReturnsAsync(false);

            var result = await _controller.SimulatePayOsPaidForDevelopment(1, 2);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SimulatePayOsPaidForDevelopment_ReturnsUnauthorized_WhenServiceThrows()
        {
            _environment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            _memberService.Setup(m => m.IsMemberAsync(_userId, 1)).ReturnsAsync(true);
            _fundService.Setup(s => s.TryCompleteOwnPendingContributionForDevelopmentAsync(_userId, 1, 2))
                .ThrowsAsync(new UnauthorizedAccessException());

            var result = await _controller.SimulatePayOsPaidForDevelopment(1, 2);

            Assert.IsType<UnauthorizedObjectResult>(result);
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

        [Fact]
        public async Task ApproveFund_ReturnsBadRequest_WhenArgumentException()
        {
            _fundService.Setup(s => s.ApproveFundAsync(_userId, It.IsAny<ApproveFundDto>()))
                .ThrowsAsync(new ArgumentException("Lý do từ chối phải có ít nhất 5 ký tự sau khi bỏ khoảng trắng đầu cuối.", nameof(ApproveFundDto.RejectReason)));

            var result = await _controller.ApproveFund(new ApproveFundDto { FundId = 1, Action = "REJECT", RejectReason = "x" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ApproveFund_ReturnsBadRequest_WhenInvalidOperationException()
        {
            _fundService.Setup(s => s.ApproveFundAsync(_userId, It.IsAny<ApproveFundDto>()))
                .ThrowsAsync(new InvalidOperationException("Quỹ đã được duyệt trước đó."));

            var result = await _controller.ApproveFund(new ApproveFundDto { FundId = 1, Action = "APPROVE" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ApproveFund_ReturnsBadRequest_WhenGenericException()
        {
            _fundService.Setup(s => s.ApproveFundAsync(_userId, It.IsAny<ApproveFundDto>()))
                .ThrowsAsync(new Exception("unexpected"));

            var result = await _controller.ApproveFund(new ApproveFundDto { FundId = 1, Action = "APPROVE" });

            Assert.IsType<BadRequestObjectResult>(result);
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

        [Fact]
        public async Task PayOSWebhook_ReturnsBadRequest_WhenOrderCodeInvalid()
        {
            SetJsonBody("{\"code\":\"00\",\"success\":true,\"data\":{\"orderCode\":0},\"signature\":\"good\"}");
            _payOSService.Setup(p => p.VerifyWebhookSignature("good", It.IsAny<System.Text.Json.JsonElement>())).Returns(true);

            var result = await _controller.PayOSWebhook(CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result);
            _fundService.Verify(s => s.ProcessPayOSPaymentSuccessAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task PayOSWebhook_ReturnsInternalServerError_WhenBodyIsInvalidJson()
        {
            SetJsonBody("{ not json");

            var result = await _controller.PayOSWebhook(CancellationToken.None);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task PayOSWebhook_ReturnsOk_WhenSuccessPropertyMissing()
        {
            SetJsonBody("{\"code\":\"00\"}");

            var result = await _controller.PayOSWebhookClubScoped(CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            _fundService.Verify(s => s.ProcessPayOSPaymentSuccessAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task PayOSWebhook_ReturnsBadRequest_WhenSignatureJsonNull()
        {
            SetJsonBody("{\"code\":\"00\",\"success\":true,\"data\":{\"orderCode\":1},\"signature\":null}");
            var result = await _controller.PayOSWebhookClubScoped(CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result);
            _payOSService.Verify(p => p.VerifyWebhookSignature(It.IsAny<string>(), It.IsAny<System.Text.Json.JsonElement>()), Times.Never);
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
            var result = await _controller.GetHistory(1, 1, null, null, 0, 10);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetHistory_ReturnsBadRequest_WhenPageSizeTooLarge()
        {
            var result = await _controller.GetHistory(1, 1, null, null, 1, 101);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetHistory_ReturnsUnauthorized_WhenServiceThrowsUnauthorized()
        {
            _fundService.Setup(s => s.GetFundByIdAsync(3)).ReturnsAsync(new FundResponseDto { FundId = 3, ClubId = 8 });
            _memberService.Setup(m => m.IsMemberAsync(_userId, 8)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundHistoryPagedAsync(3, null, null, _userId, 1, 10))
                .ThrowsAsync(new UnauthorizedAccessException());

            var result = await _controller.GetHistory(8, 3, null, null, 1, 10);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetHistory_ReturnsBadRequest_WhenServiceThrowsGenericException()
        {
            _fundService.Setup(s => s.GetFundByIdAsync(3)).ReturnsAsync(new FundResponseDto { FundId = 3, ClubId = 8 });
            _memberService.Setup(m => m.IsMemberAsync(_userId, 8)).ReturnsAsync(true);
            _fundService.Setup(s => s.GetFundHistoryPagedAsync(3, null, null, _userId, 1, 10))
                .ThrowsAsync(new Exception("db"));

            var result = await _controller.GetHistory(8, 3, null, null, 1, 10);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetHistory_ReturnsBadRequest_WhenPageSizeZero()
        {
            _fundService.Setup(s => s.GetFundByIdAsync(3)).ReturnsAsync(new FundResponseDto { FundId = 3, ClubId = 8 });
            _memberService.Setup(m => m.IsMemberAsync(_userId, 8)).ReturnsAsync(true);

            var result = await _controller.GetHistory(8, 3, null, null, 1, 0);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetHistory_ReturnsNotFound_WhenFundMissing()
        {
            _fundService.Setup(s => s.GetFundByIdAsync(3)).ReturnsAsync((FundResponseDto?)null);

            var result = await _controller.GetHistory(1, 3, null, null, 1, 10);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetHistory_Returns403_WhenFundNotInRouteClub()
        {
            _fundService.Setup(s => s.GetFundByIdAsync(3)).ReturnsAsync(new FundResponseDto { FundId = 3, ClubId = 8 });

            var result = await _controller.GetHistory(1, 3, null, null, 1, 10);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task GetHistory_Returns403_WhenCannotAccessClub()
        {
            _fundService.Setup(s => s.GetFundByIdAsync(3)).ReturnsAsync(new FundResponseDto { FundId = 3, ClubId = 8 });
            _memberService.Setup(m => m.IsMemberAsync(_userId, 8)).ReturnsAsync(false);

            var result = await _controller.GetHistory(8, 3, null, null, 1, 10);

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

            var result = await _controller.GetHistory(8, 3, null, null, 1, 10);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetHistory_ReturnsOk_WhenAdminWithoutClubMembership()
        {
            SetAuthenticatedUser(_userId, isAdmin: true);
            _fundService.Setup(s => s.GetFundByIdAsync(3)).ReturnsAsync(new FundResponseDto { FundId = 3, ClubId = 8 });
            _fundService.Setup(s => s.GetFundHistoryPagedAsync(3, null, null, _userId, 1, 10))
                .ReturnsAsync(new PagedResultDto<FundTransactionResponseDto>
                {
                    Items = Array.Empty<FundTransactionResponseDto>(),
                    PageNumber = 1,
                    PageSize = 10,
                    TotalCount = 0
                });

            var result = await _controller.GetHistory(8, 3, null, null, 1, 10);

            Assert.IsType<OkObjectResult>(result);
            _memberService.Verify(m => m.IsMemberAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetFundLocation_Returns403_WhenCannotAccessClub()
        {
            _fundService.Setup(s => s.GetFundByIdAsync(4)).ReturnsAsync(new FundResponseDto { FundId = 4, ClubId = 9 });
            _memberService.Setup(m => m.IsMemberAsync(_userId, 9)).ReturnsAsync(false);

            var result = await _controller.GetFundLocation(4);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
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
