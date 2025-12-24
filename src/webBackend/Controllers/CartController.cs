using System.Threading.Tasks;
using dotnet_store.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;
using webBackend.Services;


namespace dotnet_store.Controllers;


public class CartController : Controller
{
    
    private readonly ICartService _cartService;
    public CartController( ICartService cartService)
    {
        
        _cartService = cartService;
    }

    public async Task<ActionResult> Index()
    {   
        var customerId = _cartService.GetCustomerId();
        var cart = await _cartService.GetCart(customerId);
        return View(cart);
    }

    [HttpPost]
    public async Task<ActionResult> AddToCart(int urunId, int miktar = 1)
    {
        await _cartService.AddToCart(urunId , miktar);

        

        return RedirectToAction("Index", "Cart");
    }




    [HttpPost]
    public async Task<ActionResult> RemoveItem(int urunId , int miktar)
    {

        await _cartService.RemoveItem(urunId , miktar);

        return RedirectToAction("Index" , "Cart");        
    }



   
    
}