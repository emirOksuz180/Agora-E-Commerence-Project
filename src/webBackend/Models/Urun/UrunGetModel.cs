namespace webBackend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



public class UrunGetModel
{
  [Key]
  public int ProductId { get; set; }

  [StringLength(100)]
  public string ProductName { get; set; } = null!;

  [Column(TypeName = "decimal(18, 2)")]
  public decimal Price { get; set; }

  [StringLength(255)]
  public string? ImageUrl { get; set; }

  public bool IsActive { get; set; }

  public bool AnaSayfa { get; set; }

  [ForeignKey("CategoryId")]
  [InverseProperty("Products")]
  public virtual Category Category { get; set; } = null!;


  
}