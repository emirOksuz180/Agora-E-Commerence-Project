using System.ComponentModel.DataAnnotations;

namespace webBackend.Models 
{
    public class TwoFactorVerifyViewModel
    {
        [Required(ErrorMessage = "Lütfen 6 haneli doğrulama kodunu giriniz.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Kod tam olarak 6 haneli olmalıdır.")]
        [Display(Name = "Doğrulama Kodu")]
        public string TwoFactorCode { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
        
        public string? ReturnUrl { get; set; }
        
        public string? MaskedEmail { get; set; }
    }
}