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
    public class ClubPostRepositoryTest
    {
        private UnicContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<UnicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UnicContext(options);
        }

        private ClubPost CreateValidPost(int id, int clubId, string title = "Test Post")
        {
            return new ClubPost
            {
                PostId = id,
                ClubId = clubId,
                Title = title,
                ImageUrl = "http://image.com",
                Caption = "Caption",
                Content = "Content",
                PostDate = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = "PUBLISHED"
            };
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnPost()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.ClubPosts.Add(CreateValidPost(1, 10));
            await context.SaveChangesAsync();

            var repository = new ClubPostRepository(context);

            // Act
            var result = await repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Post", result.Title);
        }

        [Fact]
        public async Task GetByClubIdAsync_ShouldReturnClubPosts()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.ClubPosts.AddRange(new List<ClubPost>
            {
                CreateValidPost(1, 10, "P1"),
                CreateValidPost(2, 10, "P2"),
                CreateValidPost(3, 20, "P3")
            });
            await context.SaveChangesAsync();

            var repository = new ClubPostRepository(context);

            // Act
            var result = await repository.GetByClubIdAsync(10);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task CreateAsync_ShouldAddPost()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new ClubPostRepository(context);
            var post = CreateValidPost(0, 10, "New Post");

            // Act
            var result = await repository.CreateAsync(post);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.PostId > 0);
            var inDb = await context.ClubPosts.FindAsync(result.PostId);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemovePost()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.ClubPosts.Add(CreateValidPost(1, 10));
            await context.SaveChangesAsync();

            var repository = new ClubPostRepository(context);

            // Act
            var success = await repository.DeleteAsync(1);

            // Assert
            Assert.True(success);
            var inDb = await context.ClubPosts.FindAsync(1);
            Assert.Null(inDb);
        }
    }
}
