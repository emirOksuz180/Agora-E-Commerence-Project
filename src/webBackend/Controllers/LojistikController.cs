using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webBackend.Models; // Kendi namespace'ini yaz

public class LojistikController : Controller
{
    private readonly AgoraDbContext _context;

    public LojistikController(AgoraDbContext context)
    {
        _context = context;
    }

    
    public async Task<IActionResult> BolgeYonetimi()
    {
        
        ViewBag.Bolgeler = await _context.ShippingRegions.ToListAsync();

        
        var iller = await _context.TblIls
            .Include(i => i.Region)
            .OrderBy(i => i.IlAdi)
            .ToListAsync();

        return View(iller);
    }

    
    [HttpPost]
    public async Task<IActionResult> BolgeGuncelle(int ilId, int regionId)
    {
        var il = await _context.TblIls.FindAsync(ilId);
        if (il == null) return Json(new { success = false });

        il.RegionId = regionId;
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    

    [HttpGet]
    public async Task<IActionResult> GetDistricts(int cityId)
    {
        
        var districts = await _context.TblIlces
            .Where(d => d.IlId == cityId)
            .OrderBy(d => d.IlceAdi)
            .Select(d => new { 
                id = d.Id, 
                ilceAdi = d.IlceAdi 
            })
            .ToListAsync();

        return Json(districts);
    }


    // Kargo İstisna Listesi ve Ekleme Ekranı
    // Sadece bir tane kalsın ve isminde 't' olmadığına emin ol
    public async Task<IActionResult> KargoIstisnalari()
    {
        // Dropdownlar için verileri çek
        ViewBag.Carriers = await _context.Carriers.Where(c => c.IsActive == true).ToListAsync();
        ViewBag.Cities = await _context.TblIls.OrderBy(i => i.IlAdi).ToListAsync();

        // İlişkili verileri çek
        var exclusions = await _context.CarrierDistrictExclusions
            .Include(c => c.Carrier)
            .Include(c => c.District)
                .ThenInclude(d => d.Il)
            .ToListAsync();

        return View(exclusions);
    }


    [HttpPost]
    public async Task<IActionResult> IstisnaEkle(int carrierId, int cityId, int? districtId)
    {
        try
        {
            // 1. Mantık Hatası Giderimi: UI'dan 0 (Tüm İl) gelirse null yap
            if (districtId == 0) districtId = null;

            // 2. Kontrol Mantığı: x.Id değil, x.CityId kontrol edilmeli!
            var exists = await _context.CarrierDistrictExclusions
                .AnyAsync(x => x.CarrierId == carrierId && 
                            x.CityId == cityId && 
                            x.DistrictId == districtId);

            if (exists)
            {
                return Json(new { success = false, message = "Bu kısıtlama zaten mevcut." });
            }

            // 3. Kayıt Mantığı: Id'ye dokunma, CityId'ye atama yap!
            var exclusion = new CarrierDistrictExclusion
            {
                CarrierId = carrierId,
                CityId = cityId,     // Doğru kolon burası
                DistrictId = districtId 
            };

            _context.CarrierDistrictExclusions.Add(exclusion);
            await _context.SaveChangesAsync();
            
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            // Hatanın detayını görmek için (Sadece geliştirme aşamasında message'ı dön)
            return Json(new { success = false, message = "Veritabanı Hatası: " + ex.Message });
        }
    }



    [HttpPost]
    public async Task<IActionResult> IstisnaKaldir(int id)
    {
        var exclusion = await _context.CarrierDistrictExclusions.FindAsync(id);
        if (exclusion == null) return Json(new { success = false, message = "Kayıt bulunamadı." });

        _context.CarrierDistrictExclusions.Remove(exclusion);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }
 }