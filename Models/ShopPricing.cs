using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class ShopPricing
{
    public long ShopPricingId { get; set; }

    public long TenantId { get; set; }

    public long ShopId { get; set; }

    public string ColorMode { get; set; } = null!;

    public string Sides { get; set; } = null!;

    public string PaperSize { get; set; } = null!;

    public decimal RatePerPage { get; set; }

    public decimal AdditionalCharge { get; set; }

    public decimal TaxRate { get; set; }

    public DateTime EffectiveFromUtc { get; set; }

    public DateTime? EffectiveToUtc { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual Shop Shop { get; set; } = null!;
}
