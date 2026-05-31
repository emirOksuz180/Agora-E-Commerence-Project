using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;
namespace webBackend.Services;

public interface ICartService
{
    string GetCustomerId();
    Task<Cart> GetCart(string customerId); // Mevcut
    Task<Cart> GetCartAsync();             // Düzeltildi: Task yerine Task<Cart>
    Task AddToCart(int urunId, int miktar = 1);
    Task RemoveItem(int urunId, int miktar = 1);
    Task TransferCartToUser(string username);
    Task ClearCart();

    // ICartService.cs içerisindeki ilgili satırı bul ve aşağıdakiyle değiştir:
    Task<ShippingCalculationResult> GetDynamicShippingAsync(int cityId, int districtId, int carrierId, string username);
}

public class CartService : ICartService
{

  private readonly AgoraDbContext _context;
  private readonly IHttpContextAccessor _httpContextAccessor;

  public CartService(AgoraDbContext context , IHttpContextAccessor httpContextAccessor)
  {
    _context = context;
    _httpContextAccessor = httpContextAccessor;
  }

  
  public async Task AddToCart(int urunId, int miktar = 1)
  {
    var cart = await GetCart(GetCustomerId());

        // var item = cart.CartItems.Where(i => i.UrunId == urunId).FirstOrDefault();

        var urun = await _context.Products.FirstOrDefaultAsync(i => i.ProductId ==  urunId);

        if(urun != null)
        {
            cart.AddItem(urun , miktar);

            await _context.SaveChangesAsync();
        }
  }

  public async Task ClearCart()
  {
      var customerId = GetCustomerId();
      // 1. Kullanıcının sepetini ve içindeki ürünleri (CartItems) çekiyoruz
      var cart = await _context.Carts
                              .Include(i => i.CartItems)
                              .FirstOrDefaultAsync(i => i.CustomerId == customerId);

      if (cart != null)
      {
          // 2. Sepet içindeki kalemleri temizliyoruz
          _context.CartItems.RemoveRange(cart.CartItems);
          
          // 3. İstersen sepetin kendisini de silebilirsin (veya sadece kalemleri)
          _context.Carts.Remove(cart);

          await _context.SaveChangesAsync();
      }
    
  }

  public async Task<Cart> GetCart(string custId)
  {
    //  ;

        var cart = await _context.Carts
                            .Include(i => i.CartItems)
                            .ThenInclude(i => i.Urun)
                            .Where(i => i.CustomerId == custId)
                            .FirstOrDefaultAsync();

        if (cart == null)
        {
            
            

            if(string.IsNullOrEmpty(custId))
            {
                 var customerId = _httpContextAccessor.HttpContext?.User.Identity?.Name;

                customerId = Guid.NewGuid().ToString();
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddMonths(1),
                    IsEssential= true
                };

                _httpContextAccessor.HttpContext?.Response.Cookies
                  .Append("customerId", customerId, cookieOptions);

            }

            

            cart = new Cart { CustomerId = custId! };
            _context.Carts.Add(cart);
            
        }

        return cart;
  }

  public Task<Cart> GetCartAsync()
  {
    throw new NotImplementedException();
  }

  public string GetCustomerId()
  {
    var context = _httpContextAccessor.HttpContext;
    return context?.User.Identity?.Name ?? context?.Request.Cookies["customerId"]!;
    
  }

  public async Task RemoveItem(int urunId, int miktar = 1)
  {
    var cart = await GetCart(GetCustomerId());

        var urun = await _context.Products.FirstOrDefaultAsync(i => i.ProductId ==  urunId);

        if(urun != null)
        {
            cart.DeleteItem(urunId , miktar);

            await _context.SaveChangesAsync();
        }
  }

  public async Task TransferCartToUser(string username)
  {
    var userCart = await GetCart(username);

        var cookieCart = await GetCart(_httpContextAccessor.HttpContext?.Request.Cookies["customerId"]!);

    foreach(var item in cookieCart?.CartItems!)
    {
        var cartItem = userCart?.CartItems.Where(i => i.UrunId == item.UrunId).FirstOrDefault();
        if(cartItem != null)
        {
            cartItem.Miktar += item.Miktar;
        }
        else
        {
            userCart?.CartItems.Add(new CartItem {UrunId = item.UrunId , Miktar = item.Miktar});
        }
    }

    _context.Carts.Remove(cookieCart);

    await _context.SaveChangesAsync();
  }




    // CartService.cs içerisindeki metodu hafifçe güncelliyoruz
public async Task<ShippingCalculationResult> GetDynamicShippingAsync(int cityId, int districtId, int carrierId, string username)
{
    var cart = await GetCart(username);
    if (cart == null) return null;
    
    var priceParam = new SqlParameter("@FinalShippingPrice", SqlDbType.Decimal) 
    { 
        Direction = ParameterDirection.Output, 
        Precision = 18, Scale = 2 
    };
    var isFreeParam = new SqlParameter("@IsFreeShipping", SqlDbType.Bit) 
    { 
        Direction = ParameterDirection.Output 
    };

    // SQL Prosedürünü çalıştır (SP'nin @CityId aldığını varsayarak)
    await _context.Database.ExecuteSqlRawAsync(
        "EXEC sp_CalculateShippingProfitability @CartId, @CityId, @DistrictId, @SelectedCarrierId, @FinalShippingPrice OUTPUT, @IsFreeShipping OUTPUT",
        new SqlParameter("@CartId", cart.CartId),
        new SqlParameter("@CityId", cityId), // TblIl ID'si
        new SqlParameter("@DistrictId", districtId),
        new SqlParameter("@SelectedCarrierId", carrierId),
        priceParam,
        isFreeParam
    );

    return new ShippingCalculationResult 
    {
        IsSuccess = true,
        FinalFiyat = (priceParam.Value != DBNull.Value) ? (decimal)priceParam.Value : 0,
        UcretsizKargo = isFreeParam.Value != DBNull.Value && (bool)isFreeParam.Value,
        CartTotal = (decimal)cart.Toplam
    };
}



}