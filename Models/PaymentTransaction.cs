using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class PaymentTransaction
{
    public long PaymentTransactionId { get; set; }

    public long PaymentId { get; set; }

    public string? GatewayEventId { get; set; }

    public string EventType { get; set; } = null!;

    public string? GatewayStatus { get; set; }

    public string? PayloadJson { get; set; }

    public bool SignatureValid { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public DateTime ReceivedAtUtc { get; set; }

    public virtual Payment Payment { get; set; } = null!;
}
