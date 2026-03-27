using System.ComponentModel.DataAnnotations;

namespace webBackend.Models
{
    public class AccountResetPasswordModel
    {
        [Required]
        public string Token { get; set; } = null!;

        [Required(ErrorMessage = "E-posta bilgisi eksik, lütfen işlemi tekrar başlatın.")]
        [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [Display(Name = "Yeni Şifre*")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Şifreniz en az 6 karakter olmalıdır.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Şifre Tekrarı alanı boş bırakılamaz.")]
        [Display(Name = "Şifre Tekrar*")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Girdiğiniz Şifreler birbiriyle eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}