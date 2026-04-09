using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class UserActionPermission
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int PermissionId { get; set; }

    public bool IsAllowed { get; set; }

    public virtual ActionPermission Permission { get; set; } = null!;

    public virtual AppUser User { get; set; } = null!;
}
