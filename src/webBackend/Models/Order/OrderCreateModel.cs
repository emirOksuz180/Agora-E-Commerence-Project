using System.ComponentModel.DataAnnotations;

namespace webBackend.Models;

public class OrderCreateModel
{
    
    [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
    [Display(Name = "Ad Soyad")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Ad Soyad en az 3 karakter olmalıdır.")]
    public string AdSoyad { get; set; } = null!;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [DataType(DataType.EmailAddress)]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Lütfen geçerli bir e-posta formatı giriniz (örnek@alanadi.com).")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Şehir seçimi zorunludur.")]
    [Display(Name = "Şehir")]

    [RegularExpression(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ ]+$", ErrorMessage = "Şehir adı sadece harflerden oluşmalıdır.")]
    public string Sehir { get; set; } = null!;

    [Required(ErrorMessage = "Adres satırı boş bırakılamaz.")]
    [Display(Name = "Açık Adres")]
    public string AdresSatiri { get; set; } = null!;

    [Required(ErrorMessage = "Posta kodu gereklidir.")]
    [RegularExpression(@"^[0-9]{5}$", ErrorMessage = "Geçerli bir posta kodu giriniz (Örn: 34000).")]
    public string PostaKodu { get; set; } = null!;

    [Required(ErrorMessage = "Telefon numarası zorunludur.")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Telefon 10 veya 11 haneli rakam olmalıdır.")]
    public string Telefon { get; set; } = null!;

    [Display(Name = "Sipariş Notu")]
    public string? SiparisNotu { get; set; }

    
    [Required(ErrorMessage = "Kart üzerindeki isim zorunludur.")]
    [Display(Name = "Kart üzerindeki isim")]
    public string CartName { get; set; } = null!;

    [Required(ErrorMessage = "Kart numarası zorunludur.")]
    [CreditCard(ErrorMessage = "Geçerli bir kredi kartı numarası giriniz.")]
    [Display(Name = "Kart numarası")]
    public string CartNumber { get; set; } = null!;

    [Required(ErrorMessage = "Yıl seçiniz.")]
    [Range(2025, 2035, ErrorMessage = "Geçersiz yıl.")]
    [Display(Name = "Yıl")]
    public string CartExpirationYear { get; set; } = null!;

    [Required(ErrorMessage = "Ay seçiniz.")]
    [Range(1, 12, ErrorMessage = "Geçersiz ay.")]
    [Display(Name = "Ay")]
    public string CartExpirationMonth { get; set; } = null!;

    [Required(ErrorMessage = "CVV kodu zorunludur.")]
    [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "CVV 3 veya 4 haneli olmalıdır.")]
    public string CartCVV { get; set; } = null!;
}