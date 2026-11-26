using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class PrintAgent
{
    public long PrintAgentId { get; set; }

    public long TenantId { get; set; }

    public long ShopId { get; set; }

    public string AgentName { get; set; } = null!;

    public string? MachineName { get; set; }

    public string AgentKeyHash { get; set; } = null!;

    public string? AgentVersion { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? LastHeartbeatAtUtc { get; set; }

    public DateTime RegisteredAtUtc { get; set; }

    public virtual ICollection<PrintAgentLog> PrintAgentLogs { get; set; } = new List<PrintAgentLog>();

    public virtual ICollection<PrintJobAttempt> PrintJobAttempts { get; set; } = new List<PrintJobAttempt>();

    public virtual ICollection<PrintJob> PrintJobs { get; set; } = new List<PrintJob>();

    public virtual Shop Shop { get; set; } = null!;
}
