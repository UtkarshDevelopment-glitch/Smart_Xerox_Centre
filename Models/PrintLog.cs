using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class PrintLog
{
    public long PrintLogId { get; set; }

    public long PrintJobId { get; set; }

    public string LogLevel { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime LoggedAtUtc { get; set; }

    public virtual PrintJob PrintJob { get; set; } = null!;
}
