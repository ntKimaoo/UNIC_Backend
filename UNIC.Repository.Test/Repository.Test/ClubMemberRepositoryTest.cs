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
    public class ClubMemberRepositoryTest
    {
        private UnicContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<UnicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UnicContext(options);
        }

        [Fact]
        public async Task GetMembersByClubIdAsync_ShouldReturnMembers()
        {
            // Arrange
            var context = GetInMemoryContext();
            var clubId = 1;
            context.UserClubRoles.AddRange(new List<UserClubRole>
            {
                new UserClubRole { ClubId = clubId, UserId = Guid.NewGuid() },
                new UserClubRole { ClubId = clubId, UserId = Guid.NewGuid() },
                new UserClubRole { ClubId = 2, UserId = Guid.NewGuid() }
            });
            await context.SaveChangesAsync();

            var repository = new ClubMemberRepository(context);

            // Act
            var result = await repository.GetMembersByClubIdAsync(clubId);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task AddMemberAsync_ShouldAddMember()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new ClubMemberRepository(context);
            var member = new UserClubRole { ClubId = 1, UserId = Guid.NewGuid(), Status = "ACTIVE" };

            // Act
            var result = await repository.AddMemberAsync(member);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ClubMemberId > 0);
            var inDb = await context.UserClubRoles.FindAsync(result.ClubMemberId);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task IsMemberAsync_ShouldReturnTrue_WhenMemberExists()
        {
            // Arrange
            var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var clubId = 10;
            context.UserClubRoles.Add(new UserClubRole { ClubId = clubId, UserId = userId });
            await context.SaveChangesAsync();

            var repository = new ClubMemberRepository(context);

            // Act
            var isMember = await repository.IsMemberAsync(userId, clubId);
            var isNotMember = await repository.IsMemberAsync(Guid.NewGuid(), clubId);

            // Assert
            Assert.True(isMember);
            Assert.False(isNotMember);
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldDeleteMember()
        {
            // Arrange
            var context = GetInMemoryContext();
            var member = new UserClubRole { ClubMemberId = 1, ClubId = 1, UserId = Guid.NewGuid() };
            context.UserClubRoles.Add(member);
            await context.SaveChangesAsync();

            var repository = new ClubMemberRepository(context);

            // Act
            var success = await repository.RemoveMemberAsync(1);

            // Assert
            Assert.True(success);
            var inDb = await context.UserClubRoles.FindAsync(1);
            Assert.Null(inDb);
        }
    }
}
