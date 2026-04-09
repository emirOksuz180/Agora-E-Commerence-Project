using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class AppPermission
{
    public int Id { get; set; }

    public string PermissionKey { get; set; } = null!;

    public string? Description { get; set; }

    public string? GroupName { get; set; }
}
