using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class ShopExpense
{
    public long ShopExpenseId { get; set; }

    public long TenantId { get; set; }

    public long ShopId { get; set; }

    public string ExpenseCategory { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Amount { get; set; }

    public DateOnly ExpenseDate { get; set; }

    public long? RecordedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual Shop Shop { get; set; } = null!;

    public virtual User? User { get; set; }
}
