using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class SuperAdmin
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public byte[] PasswordHash { get; set; } = null!;
}
