using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class ActionPermission
{
    public int Id { get; set; }

    public string ControllerName { get; set; } = null!;

    public string ActionName { get; set; } = null!;

    public string? Description { get; set; }

    public string? Category { get; set; }

    public virtual ICollection<RoleActionPermission> RoleActionPermissions { get; set; } = new List<RoleActionPermission>();

    public virtual ICollection<UserActionPermission> UserActionPermissions { get; set; } = new List<UserActionPermission>();
}
