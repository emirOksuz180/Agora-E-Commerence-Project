using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;
using webBackend.Models.Shipping; // ViewModel namespace'inize göre kontrol edin

public class CarrierController : Controller
{
    private readonly AgoraDbContext _context;
    public CarrierController(AgoraDbContext context) => _context = context;

    // --- TEMEL CRUD İŞLEMLERİ ---

    public async Task<IActionResult> Index()
    {
        
        ViewBag.Regions = await _context.ShippingRegions
            .Where(r => r.IsActive == true)
            .ToListAsync();

    
        var carriers = await _context.Carriers
            .Include(c => c.ShippingRates)
            .Include(c => c.CarrierRegions)
                .ThenInclude(cr => cr.Region)
            .ToListAsync();
        return View("~/Views/Carrier/Index.cshtml", carriers);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Carrier carrier, List<int> selectedRegionIds)
    {
        if (ModelState.IsValid)
        {
            // 1. Önce Kargo Firmasını kaydediyoruz (Identity ID'nin oluşması için)
            _context.Carriers.Add(carrier);
            await _context.SaveChangesAsync();

            
            if (selectedRegionIds != null && selectedRegionIds.Any())
            {
                var carrierRegions = selectedRegionIds.Select(regionId => new CarrierRegion
                {
                    CarrierId = carrier.Id, 
                    RegionId = regionId
                }).ToList();

                _context.CarrierRegions.AddRange(carrierRegions);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        
        var carriers = await _context.Carriers.ToListAsync();
        
        
        ViewBag.Regions = await _context.ShippingRegions.Where(r => (bool)r.IsActive).ToListAsync();
        
        return View("~/Views/ShippingManager/Carrier.cshtml", carriers);
    }


    [HttpPost]
    public async Task<IActionResult> Edit(Carrier carrier, List<int> selectedRegionIds)
    {
        if (ModelState.IsValid)
        {
            var existingCarrier = await _context.Carriers
                .Include(c => c.CarrierRegions)
                .FirstOrDefaultAsync(c => c.Id == carrier.Id);

            if (existingCarrier == null) return NotFound();

            // 1. Temel bilgileri güncelle
            existingCarrier.CarrierName = carrier.CarrierName;
            existingCarrier.IsActive = carrier.IsActive;

            // 2. Eski bölge eşleşmelerini temizle (Yenilerini eklemek için)
            _context.CarrierRegions.RemoveRange(existingCarrier.CarrierRegions);

            // 3. Yeni seçilen bölgeleri ekle
            if (selectedRegionIds != null && selectedRegionIds.Any())
            {
                foreach (var regionId in selectedRegionIds)
                {
                    _context.CarrierRegions.Add(new CarrierRegion
                    {
                        CarrierId = carrier.Id,
                        RegionId = regionId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> AssignRegion(int carrierId, int regionId)
    {
       
        var exists = await _context.CarrierRegions
            .AnyAsync(cr => cr.CarrierId == carrierId && cr.RegionId == regionId);

        if (!exists)
        {
            var mapping = new CarrierRegion { CarrierId = carrierId, RegionId = regionId };
            _context.CarrierRegions.Add(mapping);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(ManageRates), new { carrierId = carrierId });
    }

    // --- GÜNCELLENMİŞ: FİYAT YÖNETİMİ ---


    public async Task<IActionResult> ManageRates(int carrierId)
    {
        // Kargo bilgilerini ve fiyatlarını getiriyoruz
        var carrier = await _context.Carriers
            .Include(c => c.ShippingRates)
                .ThenInclude(r => r.Region)
            .Include(c => c.CarrierRegions) // Bu kargonun hangi bölgelere bağlı olduğunu çek
                .ThenInclude(cr => cr.Region)
            .FirstOrDefaultAsync(c => c.Id == carrierId);

        if (carrier == null) return NotFound();

        // Dropdown için sadece bu kargoya bağlı bölgeleri alıyoruz
        var assignedRegions = carrier.CarrierRegions.Select(cr => cr.Region).ToList();

        var viewModel = new ShippingRateViewModel
        {
            CarrierId = carrier.Id,
            CarrierName = carrier.CarrierName, 
            Regions = assignedRegions,
            ExistingRates = carrier.ShippingRates?.ToList() ?? new List<ShippingRate>()
        };

        return View("~/Views/ShippingManager/ManageRates.cshtml", viewModel);
    }

    

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var carrier = await _context.Carriers
            .Include(c => c.ShippingRates)
            .Include(c => c.CarrierRegions)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (carrier != null)
        {
            // Önce bağlı fiyatları ve eşleşmeleri silmelisiniz (Referential Integrity)
            if (carrier.ShippingRates.Any()) _context.ShippingRates.RemoveRange(carrier.ShippingRates);
            if (carrier.CarrierRegions.Any()) _context.CarrierRegions.RemoveRange(carrier.CarrierRegions);

            _context.Carriers.Remove(carrier);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}