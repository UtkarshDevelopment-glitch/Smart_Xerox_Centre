using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class PrintJob
{
    public long PrintJobId { get; set; }

    public long TenantId { get; set; }

    public long ShopId { get; set; }

    public long OrderId { get; set; }

    public long? PrinterId { get; set; }

    public long? PrintAgentId { get; set; }

    public Guid JobGuid { get; set; }

    public string JobStatus { get; set; } = null!;

    public int Priority { get; set; }

    public DateTime QueuedAtUtc { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public string? LastError { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual PrintAgent? PrintAgent { get; set; }

    public virtual ICollection<PrintJobAttempt> PrintJobAttempts { get; set; } = new List<PrintJobAttempt>();

    public virtual ICollection<PrintJobStatus> PrintJobStatuses { get; set; } = new List<PrintJobStatus>();

    public virtual ICollection<PrintLog> PrintLogs { get; set; } = new List<PrintLog>();

    public virtual Printer? Printer { get; set; }

    public virtual ICollection<WastageLog> WastageLogs { get; set; } = new List<WastageLog>();
}
