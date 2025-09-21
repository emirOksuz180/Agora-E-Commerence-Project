using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class Role
{
    public short RoleId { get; set; }

    public string RoleName { get; set; } = null!;
}
