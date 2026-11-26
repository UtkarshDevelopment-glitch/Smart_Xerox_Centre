using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class Notification
{
    public long NotificationId { get; set; }

    public long TenantId { get; set; }

    public long? ShopId { get; set; }

    public long? UserId { get; set; }

    public long? CustomerId { get; set; }

    public string Channel { get; set; } = null!;

    public string Recipient { get; set; } = null!;

    public string? Subject { get; set; }

    public string Message { get; set; } = null!;

    public string NotificationStatus { get; set; } = null!;

    public string? ProviderReference { get; set; }

    public DateTime? SentAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Shop? Shop { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual User? User { get; set; }
}
