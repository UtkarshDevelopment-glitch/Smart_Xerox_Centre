using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class User
{
    public long UserId { get; set; }

    public long TenantId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string PasswordHash { get; set; } = null!;

    public bool IsEmailVerified { get; set; }

    public bool IsPhoneVerified { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? LastLoginAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<CollectionLog> CollectionLogs { get; set; } = new List<CollectionLog>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PrintToken> PrintTokens { get; set; } = new List<PrintToken>();

    public virtual ICollection<ShopExpense> ShopExpenses { get; set; } = new List<ShopExpense>();

    public virtual ICollection<ShopUser> ShopUsers { get; set; } = new List<ShopUser>();

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<WastageLog> WastageLogs { get; set; } = new List<WastageLog>();
}
