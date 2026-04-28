using webBackend.Models;

namespace webBackend.Models;
public class CheckoutViewModel
{
    // Veritabanından gelecek varsayılan adres
    public UserAddress? DefaultAddress { get; set; }

    
    public bool UseDefaultAddress { get; set; } = true;

    
    public List<CartItemViewModel> CartItems { get; set; } = new();

    // Ödeme özeti
    public decimal TotalPrice => CartItems.Sum(x => x.Quantity * x.Price);
}


public class CartItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
}