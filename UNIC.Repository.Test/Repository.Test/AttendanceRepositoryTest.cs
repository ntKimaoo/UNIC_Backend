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
    public class AttendanceRepositoryTest
    {
        private UnicContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<UnicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UnicContext(options);
        }

        [Fact]
        public async Task GetByEventAndUserAsync_ShouldReturnAttendance()
        {
            // Arrange
            var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var eventId = 1;
            context.Attendances.Add(new Attendance { EventId = eventId, UserId = userId, AttendanceStatus = "REGISTERED" });
            await context.SaveChangesAsync();

            var repository = new AttendanceRepository(context);

            // Act
            var result = await repository.GetByEventAndUserAsync(eventId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(eventId, result.EventId);
            Assert.Equal(userId, result.UserId);
        }

        [Fact]
        public async Task IsUserRegisteredAsync_ShouldReturnTrue_WhenExists()
        {
            // Arrange
            var context = GetInMemoryContext();
            var userId = Guid.NewGuid();
            var eventId = 1;
            context.Attendances.Add(new Attendance { EventId = eventId, UserId = userId });
            await context.SaveChangesAsync();

            var repository = new AttendanceRepository(context);

            // Act
            var isRegistered = await repository.IsUserRegisteredAsync(eventId, userId);
            var isNotRegistered = await repository.IsUserRegisteredAsync(eventId, Guid.NewGuid());

            // Assert
            Assert.True(isRegistered);
            Assert.False(isNotRegistered);
        }

        [Fact]
        public async Task GetAttendeesByEventAsync_ShouldReturnEventAttendees()
        {
            // Arrange
            var context = GetInMemoryContext();
            var eventId = 10;
            context.Attendances.AddRange(new List<Attendance>
            {
                new Attendance { EventId = eventId, UserId = Guid.NewGuid() },
                new Attendance { EventId = eventId, UserId = Guid.NewGuid() },
                new Attendance { EventId = 20, UserId = Guid.NewGuid() }
            });
            await context.SaveChangesAsync();

            var repository = new AttendanceRepository(context);

            // Act
            var result = await repository.GetAttendeesByEventAsync(eventId);

            // Assert
            Assert.Equal(2, result.Count());
        }
    }
}
