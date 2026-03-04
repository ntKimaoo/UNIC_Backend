using DataAccess.Context;
using DataAccess.Models.Meeting;
using DataAccess.Models.Meeting.Enums;
using DataAccess.Repositories.Implementation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UNIC.Repository.Test.Repository.Test
{
    public class InterviewRepositoryTest
    {
        private MeetingDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<MeetingDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new MeetingDbContext(options);
        }

        [Fact]
        public async Task CreateScheduleAsync_ShouldAddSchedule()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new InterviewRepository(context);
            var schedule = new InterviewSchedule
            {
                Title = "Test Interview",
                ApplicationId = 1,
                CandidateUserId = Guid.NewGuid(),
                CampaignId = 1,
                CreatedByUserId = Guid.NewGuid(),
                ScheduledAt = DateTime.UtcNow.AddDays(1),
                Status = InterviewStatus.Scheduled
            };

            // Act
            var result = await repository.CreateScheduleAsync(schedule);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            var inDb = await context.InterviewSchedules.FindAsync(result.Id);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task GetSchedulesAsync_ShouldFilterByCampaign()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.InterviewSchedules.AddRange(new List<InterviewSchedule>
            {
                new InterviewSchedule { CampaignId = 1, Title = "C1", ApplicationId = 1, CandidateUserId = Guid.NewGuid(), CreatedByUserId = Guid.NewGuid() },
                new InterviewSchedule { CampaignId = 2, Title = "C2", ApplicationId = 2, CandidateUserId = Guid.NewGuid(), CreatedByUserId = Guid.NewGuid() }
            });
            await context.SaveChangesAsync();

            var repository = new InterviewRepository(context);

            // Act
            var result = await repository.GetSchedulesAsync(campaignId: 1, status: null, fromDate: null, toDate: null);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result.First().CampaignId);
        }

        [Fact]
        public async Task CreateAssignmentAsync_ShouldAddAssignment()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new InterviewRepository(context);
            var assignment = new InterviewAssignment
            {
                InterviewScheduleId = 1,
                InterviewerUserId = Guid.NewGuid(),
                Role = InterviewerRole.Interviewer
            };

            // Act
            var result = await repository.CreateAssignmentAsync(assignment);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
        }

        [Fact]
        public async Task GetRoomByCodeAsync_ShouldReturnRoom()
        {
            // Arrange
            var context = GetInMemoryContext();
            var room = new MeetingRoom { RoomCode = "ROOM123", InterviewScheduleId = 1, Status = RoomStatus.Active };
            context.MeetingRooms.Add(room);
            await context.SaveChangesAsync();

            var repository = new InterviewRepository(context);

            // Act
            var result = await repository.GetRoomByCodeAsync("ROOM123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ROOM123", result.RoomCode);
        }
    }
}
