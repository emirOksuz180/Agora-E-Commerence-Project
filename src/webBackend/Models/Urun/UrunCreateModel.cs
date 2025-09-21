using System.ComponentModel.DataAnnotations;

namespace webBackend.Models;

public class UrunCreateModel
{


  [Display()]
  public string ProductName { get; set; } = null!;


  public double Price { get; set; }

  public string? ImageUrl { get; set; }

  public bool IsActive { get; set; }

  public bool AnaSayfa { get; set; }

  public int CategoryId { get; set; }


}