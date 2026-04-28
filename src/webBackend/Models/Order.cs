using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema; 
using System.ComponentModel.DataAnnotations;

namespace webBackend.Models;

public partial class Order
{
    public int Id { get; set; }

    [Display(Name = "Sipariş Tarihi")]
    public DateTime SiparisTarihi { get; set; } = DateTime.Now;

    [Required]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    [StringLength(100)]
    public string Ad { get; set; } = null!;

    [Required(ErrorMessage = "Soyad alanı zorunludur.")]
    [StringLength(100)]
    public string Soyad { get; set; } = null!;

    [Required(ErrorMessage = "Şehir bilgisi zorunludur.")]
    [Display(Name = "Şehir")]
    public string Sehir { get; set; } = null!;

    [Required(ErrorMessage = "Adres bilgisi boş bırakılamaz.")]
    [Display(Name = "Açık Adres")]
    public string AdresSatiri { get; set; } = null!;

    [Required(ErrorMessage = "Posta kodu gereklidir.")]
    [RegularExpression(@"^[0-9]{5}$", ErrorMessage = "Geçerli bir posta kodu giriniz.")]
    public string PostaKodu { get; set; } = null!;

    [Required(ErrorMessage = "Telefon numarası zorunludur.")]
    [Phone(ErrorMessage = "Geçersiz telefon formatı.")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Telefon 10 veya 11 haneli rakam olmalıdır.")]
    public string Telefon { get; set; } = null!;

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string? Email { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Toplam fiyat 0'dan büyük olmalıdır.")]
    public double ToplamFiyat { get; set; }

    [StringLength(500)]
    [Display(Name = "Sipariş Notu")]
    public string? SiparisNotu { get; set; }

    public int? StatusId { get; set; }

    public int? ShippingRateId { get; set; }

    [Display(Name = "Kargo Takip Kodu")]
    public string? CargoTrackingCode { get; set; }

    // Navigation Properties
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ShippingRate? ShippingRate { get; set; }

    public virtual OrderStatus? Status { get; set; }
}