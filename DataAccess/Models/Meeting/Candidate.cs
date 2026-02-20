using System;
using System.Collections.Generic;

namespace DataAccess.Models.Meeting;

/// <summary>
/// Ứng viên được mời phỏng vấn.
/// </summary>
public class Candidate
{
    public int      Id          { get; set; }
    public string   FullName    { get; set; } = null!;
    public string   Email       { get; set; } = null!;
    public string?  PhoneNumber { get; set; }
    public string?  ResumeUrl   { get; set; }
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<InterviewSchedule> InterviewSchedules { get; set; } = new List<InterviewSchedule>();
}
