using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;
using webBackend.Models.Shipping;

[Authorize(Roles = "Admin")]
public class ShippingManagerController : Controller
{
    private readonly AgoraDbContext _context;

    public ShippingManagerController(AgoraDbContext context)
    {
        _context = context;
    }

    // 1. Kargo Firmaları Listesi
    public async Task<IActionResult> Carriers()
    {
        var carriers = await _context.Carriers.ToListAsync();
        return View(carriers);
    }

    // Bölgeleri Listeleme
    public async Task<IActionResult> Regions()
    {
        var regions = await _context.ShippingRegions.ToListAsync();
        return View(regions);
    }

    // Yeni Bölge Ekleme
    [HttpPost]
    public async Task<IActionResult> AddRegion(ShippingRegion region)
    {
        if (ModelState.IsValid)
        {
            region.IsActive = true; // Varsayılan olarak aktif
            _context.ShippingRegions.Add(region);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Regions));
        }
        return View("Regions", await _context.ShippingRegions.ToListAsync());
    }

    // Bölge Silme (Veya Pasife Çekme)
    [HttpPost]
    public async Task<IActionResult> DeleteRegion(int id)
    {
        var region = await _context.ShippingRegions.FindAsync(id);
        if (region != null)
        {
            _context.ShippingRegions.Remove(region);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Regions));
    }



    [HttpGet]
    public async Task<IActionResult> ManageRates(int carrierId)
    {
        var carrier = await _context.Carriers
            .Include(c => c.ShippingRates)
            .ThenInclude(r => r.Region)
            .FirstOrDefaultAsync(c => c.Id == carrierId);

        if (carrier == null) return NotFound();

        var viewModel = new ShippingRateViewModel
        {
            CarrierId = carrier.Id,
            CarrierName = carrier.CarrierName,
            Regions = await _context.ShippingRegions.Where(r => r.IsActive== true).ToListAsync(),
            ExistingRates = carrier.ShippingRates.ToList()
        };

        return View(viewModel);
    }


}