using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class PrintJobStatus
{
    public long PrintJobStatusId { get; set; }

    public long PrintJobId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public string ChangedByType { get; set; } = null!;

    public long? ChangedById { get; set; }

    public string? Remarks { get; set; }

    public DateTime ChangedAtUtc { get; set; }

    public virtual PrintJob PrintJob { get; set; } = null!;
}
