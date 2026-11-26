using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class CollectionLog
{
    public long CollectionLogId { get; set; }

    public long TenantId { get; set; }

    public long ShopId { get; set; }

    public long OrderId { get; set; }

    public long PrintTokenId { get; set; }

    public long StaffUserId { get; set; }

    public string CollectionStatus { get; set; } = null!;

    public string? Remarks { get; set; }

    public DateTime CollectedAtUtc { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual PrintToken PrintToken { get; set; } = null!;

    public virtual Shop Shop { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
