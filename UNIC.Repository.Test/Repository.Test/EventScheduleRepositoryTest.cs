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
    public class EventScheduleRepositoryTest
    {
        private UnicContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<UnicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UnicContext(options);
        }

        [Fact]
        public async Task AddAsync_ShouldAddSchedule()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new EventScheduleRepository(context);
            var schedule = new EventSchedule
            {
                EventId = 1,
                ScheduleName = "Morning Session",
                Description ="First session of the day",
                StartTime = DateTime.UtcNow.AddHours(9),
                EndTime = DateTime.UtcNow.AddHours(12)
            };

            // Act
            await repository.AddAsync(schedule);
            await context.SaveChangesAsync();

            // Assert
            var inDb = await context.EventSchedules.FirstOrDefaultAsync(s => s.ScheduleName == "Morning Session");
            Assert.NotNull(inDb);
        }
    }
}
