namespace webBackend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 




    public class PermissionCreateModel {

      [Key]
      public int Id { get; set; }

      [Required(ErrorMessage = "Yetki anahtar  boş bırakılamaz.")]
      [StringLength(100 , ErrorMessage = "Yetki anahtarı en fazla 50 karakter olmalıdır.")]
      [Display(Name = "Yetki Anahtarı")]
      public string PermissionKey { get; set; } = null!;

      [StringLength(100 , ErrorMessage = "Açıklama en fazla 100 karakter olabilir.")]
      [Display(Name = "Açıklama")]
      public string? Description { get; set; }

      [Required(ErrorMessage = "Grup adı  boş bırakılamaz.")]
      [StringLength(50 , ErrorMessage = "Grup adı en fazla 100 karakter olabilir.")]
      [Display(Name = "Grup Adı")]
      public string? GroupName { get; set; }

    }



    