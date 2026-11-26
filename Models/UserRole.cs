using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class UserRole
{
    public long UserId { get; set; }

    public int RoleId { get; set; }

    public DateTime AssignedAtUtc { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
