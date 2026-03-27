using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webBackend.Models;

public partial class OrderItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }

    [Required]
    public int UrunId { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Fiyat negatif olamaz.")] 
    [Display(Name = "Birim Fiyat")]
    [DataType(DataType.Currency)]
    public double Fiyat { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Miktar en az 1 olmalıdır.")]
    [Display(Name = "Adet")]
    public int Miktar { get; set; }

    
    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("UrunId")]
    public virtual Product Urun { get; set; } = null!;

    /// <summary>
    /// / görmezden gel
    /// </summary>
    [NotMapped] 
    public double SatirToplami => Fiyat * Miktar;
}