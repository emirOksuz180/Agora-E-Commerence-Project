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
        public int Desi { get; set; }

        [Display(Name = "Fiziksel Ürün Mü?")]
        public bool? IsPhysical { get; set; }

        [Required(ErrorMessage = "Stok miktarı boş bırakılamaz.")]
        [Range(0, 1000000, ErrorMessage = "Stok miktarı 0 ile 1.000.000 arasında olmalıdır.")]
        [RegularExpression(@"^[0-9]*$", ErrorMessage = "Lütfen sadece rakam giriniz.")]
        public int Stock { get; set; }


        public int[] SelectedCarrierIds { get; set; }
    }
}