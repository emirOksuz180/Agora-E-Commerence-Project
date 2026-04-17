using Microsoft.EntityFrameworkCore;
using webBackend.Models;
using webBackend.Services;

public class ShippingService : IShippingService
{
    private readonly AgoraDbContext _context;
  private decimal totalCartPrice;

  public ShippingService(AgoraDbContext context)
    {
        _context = context;
    }

    public decimal CalculateTotalDesi(List<CartItem> items)
    {
        // Formül: (En * Boy * Yükseklik) / 3000 * Adet
        // Eğer ürün kartında desi hazır varsa direkt onu topla
        return items.Sum(x => 
        (x.Urun.Width ?? 0) * (x.Urun.Height ?? 0) * (x.Urun.Length ?? 0) / 3000 * x.Miktar);
    }
    

    public async Task<decimal> GetShippingPriceAsync(int carrierId, int regionId, decimal totalDesi, decimal totalCartPrice)
    {
        // Ücretsiz kargo limiti kontrolü
        if (totalCartPrice >= 1500m) 
        {
            return 0;
        }

        // 1. Desi aralığına tam uyan kaydı getir
        var rate = await _context.ShippingRates
            .FirstOrDefaultAsync(r => r.CarrierId == carrierId && 
                                    r.RegionId == regionId && 
                                    totalDesi >= r.MinDesi && 
                                    totalDesi <= r.MaxDesi);

        if (rate != null) return rate.Price;

        // 2. Eğer desi, tanımlı en yüksek aralıktan büyükse
        var maxRate = await _context.ShippingRates
            .Where(r => r.CarrierId == carrierId && r.RegionId == regionId)
            .OrderByDescending(r => r.MaxDesi)
            .FirstOrDefaultAsync();

        if (maxRate != null && totalDesi > maxRate.MaxDesi)
        {
            decimal extraAmount = Math.Ceiling(totalDesi - maxRate.MaxDesi);
            return maxRate.Price + (extraAmount * (maxRate.ExtraDesiPrice ?? 0));
        }

        return 0;
    }

  
}