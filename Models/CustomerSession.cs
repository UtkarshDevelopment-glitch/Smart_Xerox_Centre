using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class CustomerSession
{
    public Guid SessionId { get; set; }

    public long TenantId { get; set; }

    public long ShopId { get; set; }

    public long? CustomerId { get; set; }

    public long? QrcodeId { get; set; }

    public string SessionTokenHash { get; set; } = null!;

    public DateTime StartedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? EndedAtUtc { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ShopQrcode? Qrcode { get; set; }

    public virtual Shop Shop { get; set; } = null!;
}
