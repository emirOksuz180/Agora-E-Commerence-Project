namespace webBackend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using webBackend.Validation; 


public class KategoriCreateModel
{
  [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Kategori adı boş bırakılamaz.")]
    [StringLength(50, ErrorMessage = "Kategori adı SEO ve UI uyumu için en fazla 50 karakter olmalıdır.")]
    [Display(Name = "Kategori Adı")]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    [Required(ErrorMessage = "URL alanı zorunludur.")]
    [StringLength(100, ErrorMessage = "URL yapısı en fazla 100 karakter olabilir.")]
    [Display(Name = "URL")]
    [UrlSlug]
    public string Url { get; set; } = null!;

    [InverseProperty("Category")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}