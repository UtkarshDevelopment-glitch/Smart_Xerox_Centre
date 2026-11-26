using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class Refund
{
    public long RefundId { get; set; }

    public long PaymentId { get; set; }

    public string? GatewayRefundId { get; set; }

    public decimal RefundAmount { get; set; }

    public string? Reason { get; set; }

    public string RefundStatus { get; set; } = null!;

    public DateTime RequestedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public virtual Payment Payment { get; set; } = null!;
}
