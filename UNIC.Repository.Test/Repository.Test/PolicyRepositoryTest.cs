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
    public class PolicyRepositoryTest
    {
        private UnicContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<UnicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UnicContext(options);
        }

        [Fact]
        public async Task GetPolicyByTitleAsync_ShouldReturnPolicy()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Policies.Add(new Policy { Title = "Can_Edit", Name = "Edit" });
            await context.SaveChangesAsync();

            var repository = new PolicyRepository(context);

            // Act
            var result = await repository.GetPolicyByTitleAsync("Can_Edit");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Can_Edit", result.Title);
        }

        [Fact]
        public async Task HasUserPolicyAsync_ShouldCheckDirectAssignment()
        {
            // Arrange
            var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var policy = new Policy { Id = 1, Title = "Direct_Policy" };
            context.Policies.Add(policy);
            context.ClubMemberPolicies.Add(new ClubMemberPolicy { UserId = userId, PolicyId = 1 });
            await context.SaveChangesAsync();

            var repository = new PolicyRepository(context);

            // Act
            var hasPolicy = await repository.HasUserPolicyAsync(userId, "Direct_Policy");

            // Assert
            Assert.True(hasPolicy);
        }

        [Fact]
        public async Task HasUserPolicyAsync_ShouldCheckRoleAssignment()
        {
            // Arrange
            var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var role = new ClubRole { ClubRoleId = 1, RoleName = "Manager" };
            var policy = new Policy { Id = 2, Title = "Role_Policy" };
            context.Policies.Add(policy);
            context.ClubRoles.Add(role);
            context.ClubRolePolicies.Add(new ClubRolePolicy { ClubRoleId = 1, PolicyId = 2 });
            context.UserClubRoles.Add(new UserClubRole { UserId = userId, ClubId = 1, ClubRoleId = 1 });
            await context.SaveChangesAsync();

            var repository = new PolicyRepository(context);

            // Act
            var hasPolicy = await repository.HasUserPolicyAsync(userId, "Role_Policy");

            // Assert
            Assert.True(hasPolicy);
        }

        [Fact]
        public async Task GetAllPolicyGroupAsync_ShouldReturnAllGroups()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.PolicyGroups.AddRange(new List<PolicyGroup>
            {
                new PolicyGroup { PolicyGroupId = 1, Name = "Group 1" },
                new PolicyGroup { PolicyGroupId = 2, Name = "Group 2" }
            });
            await context.SaveChangesAsync();

            var repository = new PolicyRepository(context);

            // Act
            var result = await repository.GetAllPolicyGroupAsync();

            // Assert
            Assert.Equal(2, result.Count());
        }
    }
}
