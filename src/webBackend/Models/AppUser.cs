using Microsoft.AspNetCore.Identity;

namespace webBackend.Models;

public class AppUser : IdentityUser<int>
{
    public string AdSoyad { get; set; } = null!;
}