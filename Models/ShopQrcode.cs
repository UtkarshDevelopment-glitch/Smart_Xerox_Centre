using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class ShopQrcode
{
    public long QrcodeId { get; set; }

    public long TenantId { get; set; }

    public long ShopId { get; set; }

    public string QrcodeValue { get; set; } = null!;

    public string? QrimagePath { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? DeactivatedAtUtc { get; set; }

    public virtual ICollection<CustomerSession> CustomerSessions { get; set; } = new List<CustomerSession>();

    public virtual Shop Shop { get; set; } = null!;
}
