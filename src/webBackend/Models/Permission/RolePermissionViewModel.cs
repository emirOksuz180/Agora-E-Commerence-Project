using System.ComponentModel.DataAnnotations;
using webBackend.Models.Permissons;

namespace webBackend.Models
{
    public class RolePermissionViewModel
    {

        
        public List<AppRole> Roles { get; set; } = new List<AppRole>();
        public List<PermissionItemViewModel> Permissions { get; set; } = new();

        public int RoleId { get; set; }
        
        [Required(ErrorMessage = "Rol adı zorunludur.")]
        [Display(Name = "Role Adı")]
        public string RoleAdi { get; set; } = null!; 
    }
}