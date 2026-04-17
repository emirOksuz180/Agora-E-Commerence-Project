using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;

public class ShippingRegionController : Controller
{
    private readonly AgoraDbContext _context;
    public ShippingRegionController(AgoraDbContext context) => _context = context;

    // Bölgeleri Listeleme
    public async Task<IActionResult> Index()
    {
        var regions = await _context.ShippingRegions.ToListAsync();
        return View("~/Views/ShippingManager/Regions.cshtml", regions);
    }

    // YENİ BÖLGE EKLEME (View'daki asp-action="AddRegion" ile uyumlu)
    [HttpPost]
    public async Task<IActionResult> AddRegion(string RegionName)
    {
        if (!string.IsNullOrEmpty(RegionName))
        {
            var newRegion = new ShippingRegion
            {
                RegionName = RegionName,
                IsActive = true
            };

            _context.ShippingRegions.Add(newRegion);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRegion(int id)
    {
        var region = await _context.ShippingRegions
            .Include(r => r.CarrierRegions) 
            .Include(r => r.ShippingRates)  
            .FirstOrDefaultAsync(r => r.Id == id);

        if (region == null) return NotFound();

        bool hasCarriers = region.CarrierRegions != null && region.CarrierRegions.Any();
        bool hasRates = region.ShippingRates != null && region.ShippingRates.Any();

        if (hasCarriers || hasRates)
        {
            string message = $"'{region.RegionName}' bölgesi silinemez çünkü: ";
            if (hasCarriers) message += "Bu bölgeye atanmış kargo firmaları var. ";
            if (hasRates) message += "Bu bölge için girilmiş desi/fiyat kuralları mevcut.";

            TempData["Error"] = message;
            return RedirectToAction(nameof(Index));
        }

        try 
        {
            _context.ShippingRegions.Remove(region);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Bölge başarıyla silindi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Beklenmedik bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}