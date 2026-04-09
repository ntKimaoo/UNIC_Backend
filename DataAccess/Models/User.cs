using System;
using System.Collections.Generic;
using UNIC.DataAccess.Models;

namespace DataAccess.Models;

public class User
{
    public Guid UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Address { get; set; }

    public string? Avatar { get; set; }

    public string? StudentId { get; set; }

    public string? Major { get; set; }

    public DateOnly? JoinDate { get; set; }

    public string? Status { get; set; }

    public string? PasswordHash { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();

    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<UserRole> UserRoles { get; set; }
    public virtual ICollection<UserClubRole> ClubMembers { get; set; }
    public virtual ICollection<ClubPost> ClubPosts { get; set; }
    public virtual ICollection<Notification> Notifications { get; set; }
    public virtual ICollection<Attendance> Attendances { get; set; }
    public virtual ICollection<Application> Applications { get; set; }
}
