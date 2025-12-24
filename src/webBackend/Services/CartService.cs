using Microsoft.EntityFrameworkCore;
using webBackend.Models;
namespace webBackend.Services;

public interface ICartService
{
  string GetCustomerId();
  Task<Cart> GetCart(string customerId);
  Task AddToCart(int urunId , int miktar = 1);
  Task RemoveItem(int urunId , int miktar = 1);
  Task TransferCartToUser(string username);
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
}