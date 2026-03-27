using System.ComponentModel.DataAnnotations;
using webBackend.Validation;


namespace webBackend.Models
{
    public class KategoriEditModel
    {
        [Display(Name = "Kategori No")]
        public int Id { get; set; }

        [Display(Name = "Kategori Adı *")]
        [Required(ErrorMessage = "Kategori Adı alanı zorunludur.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Kategori Adı en az 2, en fazla 50 karakter olmalıdır.")]
        public string Name { get; set; } = null!;

        [Display(Name = "URL / Link Uzantısı *")]
        [Required(ErrorMessage = "URL alanı zorunludur.")]
        [UrlSlug] 
        public string Url { get; set; } = null!;
    }
}