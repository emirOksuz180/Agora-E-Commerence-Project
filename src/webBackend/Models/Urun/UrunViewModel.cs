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

        
    [Display(Name = "Ağırlık (kg)")]
    [Range(0.01, 1000, ErrorMessage = "Ağırlık 0.01 ile 1000 kg arasında olmalıdır.")]
    public decimal? Weight { get; set; }

    [Display(Name = "Genişlik / En (cm)")]
    [Range(1, 500, ErrorMessage = "Genişlik 1 ile 500 cm arasında olmalıdır.")]
    public decimal? Width { get; set; }

    [Display(Name = "Yükseklik (cm)")]
    [Range(1, 500, ErrorMessage = "Yükseklik 1 ile 500 cm arasında olmalıdır.")]
    public decimal? Height { get; set; }

    [Display(Name = "Derinlik / Boy (cm)")]
    [Range(1, 500, ErrorMessage = "Boy 1 ile 500 cm arasında olmalıdır.")]
    public decimal? Length { get; set; }

    [Display(Name = "Desi Değeri")]
    public decimal Desi { get; set; }

    [Display(Name = "Fiziksel Ürün Mü?")]
    public bool? IsPhysical { get; set; }

    [Display(Name = "Stok Adedi")]
    [Required(ErrorMessage = "Stok bilgisi girmek zorunludur.")]
    [Range(0, int.MaxValue, ErrorMessage = "Stok adedi negatif olamaz.")]
    public int Stock { get; set; }
    }
}