using Microsoft.AspNetCore.Identity;

namespace webBackend.Models;

public class AppRole : IdentityRole<int>
{
  public virtual ICollection<RoleActionPermission> RoleActionPermissions { get; set; } = new List<RoleActionPermission>();
}