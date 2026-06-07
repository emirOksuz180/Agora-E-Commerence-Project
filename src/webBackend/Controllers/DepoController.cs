using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;
using Microsoft.Data.SqlClient;

namespace webBackend.Controllers
{
   
    [Authorize(Policy = "Warehouse.View")]
    public class DepoController : Controller
    {
        private readonly AgoraDbContext _context;

        public DepoController(AgoraDbContext context)
        {
            _context = context;
        }

        
        public IActionResult MalKabul() => View();
        
        

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Warehouse.Create")]
        public async Task<IActionResult> ReceiveStockPost(int productId, string locationBarcode, int quantity)
        {
            try
            {
                // 1. İşlemi Yapan Kullanıcıyı Yakala
                // İhtiyacına göre User.Identity.Name veya ClaimTypes.Email kullanabilirsin
                string performedBy = User.Identity?.Name ?? "Bilinmeyen Kullanıcı";

                // 2. Parametreleri Tanımla (Yeni @PerformedBy eklendi)
                var pProductId = new SqlParameter("@ProductId", productId);
                var pQuantity = new SqlParameter("@Quantity", quantity);
                var pLocationBarcode = new SqlParameter("@LocationBarcode", locationBarcode ?? (object)DBNull.Value);
                var pPerformedBy = new SqlParameter("@PerformedBy", performedBy);

                // 3. Güncel SP'yi Çalıştır
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_ReceiveStock @ProductId = @ProductId, @Quantity = @Quantity, @LocationBarcode = @LocationBarcode, @PerformedBy = @PerformedBy", 
                    pProductId, pQuantity, pLocationBarcode, pPerformedBy
                );

                return Json(new { success = true, message = "Ürün başarıyla rafa yerleştirildi." });
            }
            catch (Exception ex)
            {
                // Güvenlik Notu: ex.Message içeriğinde veritabanı yolları vs. dönüyorsa,
                // ileride production ortamı için burayı daha jenerik bir hataya (örn: "Sistemsel bir hata oluştu") çevirebilirsin.
                return Json(new { success = false, message = "İşlem başarısız: " + ex.Message });
            }
        }



        
        public async Task<IActionResult> RafDurumu()
        {
            var rafListesi = await _context.WarehouseLocations
                .Where(wl => wl.IsActive == true) 
                .Select(wl => new RafDurumuViewModel
                {
                    LocationBarcode = wl.LocationBarcode!,
                    MaxVolumeDesi = wl.MaxVolumeDesi,
                    
                   
                    CurrentVolume = _context.Stocks
                        .Where(s => s.LocationId == wl.LocationId)
                        .Sum(s => (decimal?)((s.AvailableQuantity + s.ReservedQuantity + s.DamagedQuantity) * s.Product.Desi)) ?? 0,
                                  
                    TotalItems = _context.Stocks
                        .Where(s => s.LocationId == wl.LocationId)
                        .Sum(s => (int?)s.AvailableQuantity) ?? 0
                })
                .ToListAsync();

            return View(rafListesi);
        }




        [HttpGet]
        [Authorize(Policy = "Permission.Warehouse.View")] 
        public async Task<IActionResult> GetRafDetay(string locationBarcode)
        {
            // Barkoda göre rafı bul ve içindeki stokları ürün bilgileriyle beraber getir
            var stoklar = await _context.Stocks
                .Include(s => s.Location)
                .Include(s => s.Product)
                .Where(s => s.Location.LocationBarcode == locationBarcode && (s.AvailableQuantity > 0 || s.ReservedQuantity > 0))
                .Select(s => new
                {
                    productId = s.ProductId,
                    urunAdi = s.Product.ProductName ?? "Bilinmeyen Ürün", 
                    adet = s.AvailableQuantity,
                    rezerve = s.ReservedQuantity,
                    hasarli = s.DamagedQuantity
                })
                .ToListAsync();

            return Json(stoklar);
        }



       [HttpGet]
[Authorize(Policy = "Warehouse.View")]
public async Task<IActionResult> StokHareketleri(StokHareketFilterViewModel filter, int page = 1)
{
    int pageSize = 20;

    DateTime start = filter.StartDate ?? DateTime.Today.AddDays(-30);
    DateTime end = filter.EndDate ?? DateTime.Today;

    if (start > end)
    {
        var temp = start;
        start = end;
        end = temp;
    }

    DateTime endOfPeriod = end.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

    var query = _context.StockMovements
        .Include(sm => sm.Stock).ThenInclude(s => s.Product)
        .Include(sm => sm.Stock).ThenInclude(s => s.Location)
        .AsQueryable();

    query = query.Where(sm => sm.MovementDate >= start && sm.MovementDate <= endOfPeriod);

    if (!string.IsNullOrEmpty(filter.ProductName))
        query = query.Where(sm => sm.Stock.Product.ProductName.Contains(filter.ProductName));

    if (!string.IsNullOrEmpty(filter.LocationBarcode))
        query = query.Where(sm => sm.Stock.Location.LocationBarcode.Contains(filter.LocationBarcode));

    if (!string.IsNullOrEmpty(filter.MovementType))
        query = query.Where(sm => sm.MovementType == filter.MovementType);

    if (!string.IsNullOrEmpty(filter.PerformedBy))
        query = query.Where(sm => sm.PerformedBy.Contains(filter.PerformedBy));

    query = query.OrderByDescending(sm => sm.MovementId);

    int totalItems = await query.CountAsync();
    int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

    var hareketler = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(sm => new StokHareketViewModel
        {
            ProductName = sm.Stock.Product.ProductName ?? "Tanımsız Ürün",
            LocationBarcode = sm.Stock.Location.LocationBarcode ?? "Belirtilmemiş",
            MovementType = sm.MovementType,
            QuantityChange = sm.QuantityChange,
            MovementDate = sm.MovementDate ?? DateTime.Now,
            PerformedBy = sm.PerformedBy ?? "Sistem"
        })
        .ToListAsync();

    ViewBag.CurrentPage = page;
    ViewBag.TotalPages = totalPages;
    ViewBag.Filter = filter;

    return View(hareketler);
}





      [HttpGet]
    [Authorize(Policy = "Warehouse.View")]
    public async Task<IActionResult> StokYonetimi()
    {
        try
        {
            var rawData = await _context.Database.SqlQueryRaw<RawStockResult>("EXEC sp_GetStockList").ToListAsync();

            var groupedStocks = rawData
                .GroupBy(x => new { x.ProductId, x.ProductName })
                .Select(g => new StockGroupViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    TotalAvailableQuantity = g.Sum(x => x.AvailableQuantity),
                    Locations = g.Select(x => new LocationDetailViewModel
                    {
                        LocationBarcode = x.LocationBarcode,
                        Quantity = x.AvailableQuantity
                    }).ToList()
                })
                .ToList();

            return View(groupedStocks);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Stok listesi yüklenirken bir hata oluştu: " + ex.Message;
            return View(new List<StockGroupViewModel>());
        }
    }



        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Warehouse.Edit")] // Güncelleme yetki poliçen
        public async Task<IActionResult> AdjustStockPost([FromForm] StockAdjustmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Geçersiz veri gönderimi." });
            }

            try
            {
                // İşlemi yapan aktif personeli yakalıyoruz
                string performedBy = User.Identity?.Name ?? "Bilinmeyen Kullanıcı";

                var pProductId = new SqlParameter("@ProductId", model.ProductId);
                var pLocationBarcode = new SqlParameter("@LocationBarcode", model.LocationBarcode ?? (object)DBNull.Value);
                var pNewQuantity = new SqlParameter("@NewQuantity", model.NewQuantity);
                var pPerformedBy = new SqlParameter("@PerformedBy", performedBy);

                // sp_AdjustStock Stored Procedure'ünü tetikliyoruz
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_AdjustStock @ProductId = @ProductId, @LocationBarcode = @LocationBarcode, @NewQuantity = @NewQuantity, @PerformedBy = @PerformedBy",
                    pProductId, pLocationBarcode, pNewQuantity, pPerformedBy
                );

                return Json(new { success = true, message = "Stok miktarı başarıyla güncellendi, fark loglandı." });
            }
            catch (Exception ex)
            {
                // SQL içerisindeki RAISERROR mesajları doğrudan buraya düşer
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // STOK SİLME / KAYIT İPTALİ
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Warehouse.Delete")] 
        public async Task<IActionResult> DeleteStockPost([FromForm] StockDeleteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Geçersiz veri gönderimi." });
            }

            try
            {
                string performedBy = User.Identity?.Name ?? "Bilinmeyen Kullanıcı";

                var pProductId = new SqlParameter("@ProductId", model.ProductId);
                var pLocationBarcode = new SqlParameter("@LocationBarcode", model.LocationBarcode ?? (object)DBNull.Value);
                var pPerformedBy = new SqlParameter("@PerformedBy", performedBy);

                // sp_DeleteStock Stored Procedure'ünü tetikliyoruz
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_DeleteStock @ProductId = @ProductId, @LocationBarcode = @LocationBarcode, @PerformedBy = @PerformedBy",
                    pProductId, pLocationBarcode, pPerformedBy
                );

                return Json(new { success = true, message = "Stok kaydı başarıyla sıfırlandı ve iptal hareketi yazıldı." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



    }
}