using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public string Url { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
