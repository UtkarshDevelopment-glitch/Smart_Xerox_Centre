using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class Role
{
    public int RoleId { get; set; }

    public long? TenantId { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual Tenant? Tenant { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
