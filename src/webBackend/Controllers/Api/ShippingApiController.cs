using Microsoft.AspNetCore.Mvc;
using webBackend.Services;

namespace webBackend.Controllers.Api 
{
    [Route("api/[controller]")] 
    [ApiController]
    public class ShippingApiController : ControllerBase
    {
        private readonly IShippingService _shippingService;
        private readonly ICartService _cartService;

        public ShippingApiController(IShippingService shippingService, ICartService cartService)
        {
            _shippingService = shippingService;
            _cartService = cartService;
        }

        [HttpGet("calculate")]
        public async Task<IActionResult> Calculate(int carrierId, int regionId)
        {
            var cart = await _cartService.GetCartAsync();
            if (cart == null || !cart.CartItems.Any()) return BadRequest("Sepet boş.");

            decimal totalDesi = _shippingService.CalculateTotalDesi(cart.CartItems.ToList());
            
            // Sepet toplamını gönderiyoruz
            decimal price = await _shippingService.GetShippingPriceAsync(carrierId, regionId, totalDesi, (decimal)cart.Toplam);

            return Ok(new { 
                shippingPrice = price, 
                formattedPrice = price == 0 ? "Ücretsiz Kargo" : price.ToString("C2"),
                totalDesi = totalDesi 
            });
        }
    }
}