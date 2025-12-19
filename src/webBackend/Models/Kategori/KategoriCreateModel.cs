namespace webBackend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 


public class KategoriCreateModel
{
  [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Kategori Adı")]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    [StringLength(255)]
    [Display(Name = "URL")]
    [Required]
    public string Url { get; set; } = null!;

    [InverseProperty("Category")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}