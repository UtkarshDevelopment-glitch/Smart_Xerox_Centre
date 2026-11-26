using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class Customer
{
    public long CustomerId { get; set; }

    public long TenantId { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public virtual ICollection<CustomerSession> CustomerSessions { get; set; } = new List<CustomerSession>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Tenant Tenant { get; set; } = null!;
}
