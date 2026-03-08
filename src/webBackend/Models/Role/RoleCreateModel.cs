namespace webBackend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 


public class RoleCreateModel
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Role adı boş bırakılamaz.")]
    [StringLength(30, ErrorMessage = "Role adı  en fazla 30 karakter olmalıdır.")]
    [Display(Name = "Role Adı")]
    public string RoleAdi { get; set; } = null!;    

    


}