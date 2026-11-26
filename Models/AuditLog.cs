using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class AuditLog
{
    public long AuditLogId { get; set; }

    public long? TenantId { get; set; }

    public long? ShopId { get; set; }

    public long? UserId { get; set; }

    public string EntityName { get; set; } = null!;

    public string? EntityId { get; set; }

    public string ActionName { get; set; } = null!;

    public string? OldValuesJson { get; set; }

    public string? NewValuesJson { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual Shop? Shop { get; set; }

    public virtual Tenant? Tenant { get; set; }

    public virtual User? User { get; set; }
}
