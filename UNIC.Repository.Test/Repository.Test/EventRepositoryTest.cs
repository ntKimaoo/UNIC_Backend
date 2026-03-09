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
    public class EventRepositoryTest
    {
        private UnicContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<UnicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UnicContext(options);
        }

        private Event CreateValidEvent(int id, string name = "Test Event")
        {
            return new Event
            {
                EventId = id,
                EventName = name,
                Description = "Description",
                ImageUrl = "http://image.com",
                Location = "Location",
                Status = "PLANNED",
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEvent_WhenExists()
        {
            // Arrange
            var context = GetInMemoryContext();
            var @event = CreateValidEvent(1);
            context.Events.Add(@event);
            await context.SaveChangesAsync();

            var repository = new EventRepository(context);

            // Act
            var result = await repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Event", result.EventName);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPagedEvents()
        {
            // Arrange
            var context = GetInMemoryContext();
            for (int i = 1; i <= 15; i++)
            {
                context.Events.Add(CreateValidEvent(i, "Event " + i));
            }
            await context.SaveChangesAsync();

            var repository = new EventRepository(context);

            // Act
            var result = await repository.GetAllAsync(pageNumber: 1, pageSize: 10);

            // Assert
            Assert.Equal(10, result.Count());
        }

        [Fact]
        public async Task AddAsync_ShouldAddEventToContext()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new EventRepository(context);
            var @event = CreateValidEvent(1);

            // Act
            await repository.AddAsync(@event);
            await context.SaveChangesAsync();

            // Assert
            var inDb = await context.Events.FindAsync(1);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task GetAttendeeCountAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Events.Add(CreateValidEvent(1));
            context.Attendances.AddRange(new List<Attendance>
            {
                new Attendance { EventId = 1, UserId = Guid.NewGuid(), AttendanceStatus = "ATTENDED" },
                new Attendance { EventId = 1, UserId = Guid.NewGuid(), AttendanceStatus = "PENDING" },
                new Attendance { EventId = 1, UserId = Guid.NewGuid(), AttendanceStatus = "CANCELLED" }
            });
            await context.SaveChangesAsync();

            var repository = new EventRepository(context);

            // Act
            var count = await repository.GetAttendeeCountAsync(1);

            // Assert
            Assert.Equal(2, count); // ATTENDED and PENDING
        }
    }
}
