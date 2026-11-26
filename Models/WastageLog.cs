using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class WastageLog
{
    public long WastageLogId { get; set; }

    public long TenantId { get; set; }

    public long ShopId { get; set; }

    public long? OrderId { get; set; }

    public long? PrintJobId { get; set; }

    public string WastageType { get; set; } = null!;

    public int PagesWasted { get; set; }

    public decimal EstimatedCost { get; set; }

    public string? Reason { get; set; }

    public long? RecordedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual Order? Order { get; set; }

    public virtual PrintJob? PrintJob { get; set; }

    public virtual Shop Shop { get; set; } = null!;

    public virtual User? User { get; set; }
}
