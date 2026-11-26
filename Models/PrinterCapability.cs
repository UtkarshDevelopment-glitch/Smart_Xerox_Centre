using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class PrinterCapability
{
    public long PrinterCapabilityId { get; set; }

    public long PrinterId { get; set; }

    public string PaperSize { get; set; } = null!;

    public bool SupportsColor { get; set; }

    public bool SupportsBlackWhite { get; set; }

    public bool SupportsDuplex { get; set; }

    public bool IsActive { get; set; }

    public virtual Printer Printer { get; set; } = null!;
}
