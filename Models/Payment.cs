using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class Payment
{
    public long PaymentId { get; set; }

    public long TenantId { get; set; }

    public long ShopId { get; set; }

    public long OrderId { get; set; }

    public string GatewayName { get; set; } = null!;

    public string? GatewayOrderId { get; set; }

    public string? GatewayPaymentId { get; set; }

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public string? PaymentMethod { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public bool GatewaySignatureVerified { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}
