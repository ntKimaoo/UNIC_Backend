using DataAccess.Models;
using DataAccess.Repositories.Implementation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.DataAccess.Models;
using Xunit;

namespace UNIC.Repository.Test.Repository.Test
{
    public class ClubRoleRepositoryTest
    {
        private UnicContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<UnicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UnicContext(options);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnRole()
        {
            // Arrange
            var context = GetInMemoryContext();
            var role = new ClubRole { ClubRoleId = 1, RoleName = "Admin", Description = "Desc", Level = 1 };
            context.ClubRoles.Add(role);
            await context.SaveChangesAsync();

            var repository = new ClubRoleRepository(context);

            // Act
            var result = await repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Admin", result.RoleName);
        }

        [Fact]
        public async Task RoleNameExistsAsync_ShouldReturnTrue_WhenExists()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.ClubRoles.Add(new ClubRole { RoleName = "Member", Description = "D" });
            await context.SaveChangesAsync();

            var repository = new ClubRoleRepository(context);

            // Act
            var exists = await repository.RoleNameExistsAsync("Member");
            var notExists = await repository.RoleNameExistsAsync("NonExistent");

            // Assert
            Assert.True(exists);
            Assert.False(notExists);
        }

        [Fact]
        public async Task SetPoliciesAsync_ShouldReplaceExistingPolicies()
        {
            // Arrange
            var context = GetInMemoryContext();
            var clubRoleId = 1;
            context.ClubRolePolicies.Add(new ClubRolePolicy { ClubRoleId = clubRoleId, PolicyId = 10 });
            await context.SaveChangesAsync();

            var repository = new ClubRoleRepository(context);

            // Act
            await repository.SetPoliciesAsync(clubRoleId, new List<int> { 20, 30 });

            // Assert
            var policies = await context.ClubRolePolicies.Where(p => p.ClubRoleId == clubRoleId).ToListAsync();
            Assert.Equal(2, policies.Count);
            Assert.Contains(policies, p => p.PolicyId == 20);
            Assert.Contains(policies, p => p.PolicyId == 30);
            Assert.DoesNotContain(policies, p => p.PolicyId == 10);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveRole()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.ClubRoles.Add(new ClubRole { ClubRoleId = 1, RoleName = "To Delete", Description = "D" });
            await context.SaveChangesAsync();

            var repository = new ClubRoleRepository(context);

            // Act
            var success = await repository.DeleteAsync(1);

            // Assert
            Assert.True(success);
            var inDb = await context.ClubRoles.FindAsync(1);
            Assert.Null(inDb);
        }
    }
}
