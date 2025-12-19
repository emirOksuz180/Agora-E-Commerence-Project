using System.ComponentModel.DataAnnotations;

namespace webBackend.Models;

public class UrunModel
{
  [Display(Name = "Ürün Adı")]
  [Required(ErrorMessage = "Ürün adı girmelisiniz.")]
  [StringLength(50, ErrorMessage = "{0} için  {2}-{1} karekter aralığında değer girmelisiniz.", MinimumLength = 10)]
  public string ProductName { get; set; } = null!;



  [Display(Name = "Ürün Fiyatı")]
  [Required(ErrorMessage = "{0} zorunlu.")]
  [Range(0, 1000000, ErrorMessage = "{0} , için girdiğiniz fiyat {1} ile {2} arasında olmalıdır")]
  public decimal? Price { get; set; } = 0m;


  [StringLength(255)]
  public string? ImageUrl { get; set; }

  [Display(Name = "Ürün Resmi")]
  public IFormFile? ImageFile { get; set; }

  [Display(Name = "Ürün aktif mi ?")]
  public bool IsActive { get; set; }

  public bool AnaSayfa { get; set; }


  [Display(Name = "Kategori")]
  [Required(ErrorMessage = "{0} zorunlu.")]
  public int? CategoryId { get; set; }

  [StringLength(500)]
  public string? ProductDescription { get; set; }
}