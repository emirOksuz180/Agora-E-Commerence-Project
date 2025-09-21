using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class Slider
{
    public int SliderId { get; set; }

    public string? SliderTitle { get; set; }

    public string? SliderDescription { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool IsActive { get; set; }

    public short DisplayOrder { get; set; }
}
