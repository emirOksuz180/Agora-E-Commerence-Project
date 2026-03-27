using System.ComponentModel.DataAnnotations;

namespace webBackend.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
        [Display(Name = "Ad Soyad *")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Ad alanı en az 5, en fazla 50 karakter olmalıdır.")]
        public string AdSoyad { get; set; } = null!;

        [Required(ErrorMessage = "E-posta adresi boş bırakılamaz.")]
        [Display(Name = "E-Posta Adresi *")]
        [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [Display(Name = "Şifre*")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Şifreniz güvenlik nedeniyle en az 6 karakter olmalıdır.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Şifre Tekrarı alanı boş bırakılamaz.")]
        [Display(Name = "Şifre Tekrar*")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Girdiğiniz şifreler birbiriyle eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}