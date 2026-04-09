using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class RoleActionPermission
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public int PermissionId { get; set; }

    public virtual ActionPermission Permission { get; set; } = null!;

    public virtual AppRole Role { get; set; } = null!;
}
