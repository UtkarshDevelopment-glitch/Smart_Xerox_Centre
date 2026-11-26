using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class PrintSetting
{
    public long PrintSettingId { get; set; }

    public long OrderFileId { get; set; }

    public long? PrinterId { get; set; }

    public string ColorMode { get; set; } = null!;

    public string Sides { get; set; } = null!;

    public string PaperSize { get; set; } = null!;

    public string Orientation { get; set; } = null!;

    public int Copies { get; set; }

    public string? PageRange { get; set; }

    public int PagesToPrint { get; set; }

    public decimal RatePerPage { get; set; }

    public decimal LineTotal { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual OrderFile OrderFile { get; set; } = null!;

    public virtual Printer? Printer { get; set; }
}
