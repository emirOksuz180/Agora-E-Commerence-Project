using System.ComponentModel.DataAnnotations;

namespace webBackend.Models;

public class OrderCreateModel
{
    // --- KİŞİSEL BİLGİLER (Ad ve Soyad Ayrıldı) ---
    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    [Display(Name = "Ad")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ad en az 2 karakter olmalıdır.")]
    public string Ad { get; set; } = null!;

    [Required(ErrorMessage = "Soyad alanı zorunludur.")]
    [Display(Name = "Soyad")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Soyad en az 2 karakter olmalıdır.")]
    public string Soyad { get; set; } = null!;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta formatı giriniz.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Telefon numarası zorunludur.")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Telefon 10 veya 11 haneli rakam olmalıdır.")]
    public string Telefon { get; set; } = null!;

    // --- ADRES BİLGİLERİ ---
    [Required(ErrorMessage = "Şehir seçimi zorunludur.")]
    [Display(Name = "Şehir")]
    public string Sehir { get; set; } = null!;

    [Required(ErrorMessage = "Adres satırı boş bırakılamaz.")]
    [Display(Name = "Açık Adres")]
    public string AdresSatiri { get; set; } = null!;

    [Required(ErrorMessage = "Posta kodu gereklidir.")]
    [RegularExpression(@"^[0-9]{5}$", ErrorMessage = "Geçerli bir posta kodu giriniz (Örn: 34000).")]
    public string PostaKodu { get; set; } = null!;

    [Display(Name = "Sipariş Notu")]
    public string? SiparisNotu { get; set; }

    // --- KART BİLGİLERİ (Iyzico için) ---
    [Required(ErrorMessage = "Kart üzerindeki isim zorunludur.")]
    [Display(Name = "Kart Üzerindeki İsim")]
    public string CartName { get; set; } = null!;

    [Required(ErrorMessage = "Kart numarası zorunludur.")]
    [CreditCard(ErrorMessage = "Geçerli bir kredi kartı numarası giriniz.")]
    public string CartNumber { get; set; } = null!;

    [Required(ErrorMessage = "Yıl seçiniz.")]
    [Range(2026, 2035, ErrorMessage = "Geçersiz yıl.")] // 2025'i 2026 yapabilirsin güncel yıl için
    public string CartExpirationYear { get; set; } = null!;

    [Required(ErrorMessage = "Ay seçiniz.")]
    [Range(1, 12, ErrorMessage = "Geçersiz ay.")]
    public string CartExpirationMonth { get; set; } = null!;

    [Required(ErrorMessage = "CVV kodu zorunludur.")]
    [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "CVV 3 veya 4 haneli olmalıdır.")]
    public string CartCVV { get; set; } = null!;

    // --- SİSTEM BİLGİLERİ ---
    public UserAddress? DefaultAddress { get; set; }
    public bool UseDefaultAddress { get; set; } = true;
    public int? DefaultAddressId { get; set; } // Önemli: DB'den adresi çekmek için ID gerekebilir
}


