using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace webBackend.Models
{
    public class UrunViewModel
    {
        public int ProductId { get; set; } 

        [Required(ErrorMessage = "{0} alanı zorunludur.")]
        [Display(Name = "Ürün Adı *")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "{0} en az {2} karakter olmalıdır.")]
        public string ProductName { get; set; } = null!;

        [Required(ErrorMessage = "{0} alanı zorunludur.")]
        [Display(Name = "Ürün Fiyatı *")]
        
        [Range(0, 10000000, ErrorMessage = "Fiyat 0 ile belirleyeceğiniz bir miktar arasında olmalıdır.")]
        [RegularExpression(@"^[0-9]+(\.[0-9]{1,2})?$", ErrorMessage = "Lütfen sadece rakam ve ondalık ayırıcı olarak nokta kullanınız. (Örn: 14999.01)")]        
        public decimal Price { get; set; }

        [Display(Name = "Ürün Açıklaması *")]
        [Required(ErrorMessage = "{0} alanı zorunludur.")]
        public string Description { get; set; } = null!;

        [Display(Name = "Ürün Resmi")]
        public IFormFile? ImageFile { get; set; }

        public string? ImageUrl { get; set; } 

        [Display(Name = "Aktif mi?")]
        public bool IsActive { get; set; }

        [Display(Name = "Anasayfada Göster")]
        public bool AnaSayfa { get; set; }

        [Required(ErrorMessage = "Lütfen bir kategori seçiniz.")]
        [Display(Name = "Kategori *")]
        public int CategoryId { get; set; }
        
        public string? CategoryName { get; set; } 
    }
}