using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class PrintJobAttempt
{
    public long PrintJobAttemptId { get; set; }

    public long PrintJobId { get; set; }

    public int AttemptNumber { get; set; }

    public long? PrintAgentId { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime? EndedAtUtc { get; set; }

    public string AttemptStatus { get; set; } = null!;

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? WindowsSpoolerJobId { get; set; }

    public virtual PrintAgent? PrintAgent { get; set; }

    public virtual PrintJob PrintJob { get; set; } = null!;
}
