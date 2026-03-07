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
    public class UserRepositoryTest
    {
        private UnicContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<UnicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UnicContext(options);
        }

        private User CreateValidUser(Guid id, string email, string fullName = "Test User")
        {
            return new User
            {
                UserId = id,
                Email = email,
                FullName = fullName,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnUser_WhenExists()
        {
            // Arrange
            var context = GetInMemoryContext();
            var user = CreateValidUser(Guid.NewGuid(), "test@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var repository = new UserRepository(context);

            // Act
            var result = await repository.GetByEmailAsync("test@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test@test.com", result.Email);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnUser_WhenExists()
        {
            // Arrange
            var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var user = CreateValidUser(userId, "test@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var repository = new UserRepository(context);

            // Act
            var result = await repository.GetByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
        }

        [Fact]
        public async Task EmailExistsAsync_ShouldReturnTrue_WhenEmailTaken()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Users.Add(CreateValidUser(Guid.NewGuid(), "taken@test.com"));
            await context.SaveChangesAsync();

            var repository = new UserRepository(context);

            // Act
            var exists = await repository.EmailExistsAsync("taken@test.com");
            var notExists = await repository.EmailExistsAsync("free@test.com");

            // Assert
            Assert.True(exists);
            Assert.False(notExists);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddUserAndRole()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new UserRepository(context);
            var newUser = CreateValidUser(Guid.NewGuid(), "new@test.com");

            // Act
            var result = await repository.CreateAsync(newUser);

            // Assert
            Assert.NotNull(result);
            var userInDb = await context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.UserId == result.UserId);
            Assert.NotNull(userInDb);
            Assert.Contains(userInDb.UserRoles, r => r.RoleName == "User");
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyUser()
        {
            // Arrange
            var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var user = CreateValidUser(userId, "old@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var repository = new UserRepository(context);
            user.FullName = "Updated Name";

            // Act
            var success = await repository.UpdateAsync(user);

            // Assert
            Assert.True(success);
            var updatedUser = await context.Users.FindAsync(userId);
            Assert.Equal("Updated Name", updatedUser.FullName);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveUser()
        {
            // Arrange
            var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            context.Users.Add(CreateValidUser(userId, "delete@test.com"));
            await context.SaveChangesAsync();

            var repository = new UserRepository(context);

            // Act
            var success = await repository.DeleteAsync(userId);

            // Assert
            Assert.True(success);
            var userInDb = await context.Users.FindAsync(userId);
            Assert.Null(userInDb);
        }
    }
}
