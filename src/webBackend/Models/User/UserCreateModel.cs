using System.ComponentModel.DataAnnotations;


    public class UserCreateModel
    {
        [Required(ErrorMessage = "Ad Soyad alanı boş bırakılamaz.")]
        [Display(Name = "Ad Soyad")]
        [StringLength(100, MinimumLength = 5  , ErrorMessage ="Ad & Soyad alanı en fazla 100 karakter olmalıdır ")]
        public string AdSoyad { get; set; } = null!;

        [Required(ErrorMessage = "E-posta alanı boş bırakılamaz.")]
        [Display(Name = "E-posta")]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = null!;


    } 

