using Microsoft.EntityFrameworkCore;
using DataAccess.Models.Meeting;
using DataAccess.Models.Meeting.Enums;

namespace DataAccess.Context;

public class MeetingDbContext : DbContext
{
    public MeetingDbContext(DbContextOptions<MeetingDbContext> options)
        : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }


    public DbSet<InterviewSchedule>   InterviewSchedules   { get; set; }
    public DbSet<InterviewAssignment> InterviewAssignments { get; set; }
    public DbSet<MeetingRoom>         MeetingRooms         { get; set; }
    public DbSet<RoomParticipant>     RoomParticipants     { get; set; }
    public DbSet<RoomEvent>           RoomEvents           { get; set; }
    public DbSet<EvaluationCriterion> EvaluationCriteria   { get; set; }
    public DbSet<CriteriaScore>       CriteriaScores       { get; set; }
    public DbSet<CampaignDecision>    CampaignDecisions    { get; set; }
    public DbSet<ProposedTimeSlot>   ProposedTimeSlots    { get; set; }
    public DbSet<AiCandidateAnalysisResult> AiCandidateAnalysisResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── InterviewSchedule ─────────────────────────────────────
        modelBuilder.Entity<InterviewSchedule>(e =>
        {
            e.ToTable("InterviewSchedules");
            e.HasKey(s => s.Id);

            e.Property(s => s.Title).IsRequired().HasMaxLength(300);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(50);

            // FK mềm – Guid columns, không tạo FK constraint thật
            e.Property(s => s.ApplicationId).IsRequired();
            e.Property(s => s.CandidateUserId).IsRequired();
            e.Property(s => s.CampaignId).IsRequired();
            e.Property(s => s.CreatedByUserId).IsRequired();

            e.HasIndex(s => s.ApplicationId);
            e.HasIndex(s => s.CandidateUserId);
            e.HasIndex(s => s.CampaignId);
            e.HasIndex(s => s.CreatedByUserId);
        });

        // ── ProposedTimeSlot ─────────────────────────────────────
        modelBuilder.Entity<ProposedTimeSlot>(e =>
        {
            e.ToTable("ProposedTimeSlots");
            e.HasKey(t => t.Id);

            e.Property(t => t.ProposedAt).IsRequired();
            e.Property(t => t.IsSelected).HasDefaultValue(false);

            e.HasIndex(t => t.InterviewScheduleId);

            e.HasOne(t => t.InterviewSchedule)
             .WithMany(s => s.ProposedTimeSlots)
             .HasForeignKey(t => t.InterviewScheduleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── InterviewAssignment ───────────────────────────────────
        modelBuilder.Entity<InterviewAssignment>(e =>
        {
            e.ToTable("InterviewAssignments");
            e.HasKey(a => a.Id);

            // Unique: 1 interviewer chỉ được assign 1 lần vào 1 lịch
            e.HasIndex(a => new { a.InterviewScheduleId, a.InterviewerUserId }).IsUnique();

            e.Property(a => a.Role).HasConversion<string>().HasMaxLength(50);
            e.Property(a => a.Result).HasConversion<string>().HasMaxLength(50);
            e.Property(a => a.InterviewerUserId).IsRequired();
            e.HasIndex(a => a.InterviewerUserId);

            e.HasOne(a => a.InterviewSchedule)
             .WithMany(s => s.Assignments)
             .HasForeignKey(a => a.InterviewScheduleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── MeetingRoom ───────────────────────────────────────────
        modelBuilder.Entity<MeetingRoom>(e =>
        {
            e.ToTable("MeetingRooms");
            e.HasKey(r => r.Id);

            // Phân loại & thông tin chung
            e.Property(r => r.RoomType).HasConversion<string>().HasMaxLength(50);
            e.Property(r => r.Title).IsRequired().HasMaxLength(300);
            e.Property(r => r.Description).HasMaxLength(2000);
            e.Property(r => r.CreatedByUserId).IsRequired();
            e.HasIndex(r => r.CreatedByUserId);

            e.Property(r => r.RoomCode).IsRequired().HasMaxLength(20);
            e.HasIndex(r => r.RoomCode).IsUnique();          // Lookup /room/{code}

            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(50);
            e.Property(r => r.StunServerUri).HasMaxLength(500);
            e.Property(r => r.TurnServerUri).HasMaxLength(500);
            e.Property(r => r.TurnUsername).HasMaxLength(200);
            e.Property(r => r.TurnCredential).HasMaxLength(200);

            // 1-0..1 với InterviewSchedule (optional)
            e.HasOne(r => r.InterviewSchedule)
             .WithOne(s => s.MeetingRoom)
             .HasForeignKey<MeetingRoom>(r => r.InterviewScheduleId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── RoomParticipant ───────────────────────────────────────
        modelBuilder.Entity<RoomParticipant>(e =>
        {
            e.ToTable("RoomParticipants");
            e.HasKey(p => p.Id);

            e.Property(p => p.UserId).IsRequired();
            e.Property(p => p.DisplayName).IsRequired().HasMaxLength(200);
            e.Property(p => p.Role).HasMaxLength(50);
            e.Property(p => p.PeerId).HasMaxLength(100);
            e.Property(p => p.ConnectionState).HasConversion<string>().HasMaxLength(50);

            e.HasIndex(p => p.UserId);

            e.HasOne(p => p.MeetingRoom)
             .WithMany(r => r.Participants)
             .HasForeignKey(p => p.MeetingRoomId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── RoomEvent ─────────────────────────────────────────────
        modelBuilder.Entity<RoomEvent>(e =>
        {
            e.ToTable("RoomEvents");
            e.HasKey(ev => ev.Id);

            e.Property(ev => ev.EventType).IsRequired().HasMaxLength(100);
            e.Property(ev => ev.Payload).HasColumnType("nvarchar(max)");
            e.HasIndex(ev => ev.OccurredAt);

            e.HasOne(ev => ev.MeetingRoom)
             .WithMany(r => r.Events)
             .HasForeignKey(ev => ev.MeetingRoomId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── EvaluationCriterion ───────────────────────────────────
        modelBuilder.Entity<EvaluationCriterion>(e =>
        {
            e.ToTable("EvaluationCriteria");
            e.HasKey(c => c.Id);

            e.Property(c => c.Name).IsRequired().HasMaxLength(200);
            e.Property(c => c.Description).HasMaxLength(1000);
            e.Property(c => c.CampaignId).IsRequired();

            e.HasIndex(c => c.CampaignId);
        });

        // ── CriteriaScore ─────────────────────────────────────────
        modelBuilder.Entity<CriteriaScore>(e =>
        {
            e.ToTable("CriteriaScores");
            e.HasKey(cs => cs.Id);

            // Unique: 1 interviewer chỉ chấm 1 lần cho 1 tiêu chí
            e.HasIndex(cs => new { cs.InterviewAssignmentId, cs.EvaluationCriterionId }).IsUnique();

            e.Property(cs => cs.Note).HasMaxLength(1000);

            e.HasOne(cs => cs.InterviewAssignment)
             .WithMany(a => a.CriteriaScores)
             .HasForeignKey(cs => cs.InterviewAssignmentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(cs => cs.EvaluationCriterion)
             .WithMany(c => c.CriteriaScores)
             .HasForeignKey(cs => cs.EvaluationCriterionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── CampaignDecision ──────────────────────────────────────
        modelBuilder.Entity<CampaignDecision>(e =>
        {
            e.ToTable("CampaignDecisions");
            e.HasKey(d => d.Id);

            // Unique: 1 ứng viên chỉ có 1 quyết định trong 1 campaign
            e.HasIndex(d => new { d.CampaignId, d.CandidateUserId }).IsUnique();

            e.Property(d => d.Decision).HasConversion<string>().HasMaxLength(50);
            e.Property(d => d.PublishStatus).HasConversion<string>().HasMaxLength(50);
            e.Property(d => d.NotificationChannels).HasMaxLength(200);

            e.Property(d => d.CampaignId).IsRequired();
            e.Property(d => d.CandidateUserId).IsRequired();
            e.Property(d => d.DecidedByUserId).IsRequired();

            e.HasIndex(d => d.CampaignId);

            e.HasOne(d => d.InterviewSchedule)
             .WithMany()
             .HasForeignKey(d => d.InterviewScheduleId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── AiCandidateAnalysisResult ──────────────────────────────
        modelBuilder.Entity<AiCandidateAnalysisResult>(e =>
        {
            e.ToTable("AiCandidateAnalysisResults");
            e.HasKey(a => a.Id);

            // Unique per schedule
            e.HasIndex(a => a.InterviewScheduleId).IsUnique();
            e.HasIndex(a => a.CampaignId);

            e.Property(a => a.Result).HasMaxLength(50);
            e.Property(a => a.CriteriaEvaluationsJson).HasColumnType("nvarchar(max)");
            e.Property(a => a.StrengthsJson).HasColumnType("nvarchar(max)");
            e.Property(a => a.WeaknessesJson).HasColumnType("nvarchar(max)");

            e.HasOne(a => a.InterviewSchedule)
             .WithMany()
             .HasForeignKey(a => a.InterviewScheduleId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

