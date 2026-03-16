using BusinessLogic.DTOs;
using BusinessLogic.Services.Implementation;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
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
            _clubFundService = new ClubFundService(_mockFundRepository.Object, _mockClubMemberRepository.Object, mockPayOSService.Object);
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

        #region GetFundsByClubIdAsync

        [Fact]
        public async Task GetFundsByClubIdAsync_ShouldReturnFunds_WhenClubHasFunds()
        {
            // Arrange
            var clubId = 5;
            var funds = new List<ClubFund>
            {
                new ClubFund { FundId = 1, ClubId = clubId, FundName = "Quỹ A", CurrentBalance = 1000, TotalAmount = 1000, Status = "APPROVED" },
                new ClubFund { FundId = 2, ClubId = clubId, FundName = "Quỹ B", CurrentBalance = 500, TotalAmount = 500, Status = "PENDING" }
            };
            _mockFundRepository.Setup(r => r.GetFundsByClubIdAsync(clubId)).ReturnsAsync(funds);

            // Act
            var result = (await _clubFundService.GetFundsByClubIdAsync(clubId)).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].FundId);
            Assert.Equal(clubId, result[0].ClubId);
            Assert.Equal("Quỹ A", result[0].FundName);
            Assert.Equal(1000, result[0].CurrentBalance);
            Assert.Equal("APPROVED", result[0].Status);
            Assert.Equal(2, result[1].FundId);
            Assert.Equal(500, result[1].CurrentBalance);
        }

        [Fact]
        public async Task GetFundsByClubIdAsync_ShouldReturnEmpty_WhenClubHasNoFunds()
        {
            // Arrange
            var clubId = 99;
            _mockFundRepository.Setup(r => r.GetFundsByClubIdAsync(clubId)).ReturnsAsync(new List<ClubFund>());

            // Act
            var result = (await _clubFundService.GetFundsByClubIdAsync(clubId)).ToList();

            // Assert
            Assert.Empty(result);
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

        #region CreateRequestAsync

        [Fact]
        public async Task CreateRequestAsync_ShouldThrowException_WhenFundNotFound()
        {
            // Arrange
            var request = new CreateFundRequestDto 
            { 
                FundId = 1, 
                TransactionType = "INCOME",
                Amount = 100 
            };
            _mockFundRepository.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync((ClubFund?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _clubFundService.CreateRequestAsync(Guid.NewGuid(), request));
            Assert.Equal("Quỹ không tồn tại.", ex.Message);
        }

        [Fact]
        public async Task CreateRequestAsync_ShouldReturnTransaction_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateFundRequestDto 
            { 
                FundId = 1, CategoryId = 2, TransactionType = "income", Amount = 1000, Description = "Test fund" 
            };
            var fund = new ClubFund { FundId = 1, ClubId = 10, Status = "APPROVED" };
            _mockFundRepository.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync(fund);
            _mockFundRepository.Setup(r => r.AddTransactionAsync(It.IsAny<FundTransaction>())).Returns(Task.CompletedTask);
            _mockClubMemberRepository.Setup(r => r.GetMemberAsync(userId, 10)).ReturnsAsync(CreateActiveMember("Club Member"));

            // Act
            var result = await _clubFundService.CreateRequestAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.FundId);
            Assert.Equal(2, result.CategoryId);
            Assert.Equal("INCOME", result.TransactionType);
            Assert.Equal(1000, result.Amount);
            Assert.Equal("PENDING", result.Status);
            Assert.Equal(userId, result.CreatedBy);
            _mockFundRepository.Verify(r => r.AddTransactionAsync(It.IsAny<FundTransaction>()), Times.Once);
        }

        [Fact]
        public async Task CreateRequestAsync_ShouldThrowUnauthorized_WhenUserNotMemberOfClub()
        {
            var userId = Guid.NewGuid();
            var request = new CreateFundRequestDto { FundId = 1, TransactionType = "INCOME", Amount = 100 };
            var fund = new ClubFund { FundId = 1, ClubId = 10, Status = "APPROVED" };
            _mockFundRepository.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync(fund);
            _mockClubMemberRepository.Setup(r => r.GetMemberAsync(userId, 10)).ReturnsAsync((UserClubRole?)null);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _clubFundService.CreateRequestAsync(userId, request));
            Assert.Contains("không phải thành viên", ex.Message);
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldThrowUnauthorized_WhenUserIsNotManagerOrViceManager()
        {
            var managerId = Guid.NewGuid();
            var request = new ProcessFundRequestDto { TransactionId = 1, Action = "APPROVE" };
            var fund = new ClubFund { ClubId = 5, CurrentBalance = 2000 };
            var transaction = new FundTransaction { TransactionId = 1, Status = "PENDING", TransactionType = "EXPENSE", Amount = 100, ClubFund = fund };
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(transaction);
            _mockClubMemberRepository.Setup(r => r.GetMemberAsync(managerId, 5)).ReturnsAsync(CreateActiveMember("Club Member"));

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _clubFundService.ProcessRequestAsync(managerId, request));
            Assert.Contains("Club Manager hoặc Vice Manager", ex.Message);
        }

        #endregion

        #region ProcessRequestAsync

        [Fact]
        public async Task ProcessRequestAsync_ShouldThrowException_WhenTransactionNotFound()
        {
            // Arrange
            var request = new ProcessFundRequestDto { TransactionId = 1 };
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync((FundTransaction?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _clubFundService.ProcessRequestAsync(Guid.NewGuid(), request));
            Assert.Equal("Giao dịch không tồn tại.", ex.Message);
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldThrowException_WhenTransactionNotPending()
        {
            // Arrange
            var request = new ProcessFundRequestDto { TransactionId = 1 };
            var transaction = new FundTransaction { TransactionId = 1, Status = "APPROVED" };
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(transaction);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _clubFundService.ProcessRequestAsync(Guid.NewGuid(), request));
            Assert.Equal("Giao dịch đã được xử lý trước đó.", ex.Message);
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldThrowException_WhenApproveExpenseAndInsufficientBalance()
        {
            // Arrange
            var managerId = Guid.NewGuid();
            var request = new ProcessFundRequestDto { TransactionId = 1, Action = "approve" };
            var fund = new ClubFund { ClubId = 5, CurrentBalance = 500 }; // Balance 500
            var transaction = new FundTransaction 
            { 
                TransactionId = 1, Status = "PENDING", TransactionType = "EXPENSE", Amount = 1000, ClubFund = fund 
            }; // Wants 1000
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(transaction);
            _mockClubMemberRepository.Setup(r => r.GetMemberAsync(managerId, 5))
                .ReturnsAsync(CreateActiveMemberWithLevel("Manager", 1));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _clubFundService.ProcessRequestAsync(managerId, request));
            Assert.Equal("Số dư quỹ không đủ để duyệt chi tiêu này.", ex.Message);
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldApproveExpenseAndDeductBalance_WhenValid()
        {
            // Arrange
            var managerId = Guid.NewGuid();
            var request = new ProcessFundRequestDto { TransactionId = 1, Action = "APPROVE" };
            var fund = new ClubFund { ClubId = 5, CurrentBalance = 1500 }; 
            var transaction = new FundTransaction 
            { 
                TransactionId = 1, Status = "PENDING", TransactionType = "EXPENSE", Amount = 1000, ClubFund = fund 
            };
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(transaction);
            _mockFundRepository.Setup(r => r.UpdateTransactionAndFundAsync(It.IsAny<FundTransaction>(), It.IsAny<ClubFund>())).Returns(Task.CompletedTask);
            _mockClubMemberRepository.Setup(r => r.GetMemberAsync(managerId, 5))
                .ReturnsAsync(CreateActiveMemberWithLevel("Vice Manager", 2));

            // Act
            var result = await _clubFundService.ProcessRequestAsync(managerId, request);

            // Assert
            Assert.True(result);
            Assert.Equal("APPROVED", transaction.Status);
            Assert.Equal(managerId, transaction.ApprovedBy);
            Assert.Equal(500, fund.CurrentBalance); // Deducted
            _mockFundRepository.Verify(r => r.UpdateTransactionAndFundAsync(transaction, fund), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldApproveIncomeAndAddBalance_WhenValid()
        {
            // Arrange
            var managerId = Guid.NewGuid();
            var request = new ProcessFundRequestDto { TransactionId = 1, Action = "approve" };
            var fund = new ClubFund { ClubId = 5, CurrentBalance = 500, TotalAmount = 1000 }; 
            var transaction = new FundTransaction 
            { 
                TransactionId = 1, Status = "PENDING", TransactionType = "INCOME", Amount = 1000, ClubFund = fund 
            };
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(transaction);
            _mockFundRepository.Setup(r => r.UpdateTransactionAndFundAsync(It.IsAny<FundTransaction>(), It.IsAny<ClubFund>())).Returns(Task.CompletedTask);
            _mockClubMemberRepository.Setup(r => r.GetMemberAsync(managerId, 5))
                .ReturnsAsync(CreateActiveMemberWithLevel("Manager", 1));

            // Act
            var result = await _clubFundService.ProcessRequestAsync(managerId, request);

            // Assert
            Assert.True(result);
            Assert.Equal("APPROVED", transaction.Status);
            Assert.Equal(managerId, transaction.ApprovedBy);
            Assert.Equal(1500, fund.CurrentBalance); // Added
            Assert.Equal(2000, fund.TotalAmount);    // Added
            _mockFundRepository.Verify(r => r.UpdateTransactionAndFundAsync(transaction, fund), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldRejectAndNotChangeBalance_WhenRejectAction()
        {
            // Arrange
            var managerId = Guid.NewGuid();
            var request = new ProcessFundRequestDto { TransactionId = 1, Action = "REJECT" };
            var fund = new ClubFund { ClubId = 5, CurrentBalance = 1500 }; 
            var transaction = new FundTransaction 
            { 
                TransactionId = 1, Status = "PENDING", TransactionType = "EXPENSE", Amount = 1000, ClubFund = fund 
            };
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(transaction);
            _mockFundRepository.Setup(r => r.UpdateTransactionAndFundAsync(It.IsAny<FundTransaction>(), It.IsAny<ClubFund>())).Returns(Task.CompletedTask);
            _mockClubMemberRepository.Setup(r => r.GetMemberAsync(managerId, 5))
                .ReturnsAsync(CreateActiveMemberWithLevel("Manager", 1));

            // Act
            var result = await _clubFundService.ProcessRequestAsync(managerId, request);

            // Assert
            Assert.True(result);
            Assert.Equal("REJECTED", transaction.Status);
            Assert.Equal(managerId, transaction.ApprovedBy);
            Assert.Equal(1500, fund.CurrentBalance); // Unchanged
            _mockFundRepository.Verify(r => r.UpdateTransactionAndFundAsync(transaction, fund), Times.Once);
        }

        #endregion

        #region GetFundHistoryAsync

        [Fact]
        public async Task GetFundHistoryAsync_ShouldReturnTransactionDtos()
        {
            // Arrange
            var transactions = new List<FundTransaction> { new FundTransaction { TransactionId = 1, Amount = 100 } };
            _mockFundRepository.Setup(r => r.GetTransactionsByFundIdAsync(1, "PENDING")).ReturnsAsync(transactions);

            // Act
            var result = (await _clubFundService.GetFundHistoryAsync(1, "pending")).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].TransactionId);
            Assert.Equal(100, result[0].Amount);
        }

        [Fact]
        public async Task GetFundHistoryAsync_ShouldHandleNullStatus()
        {
            // Arrange
            var transactions = new List<FundTransaction> { new FundTransaction() };
            _mockFundRepository.Setup(r => r.GetTransactionsByFundIdAsync(1, null)).ReturnsAsync(transactions);

            // Act
            var result = (await _clubFundService.GetFundHistoryAsync(1, null)).ToList();

            // Assert
            Assert.Single(result);
        }

        #endregion
    }
}
