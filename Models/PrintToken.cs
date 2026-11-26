using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class PrintToken
{
    public long PrintTokenId { get; set; }

    public long TenantId { get; set; }

    public long ShopId { get; set; }

    public long OrderId { get; set; }

    public string TokenHash { get; set; } = null!;

    public string TokenDisplay { get; set; } = null!;

    public DateTime GeneratedAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public DateTime? UsedAtUtc { get; set; }

    public long? UsedByUserId { get; set; }

    public bool IsRevoked { get; set; }

    public virtual CollectionLog? CollectionLog { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Shop Shop { get; set; } = null!;

    public virtual User? User { get; set; }
}
