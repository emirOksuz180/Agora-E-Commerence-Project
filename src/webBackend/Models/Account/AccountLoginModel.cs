using System.ComponentModel.DataAnnotations;

namespace webBackend.Models
{
    public class AccountLoginModel
    {
        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [Display(Name = "E-Posta Adresi*")]
        [EmailAddress(ErrorMessage = "Lütfen geçerli bir E-posta adresi giriniz.")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Şifre alanı boş bırakılamaz.")]
        [Display(Name = "Şifre*")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Display(Name = "Beni Hatırla")]
        public bool BeniHatirla { get; set; } = true;
    }
}