using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class Printer
{
    public long PrinterId { get; set; }

    public long TenantId { get; set; }

    public long ShopId { get; set; }

    public string PrinterName { get; set; } = null!;

    public string? PrinterModel { get; set; }

    public string? WindowsPrinterName { get; set; }

    public bool IsDefault { get; set; }

    public string Status { get; set; } = null!;

    public string? LastStatus { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public virtual ICollection<PrintJob> PrintJobs { get; set; } = new List<PrintJob>();

    public virtual ICollection<PrintSetting> PrintSettings { get; set; } = new List<PrintSetting>();

    public virtual ICollection<PrinterCapability> PrinterCapabilities { get; set; } = new List<PrinterCapability>();

    public virtual Shop Shop { get; set; } = null!;
}
