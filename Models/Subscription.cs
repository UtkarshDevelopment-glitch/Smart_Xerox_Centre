using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class Subscription
{
    public long SubscriptionId { get; set; }

    public long TenantId { get; set; }

    public long? ShopId { get; set; }

    public string PlanName { get; set; } = null!;

    public string BillingCycle { get; set; } = null!;

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string SubscriptionStatus { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }

    public virtual Shop? Shop { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;
}
