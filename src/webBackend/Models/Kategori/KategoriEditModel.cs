namespace webBackend.Models;

public class KategoriEditModel 

{
  [Display(Name = "Kategori Adı")]

  public string KategoriAdi {get; set;} = null!;

  [Display(Name = "URL")]

  public string Url {get; set;} = null!;
}