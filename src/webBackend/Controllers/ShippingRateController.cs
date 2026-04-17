using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;
using webBackend.Models.Shipping;

public class ShippingRateController : Controller
{
    private readonly AgoraDbContext _context;
    public ShippingRateController(AgoraDbContext context) => _context = context;

   public async Task<IActionResult> Index(int carrierId)
    {
        // 1. Kargo firmasını bul (Adını parantez içine yazmak için)
        var carrier = await _context.Carriers.FindAsync(carrierId);
        if (carrier == null) return NotFound("Kargo firması bulunamadı.");

        // 2. SADECE bu kargoya atanmış bölgeleri getir (Parantez içi ve dropdown için kritik)
        var assignedRegions = await _context.CarrierRegions
            .Where(cr => cr.CarrierId == carrierId)
            .Select(cr => cr.Region)
            .ToListAsync();

        var viewModel = new ShippingRateViewModel
        {
            CarrierId = carrierId,
            CarrierName = carrier.CarrierName, // Artık garanti dolu geliyor
            Regions = assignedRegions ?? new List<ShippingRegion>(), // Sadece kargonun bölgeleri
            ExistingRates = await _context.ShippingRates
                                        .Where(r => r.CarrierId == carrierId)
                                        .Include(r => r.Region)
                                        .ToListAsync()
        };

        // View yolunu senin klasör yapına göre koruyorum
        return View("../ShippingManager/ManageRates", viewModel); 
    }

    public IActionResult Create()
    {
        ViewBag.Carriers = new SelectList(_context.Carriers, "Id", "CarrierName");
        ViewBag.Regions = new SelectList(_context.ShippingRegions, "Id", "RegionName");
        return View();
    }


    public async Task<IActionResult> Edit(int id)
{
    var rate = await _context.ShippingRates.FindAsync(id);
    if (rate == null) return NotFound();

    ViewBag.Carriers = new SelectList(_context.Carriers, "Id", "CarrierName", rate.CarrierId);
    ViewBag.Regions = new SelectList(_context.ShippingRegions, "Id", "RegionName", rate.RegionId);
    
    return View(rate);
}

  [HttpPost]
  public async Task<IActionResult> Edit(ShippingRate rate)
  {
      if (ModelState.IsValid)
      {
          _context.Update(rate);
          await _context.SaveChangesAsync();
          return RedirectToAction(nameof(Index));
      }
      return View(rate);
  }

    [HttpPost]
    public async Task<IActionResult> AddRate(ShippingRate rate)
    {
        if (rate.CarrierId <= 0 || rate.RegionId <= 0)
        {
            return Content("Hata: Kargo veya Bölge seçimi geçersiz.");
        }

        rate.Carrier = null;
        rate.Region = null;

        try 
        {
            _context.ShippingRates.Add(rate);
            await _context.SaveChangesAsync();
            
            
            return RedirectToAction("ManageRates", "Carrier", new { carrierId = rate.CarrierId });
        }
        catch (Exception ex)
        {
            return Content("Veritabanı Kayıt Hatası: " + (ex.InnerException?.Message ?? ex.Message));
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRate(int id)
    {
        
        var rate = await _context.ShippingRates.FindAsync(id);
        
        if (rate == null)
        {
            return NotFound();
        }

        
        int carrierId = rate.CarrierId;

       
        _context.ShippingRates.Remove(rate);
        await _context.SaveChangesAsync();

        
        return RedirectToAction("Index", "ShippingRate", new { carrierId = carrierId });
    }

  
}