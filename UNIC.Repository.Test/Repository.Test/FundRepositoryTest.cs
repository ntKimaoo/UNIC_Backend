using DataAccess.Models;
using DataAccess.Repositories.Implementation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.Repository.Test.Repository.Test
{
    public class FundRepositoryTest
    {
        private UnicContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<UnicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UnicContext(options);
        }

        private ClubFund CreateValidFund(int id, int clubId, string name = "General Fund")
        {
            return new ClubFund
            {
                FundId = id,
                ClubId = clubId,
                FundName = name,
                TotalAmount = 1000,
                CurrentBalance = 1000,
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task GetFundByIdAsync_ShouldReturnFund()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.ClubFunds.Add(CreateValidFund(1, 10));
            await context.SaveChangesAsync();

            var repository = new FundRepository(context);

            // Act
            var result = await repository.GetFundByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("General Fund", result.FundName);
        }

        [Fact]
        public async Task AddTransactionAsync_ShouldAddTransaction()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new FundRepository(context);
            var transaction = new FundTransaction
            {
                FundId = 1,
                TransactionType = "INCOME",
                Amount = 500,
                Description = "Donation",
                Status = "APPROVED"
            };

            // Act
            await repository.AddTransactionAsync(transaction);

            // Assert
            var inDb = await context.FundTransactions.FindAsync(transaction.TransactionId);
            Assert.NotNull(inDb);
            Assert.Equal(500, inDb.Amount);
        }

        [Fact]
        public async Task GetTransactionsByFundIdAsync_ShouldFilterByFund()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.FundTransactions.AddRange(new List<FundTransaction>
            {
                new FundTransaction { FundId = 1, TransactionType = "INCOME", Amount = 100, Description = "T1" },
                new FundTransaction { FundId = 1, TransactionType = "EXPENSE", Amount = 50, Description = "T2" },
                new FundTransaction { FundId = 2, TransactionType = "INCOME", Amount = 200, Description = "T3" }
            });
            await context.SaveChangesAsync();

            var repository = new FundRepository(context);

            // Act
            var result = await repository.GetTransactionsByFundIdAsync(1);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task UpdateClubFundAsync_ShouldModifyFund()
        {
            // Arrange
            var context = GetInMemoryContext();
            var fund = CreateValidFund(1, 10);
            context.ClubFunds.Add(fund);
            await context.SaveChangesAsync();

            var repository = new FundRepository(context);
            fund.CurrentBalance = 1500;

            // Act
            await repository.UpdateClubFundAsync(fund);

            // Assert
            var updated = await context.ClubFunds.FindAsync(1);
            Assert.Equal(1500, updated.CurrentBalance);
        }
    }
}
