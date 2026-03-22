using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using UNIC.DataAccess.Models;

namespace DataAccess.Models;

public partial class UnicContext : DbContext
{
    public UnicContext()
    {
    }

    public UnicContext(DbContextOptions<UnicContext> options)
        : base(options)
    {
    }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Club> Clubs { get; set; }
    public DbSet<ClubRole> ClubRoles { get; set; }
    public DbSet<UserClubRole> UserClubRoles { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<ClubPost> ClubPosts { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EventSchedule> EventSchedules { get; set; }
    public DbSet<ScheduleDetail> ScheduleDetails { get; set; }
    public DbSet<EventImage> EventImages { get; set; }
    public DbSet<EventBudget> EventBudgets { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<FundCategory> FundCategories { get; set; }
    public DbSet<ClubFund> ClubFunds { get; set; }
    public DbSet<FundTransaction> FundTransactions { get; set; }
    public DbSet<RecruitmentCampaign> RecruitmentCampaigns { get; set; }
    public DbSet<ApplicationForm> ApplicationForms { get; set; }
    public DbSet<ApplicationQuestion> ApplicationQuestions { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<ApplicationAnswer> ApplicationAnswers { get; set; }
    public DbSet<ClubMemberPolicy> ClubMemberPolicies { get; set; }
    public DbSet<Policy> Policies { get; set; }
    public DbSet<ClubRolePolicy> ClubRolePolicies { get; set; }
    public DbSet<PolicyGroup> PolicyGroups { get; set; }
    public DbSet<UserPolicy> UserPolicies { get; set; }
    public DbSet<UserRolePolicy> UserRolePolicies { get; set; }
    public DbSet<ClubCreationRequest> ClubCreationRequests { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasKey(e => e.EmailVerificationTokenId).HasName("PK__EmailVer__B16196D29A5849AE");

            entity.HasIndex(e => e.UserId, "IX_EmailVerificationTokens_UserId");

            entity.HasIndex(e => e.TokenHash, "IX_EmailVerificationTokens_TokenHash");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsUsed).HasDefaultValue(false);
            entity.Property(e => e.TokenHash).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.EmailVerificationTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__EmailVeri__User__45F365D3");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__0CF04B38FFE70BBA");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105345C1FCA58").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Major).HasMaxLength(200);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.StudentId)
                .HasMaxLength(50)
                .HasColumnName("StudentID");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<ClubCreationRequest>()
            .HasOne(cr => cr.User)
            .WithMany()
            .HasForeignKey(cr => cr.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ClubCreationRequest>()
            .HasOne(cr => cr.ReviewedByUser)
            .WithMany()
            .HasForeignKey(cr => cr.ReviewedBy)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.PasswordResetTokenId).HasName("PK__Password__160661284C508CB5");

            entity.HasIndex(e => e.UserId, "IX_PasswordResetTokens_UserId");

            entity.HasIndex(e => e.TokenHash, "IX_PasswordResetTokens_TokenHash");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsUsed).HasDefaultValue(false);
            entity.Property(e => e.TokenHash).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__PasswordR__User__412EB0B6");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.RefreshTokenId).HasName("PK__RefreshT__F5845E595E9566E1");

            entity.Property(e => e.RefreshTokenId).HasColumnName("RefreshTokenID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DeviceInfo).HasMaxLength(255);
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(50)
                .HasColumnName("IPAddress");
            entity.Property(e => e.IsRevoked).HasDefaultValue(false);
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.RevokedAt).HasColumnType("datetime");
            entity.Property(e => e.TokenHash).HasMaxLength(500);

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RefreshTo__User__3C69FB99");
        });
        modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

        // UserClubRole - Self-referencing for AssignedBy
        modelBuilder.Entity<UserClubRole>()
            .HasOne(ucr => ucr.AssignedByUser)
            .WithMany()
            .HasForeignKey(ucr => ucr.AssignedBy)
            .OnDelete(DeleteBehavior.NoAction);

        // UserClubRole relationships
        modelBuilder.Entity<UserClubRole>()
            .HasOne(ucr => ucr.User)
            .WithMany(u => u.ClubMembers)
            .HasForeignKey(ucr => ucr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserClubRole>()
            .HasOne(ucr => ucr.Club)
            .WithMany(c => c.ClubMembers)
            .HasForeignKey(ucr => ucr.ClubId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserClubRole>()
            .HasOne(ucr => ucr.ClubRole)
            .WithMany(cr => cr.ClubMembers)
            .HasForeignKey(ucr => ucr.ClubRoleId)
            .OnDelete(DeleteBehavior.NoAction);

        // ClubRole - Department relationship
        modelBuilder.Entity<ClubRole>()
            .HasOne(cr => cr.Department)
            .WithMany(d => d.ClubRoles)
            .HasForeignKey(cr => cr.DepartmentId)
            .OnDelete(DeleteBehavior.NoAction);

        // Department - Club
        modelBuilder.Entity<Department>()
            .HasOne(d => d.Club)
            .WithMany(c => c.Departments)
            .HasForeignKey(d => d.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        // Department - ManagerRole (ClubRole)
        modelBuilder.Entity<Department>()
            .HasOne(d => d.ManagerRole)
            .WithMany(cr => cr.ManagedDepartments)
            .HasForeignKey(d => d.ManagerRoleId)
            .OnDelete(DeleteBehavior.NoAction);

        // ClubPost relationships
        modelBuilder.Entity<ClubPost>()
            .HasOne(cp => cp.Club)
            .WithMany(c => c.ClubPosts)
            .HasForeignKey(cp => cp.ClubId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ClubPost>()
            .HasOne(cp => cp.User)
            .WithMany(u => u.ClubPosts)
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Notification - User
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Event - Club
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Club)
            .WithMany(c => c.Events)
            .HasForeignKey(e => e.ClubId)
            .OnDelete(DeleteBehavior.NoAction);

        // EventSchedule - Event
        modelBuilder.Entity<EventSchedule>()
            .HasOne(es => es.Event)
            .WithMany(e => e.EventSchedules)
            .HasForeignKey(es => es.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // ScheduleDetail - EventSchedule
        modelBuilder.Entity<ScheduleDetail>()
            .HasOne(sd => sd.EventSchedule)
            .WithMany(es => es.ScheduleDetails)
            .HasForeignKey(sd => sd.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        // EventImage - Event
        modelBuilder.Entity<EventImage>()
            .HasOne(ei => ei.Event)
            .WithMany(e => e.EventImages)
            .HasForeignKey(ei => ei.EventId)
            .OnDelete(DeleteBehavior.NoAction);

        // EventBudget - Event
        modelBuilder.Entity<EventBudget>()
            .HasOne(eb => eb.Event)
            .WithMany(e => e.EventBudgets)
            .HasForeignKey(eb => eb.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Attendance relationships
        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.User)
            .WithMany(u => u.Attendances)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Event)
            .WithMany(e => e.Attendances)
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Attendance>()
            .HasIndex(a => a.CheckInToken)
            .IsUnique()
            .HasFilter("[CheckInToken] IS NOT NULL");

        // ClubFund - Club
        modelBuilder.Entity<ClubFund>()
            .HasOne(cf => cf.Club)
            .WithMany(c => c.ClubFunds)
            .HasForeignKey(cf => cf.ClubId)
            .OnDelete(DeleteBehavior.NoAction);

        // FundTransaction relationships
        modelBuilder.Entity<FundTransaction>()
            .HasOne(ft => ft.ClubFund)
            .WithMany(cf => cf.FundTransactions)
            .HasForeignKey(ft => ft.FundId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<FundTransaction>()
            .HasOne(ft => ft.FundCategory)
            .WithMany(fc => fc.FundTransactions)
            .HasForeignKey(ft => ft.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<FundTransaction>()
            .HasOne(ft => ft.Creator)
            .WithMany()
            .HasForeignKey(ft => ft.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        // RecruitmentCampaign - Club
        modelBuilder.Entity<RecruitmentCampaign>()
            .HasOne(rc => rc.Club)
            .WithMany(c => c.RecruitmentCampaigns)
            .HasForeignKey(rc => rc.ClubId)
            .OnDelete(DeleteBehavior.NoAction);

        // ApplicationForm - RecruitmentCampaign
        modelBuilder.Entity<ApplicationForm>()
            .HasOne(af => af.RecruitmentCampaign)
            .WithMany(rc => rc.ApplicationForms)
            .HasForeignKey(af => af.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        // ApplicationQuestion - ApplicationForm
        modelBuilder.Entity<ApplicationQuestion>()
            .HasOne(aq => aq.ApplicationForm)
            .WithMany(af => af.ApplicationQuestions)
            .HasForeignKey(aq => aq.FormId)
            .OnDelete(DeleteBehavior.Cascade);

        // Application relationships
        modelBuilder.Entity<Application>()
            .HasOne(a => a.ApplicationForm)
            .WithMany(af => af.Applications)
            .HasForeignKey(a => a.FormId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.User)
            .WithMany(u => u.Applications)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ApplicationAnswer relationships
        modelBuilder.Entity<ApplicationAnswer>()
            .HasOne(aa => aa.Application)
            .WithMany(a => a.ApplicationAnswers)
            .HasForeignKey(aa => aa.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationAnswer>()
            .HasOne(aa => aa.ApplicationQuestion)
            .WithMany(aq => aq.ApplicationAnswers)
            .HasForeignKey(aa => aa.QuestionId)
            .OnDelete(DeleteBehavior.NoAction);

        // =============================================
        // INDEXES
        // =============================================

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Club>()
            .HasIndex(c => c.ClubName);

        modelBuilder.Entity<Event>()
            .HasIndex(e => e.StartDate);

        modelBuilder.Entity<ClubPost>()
            .HasIndex(cp => cp.PostDate);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead });

        modelBuilder.Entity<Application>()
            .HasIndex(a => a.Status);
        OnModelCreatingPartial(modelBuilder);
        modelBuilder.Entity<ClubMemberPolicy>()
            .HasKey(cmp => new { cmp.ClubMemberId, cmp.PolicyId });
        modelBuilder.Entity<ClubRolePolicy>()
            .HasKey(crp => new { crp.ClubRoleId, crp.PolicyId });
        modelBuilder.Entity<UserPolicy>()
            .HasKey(cmp => new { cmp.UserId, cmp.PolicyId });
        modelBuilder.Entity<UserRolePolicy>()
            .HasKey(cmp => new { cmp.RoleId, cmp.PolicyId });
        modelBuilder.Entity<ClubMemberPolicy>()
            .HasOne<UserClubRole>(u => u.ClubMember)
            .WithMany(p => p.ClubMemberPolicies)
            .HasForeignKey(crp => crp.ClubMemberId);
        modelBuilder.Entity<ClubMemberPolicy>()
            .HasOne<Policy>(p => p.Policy)
            .WithMany(cmp => cmp.ClubMemberPolicies)
            .HasForeignKey(crp => crp.PolicyId);
        modelBuilder.Entity<ClubRolePolicy>()
            .HasOne<ClubRole>(cr => cr.ClubRole)
            .WithMany(cmp => cmp.ClubRolePolicies)
            .HasForeignKey(crp=> crp.ClubRoleId);
        modelBuilder.Entity<ClubRolePolicy>()
            .HasOne<Policy>(p => p.Policy)
            .WithMany(crp => crp.ClubRolePolicies)
            .HasForeignKey(crp => crp.PolicyId);
        modelBuilder.Entity<UserPolicy>()
            .HasOne<User>(cr => cr.User)
            .WithMany(cmp => cmp.UserPolicies)
            .HasForeignKey(crp => crp.UserId);
        modelBuilder.Entity<UserPolicy>()
            .HasOne<Policy>(p => p.Policy)
            .WithMany(crp => crp.UserPolicies)
            .HasForeignKey(crp => crp.PolicyId);
        modelBuilder.Entity<UserRolePolicy>()
            .HasOne<UserRole>(cr => cr.Role)
            .WithMany(cmp => cmp.UserRolePolicies)
            .HasForeignKey(crp => crp.RoleId);
        modelBuilder.Entity<UserRolePolicy>()
            .HasOne<Policy>(p => p.Policy)
            .WithMany(crp => crp.UserRolePolicies)
            .HasForeignKey(crp => crp.PolicyId);

        modelBuilder.Entity<Policy>()
            .HasOne<PolicyGroup>(pg => pg.PolicyGroup)
            .WithMany(p => p.Policies)
            .HasForeignKey(p => p.PolicyGroupId);
        modelBuilder.Entity<ClubRole>()
            .HasOne<Club>(c => c.Club)
            .WithMany(cr => cr.ClubRoles)
            .HasForeignKey(cr => cr.ClubId);
        modelBuilder.Entity<ClubCreationRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.Property(e => e.ClubName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.Reason)
                .HasMaxLength(1000);

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.AdminComment)
                .HasMaxLength(1000);
        });
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
