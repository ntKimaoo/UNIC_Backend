using BusinessLogic.DTOs;
using BusinessLogic.Options;
using BusinessLogic.Services.Implementation;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.Extensions.Options;
using UNIC.DataAccess.Repositories.Interface;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class ClubFundServiceTest
    {
        private readonly Mock<IFundRepository> _fundRepo;
        private readonly Mock<IClubMemberRepository> _memberRepo;
        private readonly Mock<IPayOSService> _payOS;
        private readonly Mock<IPolicyService> _policy;
        private readonly ClubFundService _service;

        public ClubFundServiceTest()
        {
            _fundRepo = new Mock<IFundRepository>();
            _memberRepo = new Mock<IClubMemberRepository>();
            _payOS = new Mock<IPayOSService>();
            _policy = new Mock<IPolicyService>();

            var payOpts = Options.Create(new PayOSOptions { LinkExpirationMinutes = 60 });
            _service = new ClubFundService(
                _fundRepo.Object,
                _memberRepo.Object,
                _payOS.Object,
                _policy.Object,
                payOpts);
        }

        private static UserClubRole ActiveManagerMember(int clubId, int level = 1) =>
            new UserClubRole
            {
                ClubId = clubId,
                Status = "ACTIVE",
                ClubRole = new ClubRole { Level = level, RoleName = "Role" }
            };

        #region CreateFundAsync

        [Fact]
        public async Task CreateFundAsync_ShouldThrow_WhenFundNameEmpty()
        {
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateFundAsync(Guid.NewGuid(), new CreateFundDto { ClubId = 1, FundName = "  " }));
            Assert.Contains("Tên quỹ", ex.Message);
        }

        [Fact]
        public async Task CreateFundAsync_ShouldThrow_WhenInitialAmountNegative()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateFundAsync(Guid.NewGuid(), new CreateFundDto
                {
                    ClubId = 1,
                    FundName = "Q",
                    InitialAmount = -1
                }));
        }

        [Fact]
        public async Task CreateFundAsync_ShouldThrow_WhenExpiresAtInPast()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateFundAsync(Guid.NewGuid(), new CreateFundDto
                {
                    ClubId = 1,
                    FundName = "Q",
                    InitialAmount = 0,
                    ExpiresAt = DateTime.UtcNow.Date.AddDays(-1)
                }));
        }

        [Fact]
        public async Task CreateFundAsync_ShouldThrow_WhenNotMember()
        {
            var uid = Guid.NewGuid();
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 1)).ReturnsAsync((UserClubRole?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.CreateFundAsync(uid, new CreateFundDto { ClubId = 1, FundName = "Q" }));
        }

        [Fact]
        public async Task CreateFundAsync_ShouldThrow_WhenMemberInactive()
        {
            var uid = Guid.NewGuid();
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 1)).ReturnsAsync(new UserClubRole
            {
                Status = "LEFT",
                ClubRole = new ClubRole { Level = 1 }
            });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.CreateFundAsync(uid, new CreateFundDto { ClubId = 1, FundName = "Q" }));
        }

        [Fact]
        public async Task CreateFundAsync_ShouldThrow_WhenNotManagerOrVice()
        {
            var uid = Guid.NewGuid();
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 1)).ReturnsAsync(new UserClubRole
            {
                Status = "ACTIVE",
                ClubRole = new ClubRole { Level = 3 }
            });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.CreateFundAsync(uid, new CreateFundDto { ClubId = 1, FundName = "Q" }));
        }

        [Fact]
        public async Task CreateFundAsync_ShouldSetApproved_WhenHighestLevelManager()
        {
            var uid = Guid.NewGuid();
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 1)).ReturnsAsync(ActiveManagerMember(1, level: 1));

            ClubFund? captured = null;
            _fundRepo.Setup(r => r.AddFundAsync(It.IsAny<ClubFund>()))
                .Callback<ClubFund>(f => captured = f)
                .ReturnsAsync((ClubFund f) => f);

            var result = await _service.CreateFundAsync(uid, new CreateFundDto
            {
                ClubId = 1,
                FundName = " Quỹ A ",
                InitialAmount = 5000
            });

            Assert.NotNull(captured);
            Assert.Equal("APPROVED", captured!.Status);
            Assert.Equal(5000, captured.CurrentBalance);
            Assert.Equal("Quỹ A", result.FundName);
        }

        [Fact]
        public async Task CreateFundAsync_ShouldSetPending_WhenViceManager()
        {
            var uid = Guid.NewGuid();
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 1)).ReturnsAsync(ActiveManagerMember(1, level: 2));

            ClubFund? captured = null;
            _fundRepo.Setup(r => r.AddFundAsync(It.IsAny<ClubFund>()))
                .Callback<ClubFund>(f => captured = f)
                .ReturnsAsync((ClubFund f) => f);

            await _service.CreateFundAsync(uid, new CreateFundDto { ClubId = 1, FundName = "Q", InitialAmount = 0 });

            Assert.Equal("PENDING", captured!.Status);
        }

        #endregion

        #region CreateContributionAsync

        [Fact]
        public async Task CreateContributionAsync_ShouldThrow_WhenAmountZero()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateContributionAsync(Guid.NewGuid(), new ContributeRequestDto
                {
                    FundId = 1,
                    Amount = 0
                }, CancellationToken.None));
        }

        [Fact]
        public async Task CreateContributionAsync_ShouldThrow_WhenAmountBelowMinimum()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateContributionAsync(Guid.NewGuid(), new ContributeRequestDto
                {
                    FundId = 1,
                    Amount = 500
                }, CancellationToken.None));
        }

        [Fact]
        public async Task CreateContributionAsync_ShouldThrow_WhenFundMissing()
        {
            _fundRepo.Setup(r => r.GetFundByIdAsync(99)).ReturnsAsync((ClubFund?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateContributionAsync(Guid.NewGuid(), new ContributeRequestDto
                {
                    FundId = 99,
                    Amount = 5000
                }, CancellationToken.None));
        }

        [Fact]
        public async Task CreateContributionAsync_ShouldThrow_WhenFundNotApproved()
        {
            _fundRepo.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync(new ClubFund
            {
                FundId = 1,
                ClubId = 1,
                Status = "PENDING",
                FundName = "F"
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateContributionAsync(Guid.NewGuid(), new ContributeRequestDto
                {
                    FundId = 1,
                    Amount = 5000
                }, CancellationToken.None));
        }

        [Fact]
        public async Task CreateContributionAsync_ShouldThrow_WhenFundExpired()
        {
            _fundRepo.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync(new ClubFund
            {
                FundId = 1,
                ClubId = 1,
                Status = "APPROVED",
                FundName = "F",
                ExpiresAt = DateTime.UtcNow.Date.AddDays(-2)
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateContributionAsync(Guid.NewGuid(), new ContributeRequestDto
                {
                    FundId = 1,
                    Amount = 5000
                }, CancellationToken.None));
        }

        [Fact]
        public async Task CreateContributionAsync_ShouldReturnPayOsPayload_WhenValid()
        {
            var uid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync(new ClubFund
            {
                FundId = 1,
                ClubId = 2,
                Status = "APPROVED",
                FundName = "F"
            });
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 2)).ReturnsAsync(ActiveManagerMember(2, 1));

            _fundRepo.Setup(r => r.AddTransactionAsync(It.IsAny<FundTransaction>()))
                .Callback<FundTransaction>(t => t.TransactionId = 100)
                .Returns(Task.CompletedTask);

            _payOS.Setup(p => p.CreatePaymentLinkAsync(100, 10_000m, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PayOSPaymentLinkResult
                {
                    CheckoutUrl = "https://pay.test",
                    QrCode = "qr",
                    PaymentLinkId = "pl_1"
                });

            _fundRepo.Setup(r => r.UpdateTransactionAsync(It.IsAny<FundTransaction>())).Returns(Task.CompletedTask);

            var result = await _service.CreateContributionAsync(uid, new ContributeRequestDto
            {
                FundId = 1,
                Amount = 10_000,
                Description = "Nộp"
            }, CancellationToken.None);

            Assert.Equal(100, result.TransactionId);
            Assert.Equal("https://pay.test", result.CheckoutUrl);
            Assert.Equal("pl_1", result.PaymentLinkId);
            _payOS.Verify(p => p.CreatePaymentLinkAsync(100, 10_000m, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetContributionPaymentStatus

        [Fact]
        public async Task GetContributionPaymentStatusAsync_ShouldReturnNull_WhenWrongUser()
        {
            var t = new FundTransaction
            {
                TransactionId = 1,
                IsMemberContribution = true,
                TransactionType = "INCOME",
                CreatedBy = Guid.NewGuid(),
                ClubFund = new ClubFund { ClubId = 1 }
            };
            _fundRepo.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(t);

            var result = await _service.GetContributionPaymentStatusAsync(Guid.NewGuid(), 1, 1);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetContributionPaymentStatusByOrderCodeAsync_ShouldMap_WhenPaid()
        {
            var uid = Guid.NewGuid();
            var t = new FundTransaction
            {
                TransactionId = 7,
                FundId = 2,
                IsMemberContribution = true,
                TransactionType = "INCOME",
                Status = "APPROVED",
                Amount = 1000,
                CreatedBy = uid,
                TransactionDate = DateTime.UtcNow,
                ClubFund = new ClubFund { ClubId = 3 }
            };
            _fundRepo.Setup(r => r.GetTransactionByIdAsync(7)).ReturnsAsync(t);

            var dto = await _service.GetContributionPaymentStatusByOrderCodeAsync(uid, 7);
            Assert.NotNull(dto);
            Assert.True(dto!.IsPaid);
            Assert.Equal(3, dto.ClubId);
        }

        #endregion

        #region GetFundsByClubIdPagedAsync / GetFundByIdAsync

        [Fact]
        public async Task GetFundByIdAsync_ShouldReturnNull_WhenMissing()
        {
            _fundRepo.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync((ClubFund?)null);
            Assert.Null(await _service.GetFundByIdAsync(1));
        }

        [Fact]
        public async Task GetFundsByClubIdPagedAsync_ShouldReturnPagedDtos()
        {
            var list = new List<ClubFund>
            {
                new ClubFund
                {
                    FundId = 1,
                    ClubId = 2,
                    FundName = "A",
                    Status = "APPROVED",
                    TotalAmount = 0,
                    CurrentBalance = 0,
                    CreatedAt = DateTime.UtcNow
                }
            };
            _fundRepo.Setup(r => r.GetFundsByClubIdPagedAsync(2, 1, 10)).ReturnsAsync((list, 1));

            var page = await _service.GetFundsByClubIdPagedAsync(2, 1, 10);
            Assert.Single(page.Items);
            Assert.Equal(1, page.TotalCount);
        }

        #endregion

        #region GetFundHistoryPagedAsync

        [Fact]
        public async Task GetFundHistoryPagedAsync_ShouldDefaultStatusToApproved_WhenEmpty()
        {
            _fundRepo.Setup(r => r.GetTransactionsByFundIdPagedAsync(
                    1, "APPROVED", true, null, 1, 10))
                .ReturnsAsync((Enumerable.Empty<FundTransaction>(), 0));

            await _service.GetFundHistoryPagedAsync(1, null, null, Guid.NewGuid(), 1, 10);

            _fundRepo.Verify(r => r.GetTransactionsByFundIdPagedAsync(
                1, "APPROVED", true, null, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetFundHistoryPagedAsync_ShouldPassNullStatus_WhenAll()
        {
            _fundRepo.Setup(r => r.GetTransactionsByFundIdPagedAsync(
                    1, null, true, null, 1, 10))
                .ReturnsAsync((Enumerable.Empty<FundTransaction>(), 0));

            await _service.GetFundHistoryPagedAsync(1, "ALL", null, null, 1, 10);

            _fundRepo.Verify(r => r.GetTransactionsByFundIdPagedAsync(
                1, null, true, null, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetFundHistoryPagedAsync_ShouldFilterMine_WhenScopeMine()
        {
            var uid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetTransactionsByFundIdPagedAsync(
                    1, "APPROVED", true, uid, 1, 10))
                .ReturnsAsync((Enumerable.Empty<FundTransaction>(), 0));

            await _service.GetFundHistoryPagedAsync(1, "", "mine", uid, 1, 10);

            _fundRepo.Verify(r => r.GetTransactionsByFundIdPagedAsync(
                1, "APPROVED", true, uid, 1, 10), Times.Once);
        }

        #endregion

        #region ApproveFundAsync

        [Fact]
        public async Task ApproveFundAsync_ShouldThrow_WhenFundNotFound()
        {
            _fundRepo.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync((ClubFund?)null);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ApproveFundAsync(Guid.NewGuid(), new ApproveFundDto { FundId = 1, Action = "APPROVE" }));
        }

        [Fact]
        public async Task ApproveFundAsync_ShouldThrow_WhenAlreadyApproved()
        {
            _fundRepo.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync(new ClubFund
            {
                FundId = 1,
                ClubId = 1,
                Status = "APPROVED",
                FundName = "F"
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ApproveFundAsync(Guid.NewGuid(), new ApproveFundDto { FundId = 1, Action = "APPROVE" }));
        }

        [Fact]
        public async Task ApproveFundAsync_ShouldThrow_WhenNotTopManager()
        {
            var mid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync(new ClubFund
            {
                FundId = 1,
                ClubId = 2,
                Status = "PENDING",
                FundName = "F"
            });
            _memberRepo.Setup(r => r.GetMemberAsync(mid, 2)).ReturnsAsync(ActiveManagerMember(2, level: 2));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.ApproveFundAsync(mid, new ApproveFundDto { FundId = 1, Action = "APPROVE" }));
        }

        [Fact]
        public async Task ApproveFundAsync_ShouldUpdate_WhenReject()
        {
            var mid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync(new ClubFund
            {
                FundId = 1,
                ClubId = 2,
                Status = "PENDING",
                FundName = "F"
            });
            _memberRepo.Setup(r => r.GetMemberAsync(mid, 2)).ReturnsAsync(ActiveManagerMember(2, level: 1));
            _fundRepo.Setup(r => r.UpdateClubFundAsync(It.IsAny<ClubFund>())).Returns(Task.CompletedTask);

            var ok = await _service.ApproveFundAsync(mid, new ApproveFundDto { FundId = 1, Action = "REJECT" });
            Assert.True(ok);
            _fundRepo.Verify(r => r.UpdateClubFundAsync(It.Is<ClubFund>(f => f.Status == "REJECTED")), Times.Once);
        }

        #endregion

        #region ProcessPayOSPaymentSuccessAsync / TryCompleteOwnPending

        [Fact]
        public async Task ProcessPayOSPaymentSuccessAsync_ShouldReturnFalse_WhenTransactionInvalid()
        {
            _fundRepo.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync((FundTransaction?)null);
            Assert.False(await _service.ProcessPayOSPaymentSuccessAsync(1));
        }

        [Fact]
        public async Task ProcessPayOSPaymentSuccessAsync_ShouldUpdateBalances_WhenValidPendingIncome()
        {
            var fund = new ClubFund
            {
                FundId = 1,
                ClubId = 1,
                CurrentBalance = 100,
                TotalAmount = 100,
                FundName = "F"
            };
            var tx = new FundTransaction
            {
                TransactionId = 9,
                FundId = 1,
                IsMemberContribution = true,
                TransactionType = "INCOME",
                Status = "PENDING",
                Amount = 50,
                ClubFund = fund
            };
            _fundRepo.Setup(r => r.GetTransactionByIdAsync(9)).ReturnsAsync(tx);
            _fundRepo.Setup(r => r.UpdateTransactionAndFundAsync(It.IsAny<FundTransaction>(), It.IsAny<ClubFund>()))
                .Returns(Task.CompletedTask);

            var ok = await _service.ProcessPayOSPaymentSuccessAsync(9);
            Assert.True(ok);
            Assert.Equal(150, fund.CurrentBalance);
            Assert.Equal(150, fund.TotalAmount);
            Assert.Equal("APPROVED", tx.Status);
        }

        [Fact]
        public async Task TryCompleteOwnPendingContributionForDevelopmentAsync_ShouldReturnFalse_WhenWrongClub()
        {
            var uid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(new FundTransaction
            {
                TransactionId = 1,
                CreatedBy = uid,
                ClubFund = new ClubFund { ClubId = 5 },
                IsMemberContribution = true,
                TransactionType = "INCOME",
                Status = "PENDING"
            });

            Assert.False(await _service.TryCompleteOwnPendingContributionForDevelopmentAsync(uid, 99, 1));
        }

        [Fact]
        public async Task TryCompleteOwnPendingContributionForDevelopmentAsync_ShouldComplete_WhenValid()
        {
            var uid = Guid.NewGuid();
            var fund = new ClubFund { FundId = 1, ClubId = 2, CurrentBalance = 0, TotalAmount = 0, FundName = "F" };
            var tx = new FundTransaction
            {
                TransactionId = 3,
                FundId = 1,
                CreatedBy = uid,
                ClubFund = fund,
                IsMemberContribution = true,
                TransactionType = "INCOME",
                Status = "PENDING",
                Amount = 20
            };
            _fundRepo.Setup(r => r.GetTransactionByIdAsync(3)).ReturnsAsync(tx);
            _fundRepo.Setup(r => r.UpdateTransactionAndFundAsync(It.IsAny<FundTransaction>(), It.IsAny<ClubFund>()))
                .Returns(Task.CompletedTask);

            Assert.True(await _service.TryCompleteOwnPendingContributionForDevelopmentAsync(uid, 2, 3));
        }

        #endregion

        #region GetFundCapabilitiesAsync

        [Fact]
        public async Task GetFundCapabilitiesAsync_ShouldReturnMinimal_WhenNotMember()
        {
            _memberRepo.Setup(r => r.GetMemberAsync(It.IsAny<Guid>(), 1)).ReturnsAsync((UserClubRole?)null);

            var dto = await _service.GetFundCapabilitiesAsync(Guid.NewGuid(), 1);
            Assert.False(dto.IsActiveClubMember);
            Assert.False(dto.CanViewFunds);
        }

        [Fact]
        public async Task GetFundCapabilitiesAsync_ShouldSetFlags_WhenActiveWithPolicies()
        {
            var uid = Guid.NewGuid();
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 1)).ReturnsAsync(ActiveManagerMember(1, 1));
            _policy.Setup(p => p.HasMemberPolicyInClubAsync(uid, 1, "viewfinance")).ReturnsAsync(true);
            _policy.Setup(p => p.HasMemberPolicyInClubAsync(uid, 1, "createfinance")).ReturnsAsync(true);
            _policy.Setup(p => p.HasMemberPolicyInClubAsync(uid, 1, "editfinance")).ReturnsAsync(true);

            var dto = await _service.GetFundCapabilitiesAsync(uid, 1);
            Assert.True(dto.CanViewFunds);
            Assert.True(dto.CanCreateFund);
            Assert.True(dto.CanApproveOrRejectFundEntity);
        }

        #endregion
    }
}
