using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace webBackend.Models
{
    public class UrunEditModel
    {
        public int ProductId { get; set; } 

        [Required(ErrorMessage = "Ürün Adı alanı zorunludur.")]
        [Display(Name = "Ürün Adı *")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Ürün Adı en az 2 karakter olmalıdır.")]
        public string ProductName { get; set; } = null!;

        [Required(ErrorMessage = "Ürün Fiyatı alanı zorunludur.")]
        [Display(Name = "Ürün Fiyatı *")]
        [RegularExpression(@"^\d+([.,]\d{1,2})?$", ErrorMessage = "Lütfen geçerli bir fiyat giriniz (Örn: 150,50)")]
        public string Price { get; set; } = "0"; 

        [Display(Name = "Ürün Açıklaması *")]
        [Required(ErrorMessage = "Ürün Açıklaması alanı zorunludur.")]
        public string Description { get; set; } = null!; 

        public string? ImageUrl { get; set; } 

        [Display(Name = "Yeni Ürün Resmi (Opsiyonel)")]
        public IFormFile? ImageFile { get; set; } 

        [Display(Name = "Aktif mi?")]
        public bool IsActive { get; set; }

        [Display(Name = "Anasayfada Göster")]
        public bool AnaSayfa { get; set; }

        [Required(ErrorMessage = "Lütfen bir kategori seçiniz.")]
        [Display(Name = "Kategori *")]
        public int CategoryId { get; set; }
    }
}