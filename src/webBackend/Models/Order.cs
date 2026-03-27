using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webBackend.Models;

public partial class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime SiparisTarihi { get; set; } = DateTime.Now;

    [Required]
    public string Username { get; set; } = null!;

    [Required]
    public string Sehir { get; set; } = null!;

    [Required]
    public string AdresSatiri { get; set; } = null!;

    [Required]
    public string PostaKodu { get; set; } = null!;

    [Required]
    public string Telefon { get; set; } = null!;

    public string? Email { get; set; }

    // ZINNK: Veritabanındaki 'ToplamFiyat' sütunuyla birebir eşleşen alan
    [Required]
    [Column("ToplamFiyat", TypeName = "float")]
    public double ToplamFiyat { get; set; } = 0;

    [Required]
    public string AdSoyad { get; set; } = null!;

    public string? SiparisNotu { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    
    public double AraToplam()
    {
        if (OrderItems == null) return 0;
        return OrderItems.Where(i => i.Fiyat > 0).Sum(i => i.Fiyat * i.Miktar);
    }

   
    public double Toplam()
    {
        var sonuc = AraToplam() * 1.2;
        
        
        this.ToplamFiyat = sonuc; 
        
        return sonuc;
    }
}