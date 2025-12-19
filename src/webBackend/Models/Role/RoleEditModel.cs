namespace webBackend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 


public class RoleEditModel
{
  [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Role Adı")]
    public string RoleAdi { get; set; } = null!;

    


}