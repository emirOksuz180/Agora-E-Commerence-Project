using System.ComponentModel.DataAnnotations;

namespace webBackend.Models;

public class KategoriEditModel

{
  [Display(Name = "Kategori Id")]

  public int Id { get; set; }


  [Display(Name = "Kategori Adı")]
  [Required]
  public string Name {get; set;} = null!;

  [Display(Name = "URL")]
  [Required]
  public string Url {get; set;} = null!;
}