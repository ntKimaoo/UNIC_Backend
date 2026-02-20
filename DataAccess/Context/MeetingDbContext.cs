using Microsoft.EntityFrameworkCore;
using DataAccess.Models.Meeting;
using DataAccess.Models.Meeting.Enums;

namespace DataAccess.Context;

public class MeetingDbContext : DbContext
{
    public MeetingDbContext(DbContextOptions<MeetingDbContext> options)
        : base(options) { }

    public DbSet<Candidate>           Candidates           { get; set; }
    public DbSet<InterviewSchedule>   InterviewSchedules   { get; set; }
    public DbSet<InterviewAssignment> InterviewAssignments { get; set; }
    public DbSet<MeetingRoom>         MeetingRooms         { get; set; }
    public DbSet<RoomParticipant>     RoomParticipants     { get; set; }
    public DbSet<RoomEvent>           RoomEvents           { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Candidate>(e =>
        {
            e.ToTable("Candidate");
            e.HasKey(c => c.Id);
            e.Property(c => c.FullName).IsRequired().HasMaxLength(200);
            e.Property(c => c.Email).IsRequired().HasMaxLength(256);
            e.HasIndex(c => c.Email).IsUnique();
        });

        modelBuilder.Entity<InterviewSchedule>(e =>
        {
            e.ToTable("InterviewSchedule");
            e.HasKey(s => s.Id);
            e.Property(s => s.Title).IsRequired().HasMaxLength(300);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(50);
            e.Property(s => s.CreatedByUserId).IsRequired().HasMaxLength(36);
            e.HasIndex(s => s.CreatedByUserId);

            e.HasOne(s => s.Candidate)
                .WithMany(c => c.InterviewSchedules)
                .HasForeignKey(s => s.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InterviewAssignment>(e =>
        {
            e.ToTable("InterviewAssignment");
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.InterviewScheduleId, a.InterviewerUserId }).IsUnique();
            e.Property(a => a.Role).HasConversion<string>().HasMaxLength(50);
            e.Property(a => a.Result).HasConversion<string>().HasMaxLength(50);
            e.Property(a => a.InterviewerUserId).IsRequired().HasMaxLength(36);
            e.HasIndex(a => a.InterviewerUserId);

            e.HasOne(a => a.InterviewSchedule)
                .WithMany(s => s.Assignments)
                .HasForeignKey(a => a.InterviewScheduleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MeetingRoom>(e =>
        {
            e.ToTable("MeetingRoom");
            e.HasKey(r => r.Id);
            e.Property(r => r.RoomCode).IsRequired().HasMaxLength(20);
            e.HasIndex(r => r.RoomCode).IsUnique();   // Dùng để lookup /room/{code}

            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(50);
            e.Property(r => r.StunServerUri).HasMaxLength(500);
            e.Property(r => r.TurnServerUri).HasMaxLength(500);
            e.Property(r => r.TurnUsername).HasMaxLength(200);
            e.Property(r => r.TurnCredential).HasMaxLength(200);

            // 1-1 với InterviewSchedule
            e.HasOne(r => r.InterviewSchedule)
                .WithOne(s => s.MeetingRoom)
                .HasForeignKey<MeetingRoom>(r => r.InterviewScheduleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomParticipant>(e =>
        {
            e.ToTable("RoomParticipant");
            e.HasKey(p => p.Id);
            e.Property(p => p.ConnectionState).HasConversion<string>().HasMaxLength(50);
            e.Property(p => p.DisplayName).IsRequired().HasMaxLength(200);
            e.Property(p => p.UserId).HasMaxLength(36);
            e.Property(p => p.PeerId).HasMaxLength(100);

            e.HasIndex(p => p.UserId);
            e.HasIndex(p => p.CandidateId);

            e.HasOne(p => p.MeetingRoom)
                .WithMany(r => r.Participants)
                .HasForeignKey(p => p.MeetingRoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomEvent>(e =>
        {
            e.ToTable("RoomEvent");
            e.HasKey(ev => ev.Id);
            e.Property(ev => ev.EventType).IsRequired().HasMaxLength(100);
            e.Property(ev => ev.ActorId).HasMaxLength(36);
            e.Property(ev => ev.Payload).HasColumnType("nvarchar(max)");
            e.HasIndex(ev => ev.OccurredAt);

            e.HasOne(ev => ev.MeetingRoom)
                .WithMany(r => r.Events)
                .HasForeignKey(ev => ev.MeetingRoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
