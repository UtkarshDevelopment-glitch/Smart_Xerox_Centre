using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class Order
{
    public long OrderId { get; set; }

    public long TenantId { get; set; }

    public long ShopId { get; set; }

    public Guid? SessionId { get; set; }

    public long? CustomerId { get; set; }

    public string OrderNumber { get; set; } = null!;

    public string OrderStatus { get; set; } = null!;

    public string CurrencyCode { get; set; } = null!;

    public decimal SubTotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal AdditionalCharges { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string? CustomerNote { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<CollectionLog> CollectionLogs { get; set; } = new List<CollectionLog>();

    public virtual Customer? Customer { get; set; }

    public virtual CustomerSession? CustomerSession { get; set; }

    public virtual ICollection<OrderFile> OrderFiles { get; set; } = new List<OrderFile>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<PrintJob> PrintJobs { get; set; } = new List<PrintJob>();

    public virtual ICollection<PrintToken> PrintTokens { get; set; } = new List<PrintToken>();

    public virtual Shop Shop { get; set; } = null!;

    public virtual ICollection<WastageLog> WastageLogs { get; set; } = new List<WastageLog>();
}
