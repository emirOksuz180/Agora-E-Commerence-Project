using System.ComponentModel.DataAnnotations;


    public class AccountEditUserModel
    {
        [Required]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; } = null!;

        [Required]
        [Display(Name = "Eposta")]
        [EmailAddress]
        public string Email { get; set; } = null!;


    } 

