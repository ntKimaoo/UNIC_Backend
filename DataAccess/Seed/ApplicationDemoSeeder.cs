using DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Seed
{
    public static class ApplicationDemoSeeder
    {
        private const string DemoCampaignName = "Tuyển thành viên CLB Lập trình 2025 (Demo)";

        public static readonly Guid TestUserId = new Guid("11111111-1111-1111-1111-111111111111");
        private const string TestUserEmail = "test@unic.demo";

        public static async Task SeedAsync(UnicContext context)
        {
            await EnsureTestUserExistsAsync(context);

            var hasDemo = await context.RecruitmentCampaigns
                .AnyAsync(c => c.CampaignName == DemoCampaignName);
            if (hasDemo)
                return;

            var club = new Club
            {
                ClubName = "Câu lạc bộ Lập trình UNIC",
                ShortName = "UNIC Dev",
                Description = "CLB dành cho sinh viên yêu thích lập trình và công nghệ.",
                Status = "ACTIVE",
                Address = "Trường ĐH CNTT - UNIC (Demo)",
                Email = "",
                PhoneNumber = "",
                FacebookUrl = "",
                WebsiteUrl = "",
                LogoUrl = "",
                CoverImageUrl = "",
                IsPublic = true,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };
            context.Clubs.Add(club);
            await context.SaveChangesAsync();

            var campaign = new RecruitmentCampaign
            {
                ClubId = club.ClubId,
                CampaignName = DemoCampaignName,
                Description = "Đợt tuyển thành viên mới cho năm 2025. Hãy điền form và trả lời các câu hỏi bên dưới.",
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow.AddMonths(2),
                Status = "OPEN",
                ImageUrl = "",
                LinkCampaign = "",
                Content = "Chào mừng bạn đến với đơn ứng tuyển. Sau khi nộp đơn, ban chủ nhiệm sẽ duyệt và thông báo kết quả. Ứng viên đạt (SUCCESS) sẽ được mời tham gia phỏng vấn.",
                CreatedAt = DateTime.UtcNow
            };
            context.RecruitmentCampaigns.Add(campaign);
            await context.SaveChangesAsync();

            var form = new ApplicationForm
            {
                CampaignId = campaign.CampaignId,
                FormName = "Đơn ứng tuyển thành viên",
                FormTitle = "Form ứng tuyển thành viên CLB Lập trình UNIC",
                Description = "Vui lòng điền đầy đủ thông tin và trả lời các câu hỏi.",
                CreatedAt = DateTime.UtcNow
            };
            context.ApplicationForms.Add(form);
            await context.SaveChangesAsync();

            var questions = new[]
            {
                new ApplicationQuestion
                {
                    FormId = form.FormId,
                    QuestionText = "Họ và tên đầy đủ của bạn?",
                    QuestionType = "TEXT",
                    IsRequired = true,
                    DisplayOrder = 1
                },
                new ApplicationQuestion
                {
                    FormId = form.FormId,
                    QuestionText = "Mã số sinh viên (MSSV)?",
                    QuestionType = "TEXT",
                    IsRequired = true,
                    DisplayOrder = 2
                },
                new ApplicationQuestion
                {
                    FormId = form.FormId,
                    QuestionText = "Bạn biết đến câu lạc bộ qua đâu? (bạn bè, mạng xã hội, poster, ...)",
                    QuestionType = "TEXT",
                    IsRequired = false,
                    DisplayOrder = 3
                },
                new ApplicationQuestion
                {
                    FormId = form.FormId,
                    QuestionText = "Tại sao bạn muốn tham gia CLB Lập trình UNIC?",
                    QuestionType = "ESSAY",
                    IsRequired = true,
                    DisplayOrder = 4
                },
                new ApplicationQuestion
                {
                    FormId = form.FormId,
                    QuestionText = "Kinh nghiệm lập trình của bạn (ngôn ngữ, dự án, nếu chưa có có thể ghi \"Chưa có\")?",
                    QuestionType = "ESSAY",
                    IsRequired = false,
                    DisplayOrder = 5
                }
            };

            context.ApplicationQuestions.AddRange(questions);
            await context.SaveChangesAsync();
        }

        private static async Task EnsureTestUserExistsAsync(UnicContext context)
        {
            var exists = await context.Users.AnyAsync(u => u.UserId == TestUserId);
            if (exists) return;

            context.Users.Add(new User
            {
                UserId = TestUserId,
                FullName = "Tài khoản test (chưa đăng nhập)",
                Email = TestUserEmail,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }
}
