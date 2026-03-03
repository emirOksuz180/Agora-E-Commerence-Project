using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace webBackend.Models;

public partial class AppPermission
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string PermissionKey { get; set; } = null!;

    public string? Description { get; set; }

    public string? GroupName { get; set; }
}
