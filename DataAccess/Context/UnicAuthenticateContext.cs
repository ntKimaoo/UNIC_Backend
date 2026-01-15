using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Models;

public partial class UnicAuthenticateContext : DbContext
{
    public UnicAuthenticateContext()
    {
    }

    public UnicAuthenticateContext(DbContextOptions<UnicAuthenticateContext> options)
        : base(options)
    {
    }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public virtual DbSet<Member> Members { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-1LCJCGH\\MSSQLSERVER02;Database=UNIC_Authenticate;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasKey(e => e.EmailVerificationTokenId).HasName("PK__EmailVer__B16196D29A5849AE");

            entity.HasIndex(e => e.MemberId, "IX_EmailVerificationTokens_MemberId");

            entity.HasIndex(e => e.TokenHash, "IX_EmailVerificationTokens_TokenHash");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsUsed).HasDefaultValue(false);
            entity.Property(e => e.TokenHash).HasMaxLength(255);

            entity.HasOne(d => d.Member).WithMany(p => p.EmailVerificationTokens)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("FK__EmailVeri__Membe__45F365D3");
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.MemberId).HasName("PK__Members__0CF04B38FFE70BBA");

            entity.HasIndex(e => e.Email, "UQ__Members__A9D105345C1FCA58").IsUnique();

            entity.Property(e => e.MemberId).HasColumnName("MemberID");
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

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.PasswordResetTokenId).HasName("PK__Password__160661284C508CB5");

            entity.HasIndex(e => e.MemberId, "IX_PasswordResetTokens_MemberId");

            entity.HasIndex(e => e.TokenHash, "IX_PasswordResetTokens_TokenHash");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsUsed).HasDefaultValue(false);
            entity.Property(e => e.TokenHash).HasMaxLength(255);

            entity.HasOne(d => d.Member).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("FK__PasswordR__Membe__412EB0B6");
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
            entity.Property(e => e.MemberId).HasColumnName("MemberID");
            entity.Property(e => e.RevokedAt).HasColumnType("datetime");
            entity.Property(e => e.TokenHash).HasMaxLength(500);

            entity.HasOne(d => d.Member).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RefreshTo__Membe__3C69FB99");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
