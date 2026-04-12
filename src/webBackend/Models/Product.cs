using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace webBackend.Models;


public partial class Product
{

    public Product()
    {
        // Nesne oluştuğu an tarih bugünkü tarih olur, 0001 yılına düşmez.
        CreatedAt = DateTime.Now; 
        
    }
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? ProductDescription { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public int CategoryId { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool AnaSayfa { get; set; } = false;

    public decimal? Weight { get; set; }

    public decimal? Width { get; set; }

    public decimal? Height { get; set; }

    public decimal? Length { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [Column(TypeName = "decimal(10,2)")]
    public decimal? Desi { get; set; }

    public bool? IsPhysical { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
