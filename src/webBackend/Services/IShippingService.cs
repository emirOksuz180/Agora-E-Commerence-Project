using webBackend.Models;


namespace webBackend.Services;
public interface IShippingService
{
    // Sepetteki ürünlerin toplam desisini hesaplar
    decimal CalculateTotalDesi(List<CartItem> items);

    // Desi ve bölgeye göre kargo ücretini hesaplar
   Task<decimal> GetShippingPriceAsync(int carrierId, int regionId, decimal totalDesi, decimal totalCartPrice);
}