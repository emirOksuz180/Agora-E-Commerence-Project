using webBackend.Models.Permissons;

namespace webBackend.Models
{
    public class RolePermissionViewModel
    {
        public List<AppRole> Roles { get; set; } = new List<AppRole>();
        public List<PermissionItemViewModel> Permissions { get; set; } = new();
    }
}