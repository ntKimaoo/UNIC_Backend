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
    public class RefreshTokenRepositoryTest
    {
        private UnicContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<UnicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UnicContext(options);
        }

        [Fact]
        public async Task GetByTokenHashAsync_ShouldReturnToken()
        {
            // Arrange
            var context = GetInMemoryContext();
            var token = new RefreshToken { TokenHash = "t123", UserId = Guid.NewGuid(), ExpiresAt = DateTime.UtcNow.AddDays(7), IsRevoked = false };
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            var repository = new RefreshTokenRepository(context);

            // Act
            var result = await repository.GetByTokenHashAsync("t123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("t123", result.TokenHash);
        }

        [Fact]
        public async Task RevokeAllByUserIdAsync_ShouldRevokeTokens()
        {
            // Arrange
            var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            context.RefreshTokens.AddRange(new List<RefreshToken>
            {
                new RefreshToken { UserId = userId, TokenHash = "t1", IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddDays(1) },
                new RefreshToken { UserId = userId, TokenHash = "t2", IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddDays(1) }
            });
            await context.SaveChangesAsync();

            var repository = new RefreshTokenRepository(context);

            // Act
            var success = await repository.RevokeAllByUserIdAsync(userId);

            // Assert
            Assert.True(success);
            var tokens = await context.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync();
            Assert.All(tokens, t => Assert.True(t.IsRevoked));
        }

        [Fact]
        public async Task GetActiveTokensByUserIdAsync_ShouldReturnOnlyActive()
        {
            // Arrange
            var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            context.RefreshTokens.AddRange(new List<RefreshToken>
            {
                new RefreshToken { UserId = userId, TokenHash = "active", IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddDays(1) },
                new RefreshToken { UserId = userId, TokenHash = "revoked", IsRevoked = true, ExpiresAt = DateTime.UtcNow.AddDays(1) },
                new RefreshToken { UserId = userId, TokenHash = "expired", IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddHours(-1) }
            });
            await context.SaveChangesAsync();

            var repository = new RefreshTokenRepository(context);

            // Act
            var result = await repository.GetActiveTokensByUserIdAsync(userId);

            // Assert
            Assert.Single(result);
            Assert.Equal("active", result.First().TokenHash);
        }
    }
}
