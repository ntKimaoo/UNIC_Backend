using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class EmailVerificationToken
{
    public int EmailVerificationTokenId { get; set; }

    public int MemberId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool? IsUsed { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public virtual Member Member { get; set; } = null!;
}
