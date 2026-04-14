using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema; // [ForeignKey] için

namespace webBackend.Models;

public partial class Order
{
    public int Id { get; set; }
    public DateTime SiparisTarihi { get; set; }
    public string Username { get; set; } = null!;
    public string Sehir { get; set; } = null!;
    public string AdresSatiri { get; set; } = null!;
    public string PostaKodu { get; set; } = null!;
    public string Telefon { get; set; } = null!;
    public string? Email { get; set; }
    public double ToplamFiyat { get; set; }
    public string AdSoyad { get; set; } = null!;
    public string? SiparisNotu { get; set; }

    // --- Yeni Eklenen Alanlar ---
    
    // DB'deki isimlendirmene göre (StatusId veya OrderStatusId) burayı kontrol et:
    public int StatusId { get; set; } 
    
    public int? ShippingRateId { get; set; }
    public string? CargoTrackingCode { get; set; }

    // Navigation Properties
    [ForeignKey("StatusId")]
    public virtual OrderStatus? Status { get; set; } = null!;
    
    [ForeignKey("ShippingRateId")]
    public virtual ShippingRate? ShippingRate { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}