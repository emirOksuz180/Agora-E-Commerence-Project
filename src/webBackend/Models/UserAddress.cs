using System.ComponentModel.DataAnnotations.Schema;
using webBackend.Models;
using System.ComponentModel.DataAnnotations;


namespace webBackend.Models;
public partial class UserAddress
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required(ErrorMessage = "Adres başlığı zorunludur (Örn: Ev, İş).")]
    [StringLength(50, ErrorMessage = "Adres başlığı en fazla 50 karakter olabilir.")]
    [Display(Name = "Adres Başlığı")]
    public string AddressTitle { get; set; } = null!;

    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Soyad alanı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = null!;

    [Required(ErrorMessage = "Telefon numarası zorunludur.")]
    [Phone(ErrorMessage = "Geçersiz telefon numarası formatı.")]
    [RegularExpression(@"^(05)[0-9]{9}$", ErrorMessage = "Telefon 05xx xxx xx xx formatında olmalıdır.")]
    [Display(Name = "Telefon Numarası")]
    public string Phone { get; set; } = null!;

    [Required(ErrorMessage = "Şehir seçimi zorunludur.")]
    [Display(Name = "Şehir")]
    public string City { get; set; } = null!;

    [Required(ErrorMessage = "İlçe seçimi zorunludur.")]
    [Display(Name = "İlçe")]
    public string District { get; set; } = null!;

    [Required(ErrorMessage = "Açık adres detayı zorunludur.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Lütfen daha detaylı bir adres giriniz.")]
    [Display(Name = "Açık Adres")]
    public string AddressDetail { get; set; } = null!;

    [Display(Name = "Varsayılan Adres")]
    public bool IsDefault { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Posta kodu ödeme işlemleri için gereklidir.")]
    [RegularExpression(@"^[0-9]{5}$", ErrorMessage = "Posta kodu 5 haneli rakam olmalıdır.")]
    [Display(Name = "Posta Kodu")]
    public string? ZipCode { get; set; }

    // Navigation Property
    public virtual AppUser User { get; set; } = null!;
}