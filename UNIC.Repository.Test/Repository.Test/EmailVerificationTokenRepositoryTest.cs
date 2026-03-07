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
    public class EmailVerificationTokenRepositoryTest
    {
        private UnicContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<UnicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UnicContext(options);
        }

        [Fact]
        public async Task GetByTokenHashAsync_ShouldReturnValidToken()
        {
            // Arrange
            var context = GetInMemoryContext();
            var token = new EmailVerificationToken
            {
                TokenHash = "hash123",
                UserId = Guid.NewGuid(),
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };
            context.EmailVerificationTokens.Add(token);
            await context.SaveChangesAsync();

            var repository = new EmailVerificationTokenRepository(context);

            // Act
            var result = await repository.GetByTokenHashAsync("hash123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("hash123", result.TokenHash);
        }

        [Fact]
        public async Task MarkAsUsedAsync_ShouldUpdateToken()
        {
            // Arrange
            var context = GetInMemoryContext();
            var token = new EmailVerificationToken { EmailVerificationTokenId = 1, TokenHash = "h", UserId = Guid.NewGuid(), IsUsed = false };
            context.EmailVerificationTokens.Add(token);
            await context.SaveChangesAsync();

            var repository = new EmailVerificationTokenRepository(context);

            // Act
            var success = await repository.MarkAsUsedAsync(1);

            // Assert
            Assert.True(success);
            var inDb = await context.EmailVerificationTokens.FindAsync(1);
            Assert.True(inDb.IsUsed);
            Assert.NotNull(inDb.UsedAt);
        }

        [Fact]
        public async Task InvalidateAllByUserIdAsync_ShouldMarkUsed()
        {
            // Arrange
            var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            context.EmailVerificationTokens.AddRange(new List<EmailVerificationToken>
            {
                new EmailVerificationToken { UserId = userId, TokenHash = "h1", IsUsed = false, ExpiresAt = DateTime.UtcNow.AddDays(1) },
                new EmailVerificationToken { UserId = userId, TokenHash = "h2", IsUsed = false, ExpiresAt = DateTime.UtcNow.AddDays(1) }
            });
            await context.SaveChangesAsync();

            var repository = new EmailVerificationTokenRepository(context);

            // Act
            var success = await repository.InvalidateAllByUserIdAsync(userId);

            // Assert
            Assert.True(success);
            var tokens = await context.EmailVerificationTokens.Where(t => t.UserId == userId).ToListAsync();
            Assert.All(tokens, t => Assert.True(t.IsUsed));
        }
    }
}
