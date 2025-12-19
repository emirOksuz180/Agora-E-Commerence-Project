namespace webBackend.Models;
using System.ComponentModel.DataAnnotations;





public class SliderCreateModel
{
    

    [StringLength(100)]
    public string? SliderTitle { get; set; }

    [StringLength(200)]
    public string? SliderDescription { get; set; }

    [Required(ErrorMessage = "Lütfen bir görsel seçin.")]
    public IFormFile? ImageUrl { get; set; } = null!;

    public bool IsActive { get; set; }

    public short DisplayOrder { get; set; }
}