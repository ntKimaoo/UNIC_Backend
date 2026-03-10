using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNIC.DataAccess.Repositories.Implementation;
using Xunit;

namespace UNIC.Repository.Test.Repository.Test
{
    public class DepartmentRepositoryTest
    {
        private UnicContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<UnicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UnicContext(options);
        }

        private Department CreateValidDept(int id, int clubId, string name = "Test Dept")
        {
            return new Department
            {
                DepartmentId = id,
                ClubId = clubId,
                DepartmentName = name,
                Description = "Description",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnDept()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Departments.Add(CreateValidDept(1, 10));
            await context.SaveChangesAsync();

            var repository = new DepartmentRepository(context);

            // Act
            var result = await repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Dept", result.DepartmentName);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddDept()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new DepartmentRepository(context);
            var dept = CreateValidDept(0, 10, "New Dept");

            // Act
            var result = await repository.CreateAsync(dept);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.DepartmentId > 0);
            var inDb = await context.Departments.FindAsync(result.DepartmentId);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyDept()
        {
            // Arrange
            var context = GetInMemoryContext();
            var dept = CreateValidDept(1, 10);
            context.Departments.Add(dept);
            await context.SaveChangesAsync();

            var repository = new DepartmentRepository(context);
            dept.DepartmentName = "Updated Name";

            // Act
            var success = await repository.UpdateAsync(dept);

            // Assert
            Assert.True(success);
            var updated = await context.Departments.FindAsync(1);
            Assert.Equal("Updated Name", updated.DepartmentName);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveDept()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Departments.Add(CreateValidDept(1, 10));
            await context.SaveChangesAsync();

            var repository = new DepartmentRepository(context);

            // Act
            var success = await repository.DeleteAsync(1);

            // Assert
            Assert.True(success);
            var inDb = await context.Departments.FindAsync(1);
            Assert.Null(inDb);
        }
    }
}
