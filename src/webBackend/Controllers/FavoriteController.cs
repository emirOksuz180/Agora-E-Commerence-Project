using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using webBackend.Models;



[Authorize] // Sadece giriş yapanlar favori ekleyebilir
public class FavoriteController : Controller
{
    private readonly AgoraDbContext _context;

    public FavoriteController(AgoraDbContext context)
    {
        _context = context;
    }

    // Favorilerim Sayfası
    public async Task<IActionResult> Index()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");
        
        int userId = int.Parse(userIdString);

        // Navigation property (f.Product) bozuk olduğu için JOIN kullanıyoruz:
        var viewModel = await (from f in _context.Favorites
                              join p in _context.Products on f.ProductId equals p.ProductId
                              where f.UserId == userId
                              select new FavoriteViewModel
                              {
                                  FavoriteId = f.Id, // Senin dosyanda 'Id' olduğunu söylemiştin
                                  ProductId = p.ProductId,
                                  ProductName = p.ProductName,
                                  ImageUrl = p.ImageUrl,
                                  Price = p.Price,
                                  AddedDate = (DateTime)f.CreatedAt
                              }).ToListAsync();

        return View(viewModel);
      }

   
    [HttpPost]
    public async Task<IActionResult> ToggleFavorite(int productId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        
        var existingFavorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

        if (existingFavorite != null)
        {
            _context.Favorites.Remove(existingFavorite); // Varsa çıkar
        }
        else
        {
            _context.Favorites.Add(new Favorite 
            { 
                UserId = userId, 
                ProductId = productId, 
                CreatedAt = DateTime.Now 
            }); // Yoksa ekle
        }

        await _context.SaveChangesAsync();
        return Ok(); // Başarılıysa 200 dön, Frontend'de kalbi boyarız
    }



    
    
    public async Task<IActionResult> DeleteFavorite(int id)
    {
        var favorite = await _context.Favorites.FindAsync(id);
        if (favorite != null)
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
            
        }
        return RedirectToAction(nameof(Index));
    }
}

