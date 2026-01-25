using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class RefreshToken
{
    public int RefreshTokenId { get; set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public string? DeviceInfo { get; set; }

    public string? Ipaddress { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool? IsRevoked { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
