namespace webBackend.Models;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

public class SliderEditModel
{
    public int SliderId { get; set; }

    [Display(Name = "Slider Başlığı")]
    [StringLength(100, ErrorMessage = "{0} alanı en fazla {1} karakter olabilir.")]
    public string? SliderTitle { get; set; }

    [Display(Name = "Slider Açıklaması")]
    [StringLength(500, ErrorMessage = "{0} alanı en fazla {1} karakter olabilir.")] 
    public string? SliderDescription { get; set; }

    [Required(ErrorMessage = "Lütfen bir görsel seçin.")]
    [Display(Name = "Slider Görseli")]
    public IFormFile? ImageUrl { get; set; } = null!; 
    
    [Display(Name = "Görsel Adı")]
    public string? ImageName { get; set; } 

    [Display(Name = "Durum")]
    public bool IsActive { get; set; }

    [Display(Name = "Görüntüleme Sırası")]
    [Range(0, 500, ErrorMessage = "Sıralama 0-500 arasında olmalıdır.")]
    public short DisplayOrder { get; set; }
}