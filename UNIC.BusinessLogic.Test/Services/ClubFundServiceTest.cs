using BusinessLogic.DTOs;
using BusinessLogic.Options;
using BusinessLogic.Services.Implementation;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.DataAccess.Repositories.Interface;
using Xunit;

namespace UNIC.BusinessLogic.Test.Services
{
    public class ClubFundServiceTest
    {
        private readonly Mock<IFundRepository> _mockFundRepository;
        private readonly Mock<IClubMemberRepository> _mockClubMemberRepository;
        private readonly ClubFundService _clubFundService;

        public ClubFundServiceTest()
        {
            _mockFundRepository = new Mock<IFundRepository>();
            _mockClubMemberRepository = new Mock<IClubMemberRepository>();
            var mockPayOSService = new Mock<IPayOSService>();
            var mockPolicyService = new Mock<IPolicyService>();
            var payOsOptions = Options.Create(new PayOSOptions { LinkExpirationMinutes = 60 });
            _clubFundService = new ClubFundService(_mockFundRepository.Object, _mockClubMemberRepository.Object, mockPayOSService.Object, mockPolicyService.Object, payOsOptions);
        }

        private static UserClubRole CreateActiveMember(string? roleName = "Manager")
        {
            return new UserClubRole
            {
                Status = "ACTIVE",
                ClubRole = roleName != null ? new ClubRole { RoleName = roleName } : null
            };
        }

        private static UserClubRole CreateActiveMemberWithLevel(string roleName, int level)
        {
            return new UserClubRole
            {
                Status = "ACTIVE",
                ClubRole = new ClubRole { RoleName = roleName, Level = level }
            };
        }

        #region GetFundsByClubIdPagedAsync

        [Fact]
        public async Task GetFundsByClubIdPagedAsync_ShouldReturnFunds_WhenClubHasFunds()
        {
            var clubId = 5;
            var funds = new List<ClubFund>
            {
                new ClubFund { FundId = 1, ClubId = clubId, FundName = "Quỹ A", CurrentBalance = 1000, TotalAmount = 1000, Status = "APPROVED" },
                new ClubFund { FundId = 2, ClubId = clubId, FundName = "Quỹ B", CurrentBalance = 500, TotalAmount = 500, Status = "PENDING" }
            };
            _mockFundRepository.Setup(r => r.GetFundsByClubIdPagedAsync(clubId, 1, 10)).ReturnsAsync((funds, 2));

            var result = await _clubFundService.GetFundsByClubIdPagedAsync(clubId, 1, 10);
            var list = result.Items.ToList();

            Assert.Equal(2, list.Count);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.TotalPages);
            Assert.False(result.HasPreviousPage);
            Assert.False(result.HasNextPage);
            Assert.Equal(1, list[0].FundId);
            Assert.Equal(clubId, list[0].ClubId);
            Assert.Equal("Quỹ A", list[0].FundName);
            Assert.Equal(1000, list[0].CurrentBalance);
            Assert.Equal("APPROVED", list[0].Status);
            Assert.Equal(2, list[1].FundId);
            Assert.Equal(500, list[1].CurrentBalance);
        }

        [Fact]
        public async Task GetFundsByClubIdPagedAsync_ShouldReturnEmpty_WhenClubHasNoFunds()
        {
            var clubId = 99;
            _mockFundRepository.Setup(r => r.GetFundsByClubIdPagedAsync(clubId, 1, 10))
                .ReturnsAsync((new List<ClubFund>(), 0));

            var result = await _clubFundService.GetFundsByClubIdPagedAsync(clubId, 1, 10);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
            Assert.Equal(0, result.TotalPages);
        }

        #endregion

        #region GetFundByIdAsync

        [Fact]
        public async Task GetFundByIdAsync_ShouldReturnDto_WhenExists()
        {
            // Arrange
            var fundId = 1;
            var fund = new ClubFund
            {
                FundId = fundId,
                ClubId = 10,
                FundName = "Quỹ chính",
                CurrentBalance = 2000,
                TotalAmount = 3000,
                CreatedAt = DateTime.UtcNow,
                Status = "APPROVED"
            };
            _mockFundRepository.Setup(r => r.GetFundByIdAsync(fundId)).ReturnsAsync(fund);

            // Act
            var result = await _clubFundService.GetFundByIdAsync(fundId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(fundId, result.FundId);
            Assert.Equal(10, result.ClubId);
            Assert.Equal("Quỹ chính", result.FundName);
            Assert.Equal(2000, result.CurrentBalance);
            Assert.Equal(3000, result.TotalAmount);
            Assert.Equal("APPROVED", result.Status);
        }

        [Fact]
        public async Task GetFundByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            _mockFundRepository.Setup(r => r.GetFundByIdAsync(999)).ReturnsAsync((ClubFund?)null);

            // Act
            var result = await _clubFundService.GetFundByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateFundAsync

        [Fact]
        public async Task CreateFundAsync_ShouldReturnDto_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var clubId = 7;
            var dto = new CreateFundDto { ClubId = clubId, FundName = "Quỹ mới", InitialAmount = 5000 };
            var createdFund = new ClubFund
            {
                FundId = 1,
                ClubId = clubId,
                FundName = "Quỹ mới",
                CurrentBalance = 5000,
                TotalAmount = 5000,
                CreatedAt = DateTime.UtcNow,
                Status = "APPROVED"
            };
            _mockClubMemberRepository.Setup(r => r.GetMemberAsync(userId, clubId))
                .ReturnsAsync(CreateActiveMemberWithLevel("Manager", 1));
            _mockFundRepository.Setup(r => r.AddFundAsync(It.IsAny<ClubFund>())).ReturnsAsync(createdFund);

            // Act
            var result = await _clubFundService.CreateFundAsync(userId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.FundId);
            Assert.Equal(clubId, result.ClubId);
            Assert.Equal("Quỹ mới", result.FundName);
            Assert.Equal(5000, result.CurrentBalance);
            Assert.Equal(5000, result.TotalAmount);
            Assert.Equal("APPROVED", result.Status);
            _mockFundRepository.Verify(r => r.AddFundAsync(It.IsAny<ClubFund>()), Times.Once);
        }

        #endregion

        #region ApproveFundAsync

        [Fact]
        public async Task ApproveFundAsync_ShouldUpdateStatus_WhenValid()
        {
            // Arrange
            var managerId = Guid.NewGuid();
            var fundId = 1;
            var fund = new ClubFund { FundId = fundId, ClubId = 5, Status = "PENDING", FundName = "Quỹ", CurrentBalance = 0, TotalAmount = 0 };
            var dto = new ApproveFundDto { FundId = fundId, Action = "APPROVE" };
            _mockFundRepository.Setup(r => r.GetFundByIdAsync(fundId)).ReturnsAsync(fund);
            _mockClubMemberRepository.Setup(r => r.GetMemberAsync(managerId, 5))
                .ReturnsAsync(CreateActiveMemberWithLevel("Manager", 1));
            _mockFundRepository.Setup(r => r.UpdateClubFundAsync(It.IsAny<ClubFund>())).Returns(Task.CompletedTask);

            // Act
            var result = await _clubFundService.ApproveFundAsync(managerId, dto);

            // Assert
            Assert.True(result);
            Assert.Equal("APPROVED", fund.Status);
            _mockFundRepository.Verify(r => r.UpdateClubFundAsync(fund), Times.Once);
        }

        [Fact]
        public async Task ApproveFundAsync_ShouldThrow_WhenFundNotFound()
        {
            // Arrange
            var managerId = Guid.NewGuid();
            var dto = new ApproveFundDto { FundId = 999, Action = "APPROVE" };
            _mockFundRepository.Setup(r => r.GetFundByIdAsync(999)).ReturnsAsync((ClubFund?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _clubFundService.ApproveFundAsync(managerId, dto));
            Assert.Equal("Quỹ không tồn tại.", ex.Message);
        }

        #endregion

        #region GetContributionPaymentStatusAsync

        [Fact]
        public async Task GetContributionPaymentStatusAsync_ReturnsPaid_WhenApproved()
        {
            var userId = Guid.NewGuid();
            var txn = new FundTransaction
            {
                TransactionId = 10,
                FundId = 1,
                TransactionType = "INCOME",
                Status = "APPROVED",
                Amount = 50_000,
                CreatedBy = userId,
                IsMemberContribution = true,
                TransactionDate = DateTime.UtcNow,
                ClubFund = new ClubFund { FundId = 1, ClubId = 7 }
            };
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(txn);

            var result = await _clubFundService.GetContributionPaymentStatusAsync(userId, 7, 10);

            Assert.NotNull(result);
            Assert.Equal(7, result!.ClubId);
            Assert.True(result.IsPaid);
            Assert.False(result.IsPaymentLinkExpired);
            Assert.Null(result.PaymentLinkExpiresAtUtc);
        }

        [Fact]
        public async Task GetContributionPaymentStatusByOrderCodeAsync_ReturnsSameAsStatus_WhenSameUser()
        {
            var userId = Guid.NewGuid();
            var txn = new FundTransaction
            {
                TransactionId = 18,
                FundId = 3,
                TransactionType = "INCOME",
                Status = "APPROVED",
                Amount = 20_000,
                CreatedBy = userId,
                IsMemberContribution = true,
                TransactionDate = DateTime.UtcNow,
                ClubFund = new ClubFund { FundId = 3, ClubId = 12 }
            };
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(18)).ReturnsAsync(txn);

            var result = await _clubFundService.GetContributionPaymentStatusByOrderCodeAsync(userId, 18);

            Assert.NotNull(result);
            Assert.Equal(12, result!.ClubId);
            Assert.Equal(18, result.TransactionId);
            Assert.Equal(3, result.FundId);
            Assert.True(result.IsPaid);
        }

        [Fact]
        public async Task GetContributionPaymentStatusByOrderCodeAsync_ReturnsNull_WhenWrongUser()
        {
            var userId = Guid.NewGuid();
            var txn = new FundTransaction
            {
                TransactionId = 18,
                TransactionType = "INCOME",
                CreatedBy = Guid.NewGuid(),
                IsMemberContribution = true,
                ClubFund = new ClubFund { ClubId = 1 }
            };
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(18)).ReturnsAsync(txn);

            var result = await _clubFundService.GetContributionPaymentStatusByOrderCodeAsync(userId, 18);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetContributionPaymentStatusAsync_ReturnsNull_WhenWrongUser()
        {
            var userId = Guid.NewGuid();
            var txn = new FundTransaction
            {
                TransactionId = 10,
                FundId = 1,
                TransactionType = "INCOME",
                Status = "PENDING",
                CreatedBy = Guid.NewGuid(),
                IsMemberContribution = true,
                ClubFund = new ClubFund { ClubId = 7 }
            };
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(10)).ReturnsAsync(txn);

            var result = await _clubFundService.GetContributionPaymentStatusAsync(userId, 7, 10);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetContributionPaymentStatusAsync_ReturnsExpired_WhenPendingAndPastExpiry()
        {
            var userId = Guid.NewGuid();
            var txn = new FundTransaction
            {
                TransactionId = 11,
                FundId = 1,
                TransactionType = "INCOME",
                Status = "PENDING",
                Amount = 10_000,
                CreatedBy = userId,
                IsMemberContribution = true,
                TransactionDate = DateTime.UtcNow.AddHours(-2),
                ClubFund = new ClubFund { ClubId = 7 }
            };
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(11)).ReturnsAsync(txn);

            var result = await _clubFundService.GetContributionPaymentStatusAsync(userId, 7, 11);

            Assert.NotNull(result);
            Assert.Equal(7, result!.ClubId);
            Assert.False(result.IsPaid);
            Assert.True(result.IsPaymentLinkExpired);
            Assert.NotNull(result.PaymentLinkExpiresAtUtc);
        }

        #endregion

        #region GetFundHistoryPagedAsync

        [Fact]
        public async Task GetFundHistoryPagedAsync_ShouldReturnTransactionDtos()
        {
            var utc = DateTime.UtcNow;
            var transactions = new List<FundTransaction>
            {
                new FundTransaction
                {
                    TransactionId = 1,
                    FundId = 1,
                    Amount = 100,
                    Description = "x",
                    TransactionType = "INCOME",
                    TransactionDate = utc,
                    CreatedAt = utc,
                    UpdatedAt = utc,
                    Creator = new User { UserId = Guid.NewGuid(), FullName = "Người nộp", Email = "a@b.c" }
                }
            };
            _mockFundRepository.Setup(r => r.GetTransactionsByFundIdPagedAsync(1, "PENDING", true, null, 1, 10))
                .ReturnsAsync((transactions, 1));

            var result = await _clubFundService.GetFundHistoryPagedAsync(1, "pending", null, null, 1, 10);
            var list = result.Items.ToList();

            Assert.Single(list);
            Assert.Equal(1, list[0].TransactionId);
            Assert.Equal(100, list[0].Amount);
            Assert.Equal("Người nộp", list[0].MemberName);
            Assert.Equal("Người nộp", list[0].ContributorName);
            Assert.NotNull(list[0].CreatedAt);
            Assert.NotNull(list[0].UpdatedAt);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task GetFundHistoryPagedAsync_ShouldHandleNullStatus()
        {
            var utc = DateTime.UtcNow;
            var transactions = new List<FundTransaction>
            {
                new FundTransaction
                {
                    Description = "x",
                    TransactionType = "INCOME",
                    TransactionDate = utc,
                    CreatedAt = utc,
                    UpdatedAt = utc
                }
            };
            _mockFundRepository.Setup(r => r.GetTransactionsByFundIdPagedAsync(1, "APPROVED", true, null, 1, 10))
                .ReturnsAsync((transactions, 1));

            var result = await _clubFundService.GetFundHistoryPagedAsync(1, null, null, null, 1, 10);

            Assert.Single(result.Items);
        }

        [Fact]
        public async Task GetFundHistoryPagedAsync_WithStatusAll_PassesNullFilterToRepository()
        {
            var utc = DateTime.UtcNow;
            var transactions = new List<FundTransaction>
            {
                new FundTransaction
                {
                    TransactionId = 1,
                    FundId = 1,
                    Amount = 50,
                    Status = "PENDING",
                    TransactionType = "INCOME",
                    TransactionDate = utc,
                    CreatedAt = utc,
                    UpdatedAt = utc,
                    IsMemberContribution = true
                }
            };
            _mockFundRepository.Setup(r => r.GetTransactionsByFundIdPagedAsync(1, null, true, null, 1, 10))
                .ReturnsAsync((transactions, 1));

            var result = await _clubFundService.GetFundHistoryPagedAsync(1, "ALL", null, null, 1, 10);

            Assert.Single(result.Items);
            _mockFundRepository.Verify(r => r.GetTransactionsByFundIdPagedAsync(1, null, true, null, 1, 10), Times.Once);
        }

        #endregion
    }
}
