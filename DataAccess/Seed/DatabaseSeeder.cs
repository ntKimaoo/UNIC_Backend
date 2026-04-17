using DataAccess.Context;
using DataAccess.Models;
using DataAccess.Models.Meeting;
using DataAccess.Models.Meeting.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace UNIC.DataAccess.Seed
{
    public static class DatabaseSeeder
    {
        public static void SeedData(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
            try
            {
                var unicContext = serviceProvider.GetRequiredService<UnicContext>();
                var meetingContext = serviceProvider.GetRequiredService<MeetingDbContext>();

                SeedUnicContext(unicContext, logger);
                SeedMeetingContext(meetingContext, logger);
                
                logger.LogInformation("Database seeded successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the databases.");
            }
        }

        private static void SeedUnicContext(UnicContext context, ILogger logger)
        {
            if (!context.Users.Any(u => u.Email == "admin@uniclub.com"))
            {
                var adminUserId = Guid.NewGuid();
                var adminUser = new User
                {
                    UserId = adminUserId,
                    FullName = "System Admin",
                    Email = "admin@uniclub.com",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    // Note: Use a plain text here or a pre-computed hash string.
                    // Doing a pre-computed hash for 'admin123' so BCrypt dependency is not needed here
                    PasswordHash = "$2a$11$.Yl0uXZcZJzZJzZJzZJzZJzZJzZJzZJzZJzZJzZJzZJzZJzZJzZJz" // Placeholder for admin123
                };
                context.Users.Add(adminUser);

                context.UserRoles.Add(new UserRole
                {
                    UserId = adminUserId,
                    RoleName = "Admin",
                    AssignedAt = DateTime.UtcNow
                });
                context.SaveChanges();
                logger.LogInformation("Seeded User Admin.");
            }

            if (!context.Clubs.Any(c => c.ShortName == "UIT"))
            {
                var dummyClub = new Club
                {
                    ClubName = "UniClub IT",
                    ShortName = "UIT",
                    Description = "Information Technology Club",
                    Status = "Active",
                    Email = "it@uniclub.com",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsPublic = true,
                    LogoUrl = "logo.png",
                    CoverImageUrl = "cover.png",
                    PhoneNumber = "0123456789",
                    FacebookUrl = "https://facebook.com/uit",
                    WebsiteUrl = "https://uit.com",
                    Address = "123 IT Street"
                };
                context.Clubs.Add(dummyClub);
                context.SaveChanges();
                logger.LogInformation("Seeded Club UIT.");
            }

            if (!context.FundTypes.Any())
            {
                context.FundTypes.AddRange(
                    new FundType { Name = "Hàng Tháng", IsActive = true, SortOrder = 0 },
                    new FundType { Name = "Sự Kiện", IsActive = true, SortOrder = 1 },
                    new FundType { Name = "Quyên Góp", IsActive = true, SortOrder = 2 }
                );
                context.SaveChanges();
                logger.LogInformation("Seeded FundTypes.");
            }
        }

        private static void SeedMeetingContext(MeetingDbContext context, ILogger logger)
        {
            if (!context.InterviewSchedules.Any())
            {
                var interviewSchedule = new InterviewSchedule
                {
                    Title = "Initial Interview for IT Club",
                    Description = "Interview for evaluating technical skills.",
                    ScheduledAt = DateTime.UtcNow.AddDays(1),
                    DurationMinutes = 60,
                    Status = InterviewStatus.Scheduled,
                    ApplicationId = 1, // Dummy ID
                    CandidateUserId = Guid.NewGuid(),
                    CampaignId = 1,
                    CreatedByUserId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                };
                context.InterviewSchedules.Add(interviewSchedule);
                context.SaveChanges();

                // Now add meeting room
                var meetingRoom = new MeetingRoom
                {
                    InterviewScheduleId = interviewSchedule.Id,
                    RoomType = RoomType.Interview,
                    Title = interviewSchedule.Title,
                    CreatedByUserId = interviewSchedule.CreatedByUserId,
                    ScheduledStartAt = interviewSchedule.ScheduledAt,
                    ScheduledEndAt = interviewSchedule.ScheduledAt.AddMinutes(interviewSchedule.DurationMinutes),
                    RoomCode = "ROOM-IT-001",
                    Status = RoomStatus.Idle,
                    CreatedAt = DateTime.UtcNow
                };
                context.MeetingRooms.Add(meetingRoom);
                context.SaveChanges();
                
                logger.LogInformation("Seeded Meeting Room and Interview Schedule.");
            }
        }
    }
}
