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
    public class ClubRepositoryTest
    {
        private UnicContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<UnicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UnicContext(options);
        }

        private Club CreateValidClub(int id, string name, bool isDeleted = false, bool isActive = true, bool isPublic = true)
        {
            return new Club
            {
                ClubId = id,
                ClubName = name,
                ShortName = id.ToString(),
                Description = "Description for " + name,
                LogoUrl = "http://logo.com",
                CoverImageUrl = "http://cover.com",
                Email = "test@club.com",
                PhoneNumber = "123456789",
                FacebookUrl = "fb.com",
                WebsiteUrl = "web.com",
                Address = "Address",
                Status = "Active",
                IsDeleted = isDeleted,
                IsActive = isActive,
                IsPublic = isPublic,
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnClub_WhenClubExists()
        {
            // Arrange
            var context = GetInMemoryContext();
            var club = CreateValidClub(1, "Test Club");
            context.Clubs.Add(club);
            await context.SaveChangesAsync();

            var repository = new ClubRepository(context);

            // Act
            var result = await repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Club", result.ClubName);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenClubDoesNotExistOrDeleted()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Clubs.Add(CreateValidClub(1, "Deleted Club", isDeleted: true));
            await context.SaveChangesAsync();

            var repository = new ClubRepository(context);

            // Act
            var resultByRealId = await repository.GetByIdAsync(1); 
            var resultByNonExistId = await repository.GetByIdAsync(999);

            // Assert
            Assert.Null(resultByRealId);
            Assert.Null(resultByNonExistId);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllNonDeletedClubs()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Clubs.AddRange(new List<Club>
            {
                CreateValidClub(1, "Club 1"),
                CreateValidClub(2, "Club 2"),
                CreateValidClub(3, "Deleted Club", isDeleted: true)
            });
            await context.SaveChangesAsync();

            var repository = new ClubRepository(context);

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.DoesNotContain(result, c => c.IsDeleted);
        }

        [Fact]
        public async Task GetActiveClubsAsync_ShouldReturnOnlyActiveAndNonDeleted()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Clubs.AddRange(new List<Club>
            {
                CreateValidClub(1, "Active Club", isActive: true),
                CreateValidClub(2, "Inactive Club", isActive: false),
                CreateValidClub(3, "Active Deleted", isActive: true, isDeleted: true)
            });
            await context.SaveChangesAsync();

            var repository = new ClubRepository(context);

            // Act
            var result = await repository.GetActiveClubsAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("Active Club", result.First().ClubName);
        }

        [Fact]
        public async Task GetPublicClubsAsync_ShouldReturnOnlyPublicActiveNonDeleted()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Clubs.AddRange(new List<Club>
            {
                CreateValidClub(1, "Public Active", isActive: true, isPublic: true),
                CreateValidClub(2, "Private Active", isActive: true, isPublic: false),
                CreateValidClub(3, "Public Inactive", isActive: false, isPublic: true)
            });
            await context.SaveChangesAsync();

            var repository = new ClubRepository(context);

            // Act
            var result = await repository.GetPublicClubsAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("Public Active", result.First().ClubName);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddClubToDatabase()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new ClubRepository(context);
            var newClub = CreateValidClub(0, "New Club");
            newClub.ClubId = 0;

            // Act
            var result = await repository.CreateAsync(newClub);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ClubId > 0);
            Assert.Equal("New Club", result.ClubName);
            Assert.False(result.IsDeleted);
            
            var clubInDb = await context.Clubs.FindAsync(result.ClubId);
            Assert.NotNull(clubInDb);
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyExistingClub()
        {
            // Arrange
            var context = GetInMemoryContext();
            var club = CreateValidClub(1, "Old Name");
            context.Clubs.Add(club);
            await context.SaveChangesAsync();

            var repository = new ClubRepository(context);
            club.ClubName = "Updated Name";

            // Act
            var success = await repository.UpdateAsync(club);

            // Assert
            Assert.True(success);
            var updatedClub = await context.Clubs.FindAsync(1);
            Assert.Equal("Updated Name", updatedClub.ClubName);
            Assert.NotNull(updatedClub.UpdatedAt);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveClubFromDatabase()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Clubs.Add(CreateValidClub(1, "To Delete"));
            await context.SaveChangesAsync();

            var repository = new ClubRepository(context);

            // Act
            var success = await repository.DeleteAsync(1);

            // Assert
            Assert.True(success);
            var clubInDb = await context.Clubs.FindAsync(1);
            Assert.Null(clubInDb);
        }

        [Fact]
        public async Task SoftDeleteAsync_ShouldSetIsDeletedToTrue()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Clubs.Add(CreateValidClub(1, "To Soft Delete"));
            await context.SaveChangesAsync();

            var repository = new ClubRepository(context);

            // Act
            var success = await repository.SoftDeleteAsync(1);

            // Assert
            Assert.True(success);
            var clubInDb = await context.Clubs.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ClubId == 1);
            Assert.True(clubInDb.IsDeleted);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenClubExists()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Clubs.Add(CreateValidClub(1, "Existing Club"));
            await context.SaveChangesAsync();

            var repository = new ClubRepository(context);

            // Act
            var result = await repository.ExistsAsync(1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ClubNameExistsAsync_ShouldReturnTrue_WhenNameTaken()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Clubs.Add(CreateValidClub(1, "UniqueName"));
            await context.SaveChangesAsync();

            var repository = new ClubRepository(context);

            // Act
            var exists = await repository.ClubNameExistsAsync("UniqueName");
            var notExists = await repository.ClubNameExistsAsync("NonExistent");

            // Assert
            Assert.True(exists);
            Assert.False(notExists);
        }

        [Fact]
        public async Task ChangeStatusClub_ShouldToggleIsActive()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Clubs.Add(CreateValidClub(1, "StatusToggle", isActive: true));
            await context.SaveChangesAsync();

            var repository = new ClubRepository(context);

            // Act
            await repository.ChangeStatusClub(1);

            // Assert
            var clubInDb = await context.Clubs.FindAsync(1);
            Assert.False(clubInDb.IsActive);

            // Act again
            await repository.ChangeStatusClub(1);
            Assert.True(clubInDb.IsActive);
        }
    }
}
