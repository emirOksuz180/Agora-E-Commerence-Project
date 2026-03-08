using System.ComponentModel.DataAnnotations;

namespace webBackend.Models.Permissons;



public class UserEditModel
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

    [Required(ErrorMessage = "Şifre alanı boş bırakılamaz.")]
    [Display(Name = "Parola")]
    [DataType(DataType.Password)]
    [StringLength(64, MinimumLength = 6  , ErrorMessage ="Şifre min. 6 , maks. 64 karakter olamlıdır")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Karşılaştırma adına şifre tekrarı boş bırakılamaz.")]
    [Display(Name = "Parola Tekrar")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Parola eşleşmiyor")]
    public string ConfirmPassword { get; set; } = null!;


    public IList<string>?  SelectedRoles { get; set; }

    public List<PermissionItemViewModel> Permissions { get; set; } = new();
}