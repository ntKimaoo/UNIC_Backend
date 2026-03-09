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

namespace UNIC.BusinessLogic.Test.Services
{
    public class ClubFundServiceTest
    {
        private readonly Mock<IFundRepository> _mockFundRepository;
        private readonly ClubFundService _clubFundService;

        public ClubFundServiceTest()
        {
            _mockFundRepository = new Mock<IFundRepository>();
            _clubFundService = new ClubFundService(_mockFundRepository.Object);
        }

        #region CreateRequestAsync

        [Fact]
        public async Task CreateRequestAsync_ShouldThrowException_WhenFundNotFound()
        {
            // Arrange
            var request = new CreateFundRequestDto { FundId = 1 };
            _mockFundRepository.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync((ClubFund?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _clubFundService.CreateRequestAsync(Guid.NewGuid(), request));
            Assert.Equal("Quỹ không tồn tại", ex.Message);
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
            var fund = new ClubFund { FundId = 1 };
            
            _mockFundRepository.Setup(r => r.GetFundByIdAsync(1)).ReturnsAsync(fund);
            _mockFundRepository.Setup(r => r.AddTransactionAsync(It.IsAny<FundTransaction>())).Returns(Task.CompletedTask);

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

        #endregion

        #region ProcessRequestAsync

        [Fact]
        public async Task ProcessRequestAsync_ShouldThrowException_WhenTransactionNotFound()
        {
            // Arrange
            var request = new ProcessFundRequestDto { TransactionId = 1 };
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync((FundTransaction?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _clubFundService.ProcessRequestAsync(Guid.NewGuid(), request));
            Assert.Equal("Giao dịch không tồn tại", ex.Message);
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldThrowException_WhenTransactionNotPending()
        {
            // Arrange
            var request = new ProcessFundRequestDto { TransactionId = 1 };
            var transaction = new FundTransaction { TransactionId = 1, Status = "APPROVED" };
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(transaction);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _clubFundService.ProcessRequestAsync(Guid.NewGuid(), request));
            Assert.Equal("Giao dịch đã được xử lý trước đó", ex.Message);
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldThrowException_WhenApproveExpenseAndInsufficientBalance()
        {
            // Arrange
            var request = new ProcessFundRequestDto { TransactionId = 1, Action = "approve" };
            var fund = new ClubFund { CurrentBalance = 500 }; // Balance 500
            var transaction = new FundTransaction 
            { 
                TransactionId = 1, Status = "PENDING", TransactionType = "EXPENSE", Amount = 1000, ClubFund = fund 
            }; // Wants 1000
            
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(transaction);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _clubFundService.ProcessRequestAsync(Guid.NewGuid(), request));
            Assert.Equal("Số dư quỹ không đủ để duyệt chi tiêu này", ex.Message);
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldApproveExpenseAndDeductBalance_WhenValid()
        {
            // Arrange
            var managerId = Guid.NewGuid();
            var request = new ProcessFundRequestDto { TransactionId = 1, Action = "APPROVE" };
            var fund = new ClubFund { CurrentBalance = 1500 }; 
            var transaction = new FundTransaction 
            { 
                TransactionId = 1, Status = "PENDING", TransactionType = "EXPENSE", Amount = 1000, ClubFund = fund 
            };
            
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(transaction);
            _mockFundRepository.Setup(r => r.UpdateClubFundAsync(fund)).Returns(Task.CompletedTask);
            _mockFundRepository.Setup(r => r.UpdateTransactionAsync(transaction)).Returns(Task.CompletedTask);

            // Act
            var result = await _clubFundService.ProcessRequestAsync(managerId, request);

            // Assert
            Assert.True(result);
            Assert.Equal("APPROVED", transaction.Status);
            Assert.Equal(managerId, transaction.ApprovedBy);
            Assert.Equal(500, fund.CurrentBalance); // Deducted
            _mockFundRepository.Verify(r => r.UpdateClubFundAsync(fund), Times.Once);
            _mockFundRepository.Verify(r => r.UpdateTransactionAsync(transaction), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldApproveIncomeAndAddBalance_WhenValid()
        {
            // Arrange
            var managerId = Guid.NewGuid();
            var request = new ProcessFundRequestDto { TransactionId = 1, Action = "approve" };
            var fund = new ClubFund { CurrentBalance = 500, TotalAmount = 1000 }; 
            var transaction = new FundTransaction 
            { 
                TransactionId = 1, Status = "PENDING", TransactionType = "INCOME", Amount = 1000, ClubFund = fund 
            };
            
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(transaction);
            _mockFundRepository.Setup(r => r.UpdateClubFundAsync(fund)).Returns(Task.CompletedTask);
            _mockFundRepository.Setup(r => r.UpdateTransactionAsync(transaction)).Returns(Task.CompletedTask);

            // Act
            var result = await _clubFundService.ProcessRequestAsync(managerId, request);

            // Assert
            Assert.True(result);
            Assert.Equal("APPROVED", transaction.Status);
            Assert.Equal(managerId, transaction.ApprovedBy);
            Assert.Equal(1500, fund.CurrentBalance); // Added
            Assert.Equal(2000, fund.TotalAmount);    // Added
            _mockFundRepository.Verify(r => r.UpdateClubFundAsync(fund), Times.Once);
            _mockFundRepository.Verify(r => r.UpdateTransactionAsync(transaction), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldRejectAndNotChangeBalance_WhenRejectAction()
        {
            // Arrange
            var managerId = Guid.NewGuid();
            var request = new ProcessFundRequestDto { TransactionId = 1, Action = "REJECT" };
            var fund = new ClubFund { CurrentBalance = 1500 }; 
            var transaction = new FundTransaction 
            { 
                TransactionId = 1, Status = "PENDING", TransactionType = "EXPENSE", Amount = 1000, ClubFund = fund 
            };
            
            _mockFundRepository.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(transaction);
            _mockFundRepository.Setup(r => r.UpdateTransactionAsync(transaction)).Returns(Task.CompletedTask);

            // Act
            var result = await _clubFundService.ProcessRequestAsync(managerId, request);

            // Assert
            Assert.True(result);
            Assert.Equal("REJECTED", transaction.Status);
            Assert.Equal(managerId, transaction.ApprovedBy);
            Assert.Equal(1500, fund.CurrentBalance); // Unchanged
            _mockFundRepository.Verify(r => r.UpdateClubFundAsync(It.IsAny<ClubFund>()), Times.Never);
            _mockFundRepository.Verify(r => r.UpdateTransactionAsync(transaction), Times.Once);
        }

        #endregion

        #region GetFundHistoryAsync

        [Fact]
        public async Task GetFundHistoryAsync_ShouldReturnTransactions()
        {
            // Arrange
            var transactions = new List<FundTransaction> { new FundTransaction() };
            _mockFundRepository.Setup(r => r.GetTransactionsByFundIdAsync(1, "PENDING")).ReturnsAsync(transactions);

            // Act
            var result = await _clubFundService.GetFundHistoryAsync(1, "pending");

            // Assert
            Assert.Equal(transactions, result);
        }

        [Fact]
        public async Task GetFundHistoryAsync_ShouldHandleNullStatus()
        {
            // Arrange
            var transactions = new List<FundTransaction> { new FundTransaction() };
            _mockFundRepository.Setup(r => r.GetTransactionsByFundIdAsync(1, null)).ReturnsAsync(transactions);

            // Act
            var result = await _clubFundService.GetFundHistoryAsync(1, null);

            // Assert
            Assert.Equal(transactions, result);
        }

        #endregion
    }
}
