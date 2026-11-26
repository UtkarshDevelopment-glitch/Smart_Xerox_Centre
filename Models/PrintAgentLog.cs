using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class PrintAgentLog
{
    public long PrintAgentLogId { get; set; }

    public long PrintAgentId { get; set; }

    public string LogLevel { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public string? Message { get; set; }

    public DateTime LoggedAtUtc { get; set; }

    public virtual PrintAgent PrintAgent { get; set; } = null!;
}
