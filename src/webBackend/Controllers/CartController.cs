// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace webBackend.Controllers;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;

[Authorize]


public class CartController: Controller
{

  private readonly AgoraDbContext _context;

  public CartController(AgoraDbContext context)
  {
    
    _context = context;

  }

  [HttpPost]
  public async Task<ActionResult> AddToCart(int urunId , int miktar = 1)
  {
    var customerId = User.Identity?.Name;
    var cart = await _context.Carts.Include(i => i.CartItems)
      .Where(i => i.CustomerId == customerId)
      .FirstOrDefaultAsync();


    if(cart == null)
    {
      cart = new Cart {CustomerId = customerId!};
      _context.Carts.Add(cart);
    }
    
    var item = cart.CartItems.Where(i => i.UrunId == urunId).Any();

    if(item != null)
    {
      // item += 1;
    }
    else
    {
      cart.CartItems.Add(new CartItem
      {
        UrunId = urunId,
        Miktar = miktar  
      });
    }

    await _context.SaveChangesAsync();

    return RedirectToAction("Index" , "Home");

    return View();
  }
  
}