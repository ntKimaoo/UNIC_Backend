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
using System.Text.Json;
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
            _fundRepo.Setup(r => r.ExistsNonRejectedFundNameInClubAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(false);

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
        public async Task CreateFundAsync_ShouldThrow_WhenFundNameAlreadyExistsInClub()
        {
            var uid = Guid.NewGuid();
            _fundRepo.Setup(r => r.ExistsNonRejectedFundNameInClubAsync(1, "Q2")).ReturnsAsync(true);
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 1)).ReturnsAsync(ActiveManagerMember(1, level: 2));

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateFundAsync(uid, new CreateFundDto
                {
                    ClubId = 1,
                    FundName = "Q2"
                }));

            Assert.Contains("Tên quỹ đã tồn tại", ex.Message);
        }

        [Fact]
        public async Task CreateFundAsync_ShouldThrow_WhenExpiresAtInPast()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateFundAsync(Guid.NewGuid(), new CreateFundDto
                {
                    ClubId = 1,
                    FundName = "Q",
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
                Description = "  Mo ta quy  "
            });

            Assert.NotNull(captured);
            Assert.Equal("APPROVED", captured!.Status);
            Assert.Equal(0, captured.CurrentBalance);
            Assert.Equal(0, captured.TotalAmount);
            Assert.Equal("Quỹ A", result.FundName);
            Assert.Equal("Mo ta quy", captured.Description);
            Assert.Equal("Mo ta quy", result.Description);
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

            await _service.CreateFundAsync(uid, new CreateFundDto { ClubId = 1, FundName = "Q" });

            Assert.Equal("PENDING", captured!.Status);
        }

        [Fact]
        public async Task CreateFundAsync_ShouldMapDescription_FromAliasKey()
        {
            var uid = Guid.NewGuid();
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 1)).ReturnsAsync(ActiveManagerMember(1, level: 1));

            ClubFund? captured = null;
            _fundRepo.Setup(r => r.AddFundAsync(It.IsAny<ClubFund>()))
                .Callback<ClubFund>(f => captured = f)
                .ReturnsAsync((ClubFund f) => f);

            var aliasJson = JsonDocument.Parse("\"Mo ta tu alias\"").RootElement.Clone();
            var dto = new CreateFundDto
            {
                ClubId = 1,
                FundName = "Q Alias",
                ExtraData = new Dictionary<string, JsonElement>
                {
                    ["fundDescription"] = aliasJson
                }
            };

            var result = await _service.CreateFundAsync(uid, dto);

            Assert.NotNull(captured);
            Assert.Equal("Mo ta tu alias", captured!.Description);
            Assert.Equal("Mo ta tu alias", result.Description);
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

        [Fact]
        public async Task CreateContributionAsync_ShouldThrow_WhenCategoryNotFound()
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
            _fundRepo.Setup(r => r.GetFundCategoryByIdAsync(99)).ReturnsAsync((FundCategory?)null);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateContributionAsync(uid, new ContributeRequestDto
                {
                    FundId = 1,
                    CategoryId = 99,
                    Amount = 5000
                }, CancellationToken.None));
        }

        [Fact]
        public async Task CreateContributionAsync_ShouldThrow_WhenCategoryBelongsToOtherClub()
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
            _fundRepo.Setup(r => r.GetFundCategoryByIdAsync(5)).ReturnsAsync(new FundCategory
            {
                CategoryId = 5,
                ClubId = 99,
                CategoryName = "X",
                Description = ""
            });

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateContributionAsync(uid, new ContributeRequestDto
                {
                    FundId = 1,
                    CategoryId = 5,
                    Amount = 5000
                }, CancellationToken.None));
        }

        [Fact]
        public async Task CreateContributionAsync_ShouldCallPayOS_WhenCategoryGlobal()
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
            _fundRepo.Setup(r => r.GetFundCategoryByIdAsync(5)).ReturnsAsync(new FundCategory
            {
                CategoryId = 5,
                ClubId = null,
                CategoryName = "Du Lịch",
                Description = ""
            });
            _fundRepo.Setup(r => r.AddTransactionAsync(It.IsAny<FundTransaction>()))
                .Callback<FundTransaction>(t => t.TransactionId = 200)
                .Returns(Task.CompletedTask);
            _payOS.Setup(p => p.CreatePaymentLinkAsync(200, 5000m, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PayOSPaymentLinkResult { CheckoutUrl = "u", QrCode = "q", PaymentLinkId = "p" });
            _fundRepo.Setup(r => r.UpdateTransactionAsync(It.IsAny<FundTransaction>())).Returns(Task.CompletedTask);

            await _service.CreateContributionAsync(uid, new ContributeRequestDto
            {
                FundId = 1,
                CategoryId = 5,
                Amount = 5000
            }, CancellationToken.None);

            _fundRepo.Verify(r => r.GetFundCategoryByIdAsync(5), Times.Once);
        }

        [Fact]
        public async Task CreateContributionAsync_ShouldDeleteTransaction_WhenPayOSFails()
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
                .Callback<FundTransaction>(t => t.TransactionId = 300)
                .Returns(Task.CompletedTask);
            _payOS.Setup(p => p.CreatePaymentLinkAsync(300, It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("payos down"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateContributionAsync(uid, new ContributeRequestDto
                {
                    FundId = 1,
                    Amount = 5000
                }, CancellationToken.None));

            _fundRepo.Verify(r => r.DeleteTransactionByIdAsync(300), Times.Once);
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
            var uid = Guid.NewGuid();
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 2)).ReturnsAsync(ActiveManagerMember(2, level: 1));
            _policy.Setup(p => p.HasMemberPolicyInClubAsync(uid, 2, "editfinance")).ReturnsAsync(true);

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
            _fundRepo.Setup(r => r.GetFundsByClubIdPagedAsync(2, null, null, "NEWEST", 1, 10)).ReturnsAsync((list, 1));

            var page = await _service.GetFundsByClubIdPagedAsync(2, uid, false, null, null, null, 1, 10);
            Assert.Single(page.Items);
            Assert.Equal(1, page.TotalCount);
        }

        [Fact]
        public async Task GetFundsByClubIdPagedAsync_ShouldThrow_WhenStatusInvalid()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetFundsByClubIdPagedAsync(2, Guid.NewGuid(), true, "INVALID", null, null, 1, 10));
        }

        [Fact]
        public async Task GetFundsByClubIdPagedAsync_ShouldThrow_WhenSortInvalid()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetFundsByClubIdPagedAsync(2, Guid.NewGuid(), true, null, null, "RANDOM", 1, 10));
        }

        [Fact]
        public async Task GetFundsByClubIdPagedAsync_ShouldForceApproved_WhenMemberCannotManageWorkflow_EvenIfQueryPending()
        {
            var uid = Guid.NewGuid();
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 2)).ReturnsAsync(ActiveManagerMember(2, level: 3));
            _policy.Setup(p => p.HasMemberPolicyInClubAsync(uid, 2, "editfinance")).ReturnsAsync(true);
            _fundRepo.Setup(r => r.GetFundsByClubIdPagedAsync(2, "APPROVED", null, "NEWEST", 1, 10))
                .ReturnsAsync((Enumerable.Empty<ClubFund>(), 0));

            await _service.GetFundsByClubIdPagedAsync(2, uid, false, "PENDING", null, null, 1, 10);

            _fundRepo.Verify(r => r.GetFundsByClubIdPagedAsync(2, "APPROVED", null, "NEWEST", 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetFundsByClubIdPagedAsync_ShouldUsePending_WhenTopManagerRequestsPending()
        {
            var uid = Guid.NewGuid();
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 2)).ReturnsAsync(ActiveManagerMember(2, level: 1));
            _policy.Setup(p => p.HasMemberPolicyInClubAsync(uid, 2, "editfinance")).ReturnsAsync(true);
            _fundRepo.Setup(r => r.GetFundsByClubIdPagedAsync(2, "PENDING", null, "NEWEST", 1, 10))
                .ReturnsAsync((Enumerable.Empty<ClubFund>(), 0));

            await _service.GetFundsByClubIdPagedAsync(2, uid, false, "PENDING", null, null, 1, 10);

            _fundRepo.Verify(r => r.GetFundsByClubIdPagedAsync(2, "PENDING", null, "NEWEST", 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetFundsByClubIdPagedAsync_ShouldBypassMemberGate_WhenSystemAdmin()
        {
            var uid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetFundsByClubIdPagedAsync(2, "REJECTED", null, "NEWEST", 1, 10))
                .ReturnsAsync((Enumerable.Empty<ClubFund>(), 0));

            await _service.GetFundsByClubIdPagedAsync(2, uid, true, "REJECTED", null, null, 1, 10);

            _memberRepo.Verify(r => r.GetMemberAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
            _fundRepo.Verify(r => r.GetFundsByClubIdPagedAsync(2, "REJECTED", null, "NEWEST", 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetMyFundsByClubIdPagedAsync_ShouldPassNormalizedFilters_ToRepository()
        {
            var uid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetMyFundsByClubIdPagedAsync(
                    2, uid, "CREATED", null, null, "NEWEST", 1, 10))
                .ReturnsAsync((Enumerable.Empty<ClubFund>(), 0));

            await _service.GetMyFundsByClubIdPagedAsync(2, uid, null, "ALL", null, null, 1, 10);

            _fundRepo.Verify(r => r.GetMyFundsByClubIdPagedAsync(
                2, uid, "CREATED", null, null, "NEWEST", 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetMyFundsByClubIdPagedAsync_ShouldPassAllMineType_WhenExplicitAll()
        {
            var uid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetMyFundsByClubIdPagedAsync(
                    2, uid, "ALL", null, null, "NEWEST", 1, 10))
                .ReturnsAsync((Enumerable.Empty<ClubFund>(), 0));

            await _service.GetMyFundsByClubIdPagedAsync(2, uid, "ALL", null, null, null, 1, 10);

            _fundRepo.Verify(r => r.GetMyFundsByClubIdPagedAsync(
                2, uid, "ALL", null, null, "NEWEST", 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetMyFundsByClubIdPagedAsync_ShouldThrow_WhenMineTypeInvalid()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetMyFundsByClubIdPagedAsync(2, Guid.NewGuid(), "INVALID", null, null, null, 1, 10));
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

            var ok = await _service.ApproveFundAsync(mid,
                new ApproveFundDto { FundId = 1, Action = "REJECT", RejectReason = "Lý do từ chối đủ dài để hợp lệ." });
            Assert.True(ok);
            _fundRepo.Verify(r => r.UpdateClubFundAsync(It.Is<ClubFund>(f =>
                f.Status == "REJECTED"
                && f.RejectReason == "Lý do từ chối đủ dài để hợp lệ."
                && f.RejectedAt.HasValue)), Times.Once);
        }

        [Fact]
        public async Task ApproveFundAsync_ShouldThrow_WhenRejectWithoutReason()
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

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ApproveFundAsync(mid, new ApproveFundDto { FundId = 1, Action = "REJECT" }));
            Assert.Contains("lý do", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ApproveFundAsync_ShouldThrow_WhenRejectReasonTooShort()
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

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ApproveFundAsync(mid, new ApproveFundDto { FundId = 1, Action = "REJECT", RejectReason = "abc" }));
        }

        #endregion

        #region ProcessPayOSPaymentSuccessAsync / TryCompleteOwnPending

        [Fact]
        public async Task ProcessPayOSPaymentSuccessAsync_ShouldReturnFalse_WhenTransactionInvalid()
        {
            _fundRepo.Setup(r => r.TryApproveMemberContributionAsync(1)).ReturnsAsync(false);
            Assert.False(await _service.ProcessPayOSPaymentSuccessAsync(1));
        }

        [Fact]
        public async Task ProcessPayOSPaymentSuccessAsync_ShouldReturnTrue_WhenRepositoryApproves()
        {
            _fundRepo.Setup(r => r.TryApproveMemberContributionAsync(9)).ReturnsAsync(true);

            var ok = await _service.ProcessPayOSPaymentSuccessAsync(9);
            Assert.True(ok);
            _fundRepo.Verify(r => r.TryApproveMemberContributionAsync(9), Times.Once);
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
            _fundRepo.Setup(r => r.TryApproveMemberContributionAsync(3)).ReturnsAsync(true);

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
            Assert.Equal(5, dto.MenuItems.Count);
            Assert.Equal(new[] { "overview", "my-funds", "transactions", "reports", "settings" }, dto.MenuItems.Select(m => m.Id).ToArray());
            Assert.All(dto.MenuItems, m => Assert.True(m.Visible));
        }

        [Fact]
        public async Task GetFundCapabilitiesAsync_ShouldReturnEmptyMenu_WhenCannotViewFunds()
        {
            var uid = Guid.NewGuid();
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 1)).ReturnsAsync(ActiveManagerMember(1, 1));
            _policy.Setup(p => p.HasMemberPolicyInClubAsync(uid, 1, "viewfinance")).ReturnsAsync(false);
            _policy.Setup(p => p.HasMemberPolicyInClubAsync(uid, 1, "createfinance")).ReturnsAsync(false);
            _policy.Setup(p => p.HasMemberPolicyInClubAsync(uid, 1, "editfinance")).ReturnsAsync(false);

            var dto = await _service.GetFundCapabilitiesAsync(uid, 1);
            Assert.False(dto.CanViewFunds);
            Assert.Empty(dto.MenuItems);
        }

        [Fact]
        public async Task GetClubFundReportSummaryAsync_ShouldMapAggregates_FromRepository()
        {
            _fundRepo.Setup(r => r.GetClubFundReportAggregatesAsync(3, null, null))
                .ReturnsAsync((1, 4, 2, 100m, 500m, 200m));

            var dto = await _service.GetClubFundReportSummaryAsync(3, null, null);

            Assert.Equal(3, dto.ClubId);
            Assert.Equal(1, dto.PendingFundCount);
            Assert.Equal(4, dto.ApprovedFundCount);
            Assert.Equal(2, dto.RejectedFundCount);
            Assert.Equal(100m, dto.TotalBalanceApprovedFunds);
            Assert.Equal(500m, dto.TotalApprovedIncome);
            Assert.Equal(200m, dto.TotalApprovedExpense);
        }

        [Fact]
        public async Task GetClubFundTransactionsPagedAsync_PassesApprovedAndAllTypes_ToRepository()
        {
            var uid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetTransactionsByClubIdPagedAsync(
                    1, null, "APPROVED", false, null, null, null, 1, 20))
                .ReturnsAsync((Array.Empty<FundTransaction>(), 0));

            await _service.GetClubFundTransactionsPagedAsync(1, null, null, null, uid, null, null, 1, 20);

            _fundRepo.Verify(r => r.GetTransactionsByClubIdPagedAsync(
                1, null, "APPROVED", false, null, null, null, 1, 20), Times.Once);
        }

        [Fact]
        public async Task GetClubFundTransactionsPagedAsync_ScopeMine_PassesUserId()
        {
            var uid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetTransactionsByClubIdPagedAsync(
                    1, 2, "APPROVED", false, uid, null, null, 1, 10))
                .ReturnsAsync((Array.Empty<FundTransaction>(), 0));

            await _service.GetClubFundTransactionsPagedAsync(1, 2, null, "mine", uid, null, null, 1, 10);

            _fundRepo.Verify(r => r.GetTransactionsByClubIdPagedAsync(
                1, 2, "APPROVED", false, uid, null, null, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetClubFundTransactionsPagedAsync_StatusAll_PassesNullStatus()
        {
            var uid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetTransactionsByClubIdPagedAsync(
                    1, null, null, false, null, null, null, 1, 10))
                .ReturnsAsync((Array.Empty<FundTransaction>(), 0));

            await _service.GetClubFundTransactionsPagedAsync(1, null, "ALL", null, uid, null, null, 1, 10);

            _fundRepo.Verify(r => r.GetTransactionsByClubIdPagedAsync(
                1, null, null, false, null, null, null, 1, 10), Times.Once);
        }

        [Fact]
        public async Task CreateFundAsync_ShouldPersistExpiresAt_WhenDateIsInFuture()
        {
            var uid = Guid.NewGuid();
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 1)).ReturnsAsync(ActiveManagerMember(1, level: 1));
            ClubFund? captured = null;
            _fundRepo.Setup(r => r.AddFundAsync(It.IsAny<ClubFund>()))
                .Callback<ClubFund>(f => captured = f)
                .ReturnsAsync((ClubFund f) => f);

            var future = DateTime.UtcNow.Date.AddDays(30);
            await _service.CreateFundAsync(uid, new CreateFundDto
            {
                ClubId = 1,
                FundName = "QExp",
                ExpiresAt = future
            });

            Assert.NotNull(captured);
            Assert.Equal(future, captured!.ExpiresAt!.Value.Date);
        }

        private static FundTransaction RichTransaction(int id)
        {
            return new FundTransaction
            {
                TransactionId = id,
                FundId = 5,
                CategoryId = 2,
                TransactionType = "INCOME",
                Status = "APPROVED",
                Amount = 100m,
                Description = "D",
                TransactionDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = Guid.NewGuid(),
                IsMemberContribution = true,
                PaymentLinkId = "pl",
                CreatedAt = new DateTime(2024, 6, 2, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 6, 3, 0, 0, 0, DateTimeKind.Utc),
                ClubFund = new ClubFund { FundId = 5, FundName = "  Quỹ X  " },
                FundCategory = new FundCategory
                {
                    CategoryId = 2,
                    CategoryName = "  Cat  ",
                    Description = "d",
                    ClubId = 1
                },
                Creator = new User { FullName = "  Nguyen Van A  " }
            };
        }

        [Fact]
        public async Task GetFundHistoryPagedAsync_ShouldMapTransactionDtos_WhenRepositoryReturnsRows()
        {
            var list = new List<FundTransaction> { RichTransaction(1) };
            _fundRepo.Setup(r => r.GetTransactionsByFundIdPagedAsync(10, "APPROVED", true, null, 1, 5))
                .ReturnsAsync((list, 1));

            var page = await _service.GetFundHistoryPagedAsync(10, "", null, null, 1, 5);

            Assert.Single(page.Items);
            var item = page.Items.First();
            Assert.Equal(1, item.TransactionId);
            Assert.Equal("Quỹ X", item.FundName);
            Assert.Equal("Cat", item.CategoryName);
            Assert.Equal("Nguyen Van A", item.MemberName);
            Assert.Equal(new DateTime(2024, 6, 2, 0, 0, 0, DateTimeKind.Utc), item.CreatedAt);
        }

        [Fact]
        public async Task GetFundHistoryPagedAsync_ShouldMapSparseTransaction_WhenNavigationsMinimal()
        {
            var tx = new FundTransaction
            {
                TransactionId = 2,
                FundId = 1,
                TransactionType = "INCOME",
                Status = null,
                Amount = 1,
                TransactionDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = default,
                UpdatedAt = default,
                ClubFund = new ClubFund { FundName = "   " },
                Creator = null!,
                FundCategory = null!
            };
            _fundRepo.Setup(r => r.GetTransactionsByFundIdPagedAsync(10, "APPROVED", true, null, 1, 5))
                .ReturnsAsync((new List<FundTransaction> { tx }, 1));

            var page = await _service.GetFundHistoryPagedAsync(10, null, null, null, 1, 5);
            var item = page.Items.First();
            Assert.Null(item.FundName);
            Assert.Null(item.CategoryName);
            Assert.Null(item.MemberName);
            Assert.Equal("PENDING", item.Status);
        }

        [Fact]
        public async Task GetClubFundTransactionsPagedAsync_ShouldMapTransactions_WhenItemsReturned()
        {
            var uid = Guid.NewGuid();
            var list = new List<FundTransaction> { RichTransaction(7) };
            _fundRepo.Setup(r => r.GetTransactionsByClubIdPagedAsync(
                    3, null, "APPROVED", false, null, null, null, 1, 10))
                .ReturnsAsync((list, 1));

            var page = await _service.GetClubFundTransactionsPagedAsync(3, null, null, null, uid, null, null, 1, 10);

            Assert.Single(page.Items);
            Assert.Equal(7, page.Items.First().TransactionId);
        }

        [Fact]
        public async Task GetFundCategoriesForClubAsync_ShouldMap_FromRepository()
        {
            _fundRepo.Setup(r => r.GetFundCategoriesForClubAsync(8)).ReturnsAsync(new List<FundCategory>
            {
                new FundCategory { CategoryId = 1, CategoryName = "A", Description = "d", ClubId = 8 }
            });

            var list = await _service.GetFundCategoriesForClubAsync(8);

            Assert.Single(list);
            Assert.Equal(1, list[0].CategoryId);
            Assert.Equal("A", list[0].CategoryName);
            Assert.Equal(8, list[0].ClubId);
        }

        [Fact]
        public async Task GetContributionPaymentStatusByOrderCodeAsync_ShouldReturnNull_WhenClubFundMissing()
        {
            var uid = Guid.NewGuid();
            var t = new FundTransaction
            {
                TransactionId = 5,
                IsMemberContribution = true,
                TransactionType = "INCOME",
                CreatedBy = uid,
                ClubFund = null!
            };
            _fundRepo.Setup(r => r.GetTransactionByIdAsync(5)).ReturnsAsync(t);

            Assert.Null(await _service.GetContributionPaymentStatusByOrderCodeAsync(uid, 5));
        }

        [Fact]
        public async Task GetContributionPaymentStatusAsync_ShouldReturnNull_WhenClubMismatch()
        {
            var uid = Guid.NewGuid();
            var t = new FundTransaction
            {
                TransactionId = 1,
                IsMemberContribution = true,
                TransactionType = "INCOME",
                CreatedBy = uid,
                ClubFund = new ClubFund { ClubId = 99 }
            };
            _fundRepo.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(t);

            Assert.Null(await _service.GetContributionPaymentStatusAsync(uid, 1, 1));
        }

        [Fact]
        public async Task GetContributionPaymentStatusAsync_ShouldReturnNull_WhenNotIncome()
        {
            var uid = Guid.NewGuid();
            var t = new FundTransaction
            {
                TransactionId = 1,
                IsMemberContribution = true,
                TransactionType = "EXPENSE",
                CreatedBy = uid,
                ClubFund = new ClubFund { ClubId = 1 }
            };
            _fundRepo.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(t);

            Assert.Null(await _service.GetContributionPaymentStatusAsync(uid, 1, 1));
        }

        [Fact]
        public async Task GetContributionPaymentStatusAsync_ShouldMarkLinkExpired_WhenPastExpiration()
        {
            var uid = Guid.NewGuid();
            var t = new FundTransaction
            {
                TransactionId = 1,
                FundId = 1,
                IsMemberContribution = true,
                TransactionType = "INCOME",
                Status = "PENDING",
                CreatedBy = uid,
                TransactionDate = DateTime.UtcNow.AddHours(-3),
                Amount = 100,
                ClubFund = new ClubFund { ClubId = 1 }
            };
            _fundRepo.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(t);

            var dto = await _service.GetContributionPaymentStatusAsync(uid, 1, 1);
            Assert.NotNull(dto);
            Assert.True(dto!.IsPaymentLinkExpired);
            Assert.Contains("hết hạn", dto.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetContributionPaymentStatusAsync_ShouldShowWaiting_WhenPendingAndNotExpired()
        {
            var uid = Guid.NewGuid();
            var t = new FundTransaction
            {
                TransactionId = 1,
                FundId = 1,
                IsMemberContribution = true,
                TransactionType = "INCOME",
                Status = "PENDING",
                CreatedBy = uid,
                TransactionDate = DateTime.UtcNow,
                Amount = 100,
                ClubFund = new ClubFund { ClubId = 1 }
            };
            _fundRepo.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(t);

            var dto = await _service.GetContributionPaymentStatusAsync(uid, 1, 1);
            Assert.NotNull(dto);
            Assert.False(dto!.IsPaymentLinkExpired);
            Assert.Contains("chờ", dto.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetFundCapabilitiesAsync_ShouldSetInactiveHint_WhenMemberNotActive()
        {
            var uid = Guid.NewGuid();
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 1)).ReturnsAsync(new UserClubRole
            {
                Status = "LEFT",
                ClubRole = new ClubRole { Level = 1 }
            });

            var dto = await _service.GetFundCapabilitiesAsync(uid, 1);
            Assert.False(dto.IsActiveClubMember);
            Assert.Contains("hoạt động", dto.FinanceAccessHintVi ?? "", StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetFundCapabilitiesAsync_ShouldHintEditFinance_WhenManagerWithoutEdit()
        {
            var uid = Guid.NewGuid();
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 1)).ReturnsAsync(ActiveManagerMember(1, 1));
            _policy.Setup(p => p.HasMemberPolicyInClubAsync(uid, 1, "viewfinance")).ReturnsAsync(true);
            _policy.Setup(p => p.HasMemberPolicyInClubAsync(uid, 1, "createfinance")).ReturnsAsync(true);
            _policy.Setup(p => p.HasMemberPolicyInClubAsync(uid, 1, "editfinance")).ReturnsAsync(false);

            var dto = await _service.GetFundCapabilitiesAsync(uid, 1);
            Assert.Contains("duyệt quỹ", dto.FinanceAccessHintVi ?? "", StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetFundCapabilitiesAsync_ShouldHintCreateFinance_WhenViceWithoutCreate()
        {
            var uid = Guid.NewGuid();
            _memberRepo.Setup(r => r.GetMemberAsync(uid, 1)).ReturnsAsync(ActiveManagerMember(1, 2));
            _policy.Setup(p => p.HasMemberPolicyInClubAsync(uid, 1, "viewfinance")).ReturnsAsync(true);
            _policy.Setup(p => p.HasMemberPolicyInClubAsync(uid, 1, "createfinance")).ReturnsAsync(false);
            _policy.Setup(p => p.HasMemberPolicyInClubAsync(uid, 1, "editfinance")).ReturnsAsync(false);

            var dto = await _service.GetFundCapabilitiesAsync(uid, 1);
            Assert.Contains("tạo quỹ", dto.FinanceAccessHintVi ?? "", StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ApproveFundAsync_ShouldThrow_WhenActionInvalid()
        {
            var mid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync(new ClubFund
            {
                FundId = 1,
                ClubId = 2,
                Status = "PENDING",
                FundName = "F"
            });
            _memberRepo.Setup(r => r.GetMemberAsync(mid, 2)).ReturnsAsync(ActiveManagerMember(2, 1));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ApproveFundAsync(mid, new ApproveFundDto { FundId = 1, Action = "HOLD" }));
        }

        [Fact]
        public async Task ApproveFundAsync_ShouldThrow_WhenRejectReasonTooLong()
        {
            var mid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync(new ClubFund
            {
                FundId = 1,
                ClubId = 2,
                Status = "PENDING",
                FundName = "F"
            });
            _memberRepo.Setup(r => r.GetMemberAsync(mid, 2)).ReturnsAsync(ActiveManagerMember(2, 1));

            var longReason = new string('x', 2001);
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ApproveFundAsync(mid, new ApproveFundDto { FundId = 1, Action = "REJECT", RejectReason = longReason }));
        }

        [Fact]
        public async Task ApproveFundAsync_ShouldThrow_WhenMemberInactive()
        {
            var mid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync(new ClubFund
            {
                FundId = 1,
                ClubId = 2,
                Status = "PENDING",
                FundName = "F"
            });
            _memberRepo.Setup(r => r.GetMemberAsync(mid, 2)).ReturnsAsync(new UserClubRole
            {
                Status = "LEFT",
                ClubRole = new ClubRole { Level = 1 }
            });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.ApproveFundAsync(mid, new ApproveFundDto { FundId = 1, Action = "APPROVE" }));
        }

        [Fact]
        public async Task ApproveFundAsync_ShouldThrow_WhenFundAlreadyRejected()
        {
            _fundRepo.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync(new ClubFund
            {
                FundId = 1,
                ClubId = 1,
                Status = "REJECTED",
                FundName = "F"
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ApproveFundAsync(Guid.NewGuid(), new ApproveFundDto { FundId = 1, Action = "APPROVE" }));
        }

        [Fact]
        public async Task ApproveFundAsync_ShouldThrow_WhenFundStatusUnexpected()
        {
            var mid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync(new ClubFund
            {
                FundId = 1,
                ClubId = 1,
                Status = "ARCHIVED",
                FundName = "F"
            });
            _memberRepo.Setup(r => r.GetMemberAsync(mid, 1)).ReturnsAsync(ActiveManagerMember(1, 1));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ApproveFundAsync(mid, new ApproveFundDto { FundId = 1, Action = "APPROVE" }));
        }

        [Fact]
        public async Task TryCompleteOwnPendingContributionForDevelopmentAsync_ShouldReturnFalse_WhenWrongUser()
        {
            var uid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(new FundTransaction
            {
                TransactionId = 1,
                CreatedBy = Guid.NewGuid(),
                ClubFund = new ClubFund { ClubId = 1 },
                IsMemberContribution = true,
                TransactionType = "INCOME",
                Status = "PENDING"
            });

            Assert.False(await _service.TryCompleteOwnPendingContributionForDevelopmentAsync(uid, 1, 1));
        }

        [Fact]
        public async Task TryCompleteOwnPendingContributionForDevelopmentAsync_ShouldReturnFalse_WhenNotPending()
        {
            var uid = Guid.NewGuid();
            _fundRepo.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(new FundTransaction
            {
                TransactionId = 1,
                CreatedBy = uid,
                ClubFund = new ClubFund { ClubId = 1 },
                IsMemberContribution = true,
                TransactionType = "INCOME",
                Status = "APPROVED"
            });

            Assert.False(await _service.TryCompleteOwnPendingContributionForDevelopmentAsync(uid, 1, 1));
        }

        #endregion
    }
}
