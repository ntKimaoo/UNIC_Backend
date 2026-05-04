using DataAccess.Context;
using DataAccess.Models;
using DataAccess.Models.Meeting;
using DataAccess.Models.Meeting.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using BC = BCrypt.Net.BCrypt;

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
            SeedVietnameseUsers(context, logger);

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

        private static void SeedVietnameseUsers(UnicContext context, ILogger logger)
        {
            const string sentinelEmail = "nguyenan0101@gmail.com";
            if (context.Users.Any(u => u.Email == sentinelEmail))
                return;

            string[] familyNames   = { "Nguyễn","Trần","Lê","Phạm","Hoàng","Huỳnh","Phan","Vũ","Võ","Đặng","Bùi","Đỗ","Hồ","Ngô","Dương","Lý","Đinh","Lâm","Trương","Thái" };
            string[] familyNamesEn = { "nguyen","tran","le","pham","hoang","huynh","phan","vu","vo","dang","bui","do","ho","ngo","duong","ly","dinh","lam","truong","thai" };

            string[] maleMidNames  = { "Văn","Hữu","Quốc","Minh","Quang","Đức","Thanh","Bảo","Xuân","Công" };
            string[] femMidNames   = { "Thị","Thùy","Bảo","Thanh","Phương","Kim","Ngọc","Diệu","Thu","Mỹ" };

            string[] maleFirst   = { "An","Bình","Cường","Dũng","Đạt","Giang","Hiếu","Khánh","Long","Minh","Nam","Phong","Quân","Sơn","Thắng","Tuấn","Việt","Hùng","Đức","Hải" };
            string[] maleFirstEn = { "an","binh","cuong","dung","dat","giang","hieu","khanh","long","minh","nam","phong","quan","son","thang","tuan","viet","hung","duc","hai" };

            string[] femFirst   = { "Chi","Diệu","Giang","Hạnh","Khánh","Linh","Mai","Nhi","Phương","Quỳnh","Trang","Uyên","Vân","Xuân","Yến","Lan","Hoa","Nhung","Thảo","Ngọc" };
            string[] femFirstEn = { "chi","dieu","giang","hanh","khanh","linh","mai","nhi","phuong","quynh","trang","uyen","van","xuan","yen","lan","hoa","nhung","thao","ngoc" };

            string[] majors = { "Công nghệ thông tin","Kỹ thuật phần mềm","Hệ thống thông tin","An toàn thông tin","Kỹ thuật máy tính","Toán ứng dụng","Quản trị kinh doanh","Kế toán","Ngôn ngữ Anh","Kinh tế" };
            string[] phonePrefixes = { "0912","0913","0914","0915","0916","0917","0918","0919","0932","0933","0934","0935","0936","0937","0938","0939","0962","0963","0964","0965" };

            string passwordHash = BC.HashPassword("Password@123");
            var users = new List<User>();

            // 70 male (i = 0..69) + 20 female (i = 70..89) = 90 users
            // Dùng i trực tiếp cho ngày sinh: i%28 → day, (i/4)%12 → month
            // → lcm(20,28)=140 > 90 nên không thể trùng (tên × ngày) trong phạm vi 90 user
            for (int i = 0; i < 90; i++)
            {
                bool isMale = i < 70;

                string familyName   = familyNames[i   % familyNames.Length];
                string familyNameEn = familyNamesEn[i % familyNamesEn.Length];
                string midName      = isMale ? maleMidNames[i % maleMidNames.Length] : femMidNames[i % femMidNames.Length];
                string firstName    = isMale ? maleFirst[i   % maleFirst.Length]    : femFirst[i   % femFirst.Length];
                string firstNameEn  = isMale ? maleFirstEn[i % maleFirstEn.Length]  : femFirstEn[i % femFirstEn.Length];

                int birthDay   = (i % 28) + 1;
                int birthMonth = ((i / 4) % 12) + 1;
                int birthYear  = 2001 + (i % 4);

                // Email dạng: nguyenthang1610@gmail.com  (familyNameEn + firstNameEn + DD + MM)
                string email     = $"{familyNameEn}{firstNameEn}{birthDay:D2}{birthMonth:D2}@gmail.com";
                string studentId = $"2252{i + 1:D4}";
                string phone     = $"{phonePrefixes[i % phonePrefixes.Length]}{100000 + i}";

                users.Add(new User
                {
                    UserId       = Guid.NewGuid(),
                    FullName     = $"{familyName} {midName} {firstName}",
                    Email        = email,
                    Gender       = isMale ? "Male" : "Female",
                    StudentId    = studentId,
                    Major        = majors[i % majors.Length],
                    PhoneNumber  = phone,
                    DateOfBirth  = new DateOnly(birthYear, birthMonth, birthDay),
                    JoinDate     = new DateOnly(2022 + (i % 2), 9, 5),
                    Status       = "Active",
                    PasswordHash = passwordHash,
                    CreatedAt    = DateTime.UtcNow,
                    UpdatedAt    = DateTime.UtcNow
                });
            }

            context.Users.AddRange(users);
            context.SaveChanges();
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
                    StartedAt = interviewSchedule.ScheduledAt,
                    EndedAt = interviewSchedule.ScheduledAt?.AddMinutes(interviewSchedule.DurationMinutes),
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
