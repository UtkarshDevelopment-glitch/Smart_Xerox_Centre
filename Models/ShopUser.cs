using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class ShopUser
{
    public long ShopUserId { get; set; }

    public long TenantId { get; set; }

    public long ShopId { get; set; }

    public long UserId { get; set; }

    public bool IsPrimaryShop { get; set; }

    public string Status { get; set; } = null!;

    public DateTime AssignedAtUtc { get; set; }

    public virtual Shop Shop { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
