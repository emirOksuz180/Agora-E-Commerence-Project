namespace webBackend.Models;
using System.ComponentModel.DataAnnotations;





public class SliderGetModel
{
    [Key]
    public int SliderId { get; set; }

    [StringLength(100)]
    public string? SliderTitle { get; set; }

    [StringLength(200)]
    public string? SliderDescription { get; set; }

    [StringLength(250)]
    public string ImageUrl { get; set; } = null!;

    public bool IsActive { get; set; }

    public short DisplayOrder { get; set; }
}