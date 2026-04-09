using Microsoft.AspNetCore.Identity;

namespace webBackend.Models;

public class AppUser : IdentityUser<int>
{
    public string AdSoyad { get; set; } = null!;

    public virtual ICollection<UserActionPermission> UserActionPermissions { get; set; } = new List<UserActionPermission>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
}