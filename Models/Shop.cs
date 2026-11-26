using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class Shop
{
    public long ShopId { get; set; }

    public long TenantId { get; set; }

    public string ShopCode { get; set; } = null!;

    public string ShopName { get; set; } = null!;

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string Country { get; set; } = null!;

    public string? ContactPerson { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string TimeZone { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<CollectionLog> CollectionLogs { get; set; } = new List<CollectionLog>();

    public virtual ICollection<CustomerSession> CustomerSessions { get; set; } = new List<CustomerSession>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<PrintAgent> PrintAgents { get; set; } = new List<PrintAgent>();

    public virtual ICollection<PrintToken> PrintTokens { get; set; } = new List<PrintToken>();

    public virtual ICollection<Printer> Printers { get; set; } = new List<Printer>();

    public virtual ICollection<ShopExpense> ShopExpenses { get; set; } = new List<ShopExpense>();

    public virtual ICollection<ShopPricing> ShopPricings { get; set; } = new List<ShopPricing>();

    public virtual ICollection<ShopQrcode> ShopQrcodes { get; set; } = new List<ShopQrcode>();

    public virtual ICollection<ShopUser> ShopUsers { get; set; } = new List<ShopUser>();

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual ICollection<WastageLog> WastageLogs { get; set; } = new List<WastageLog>();
}
