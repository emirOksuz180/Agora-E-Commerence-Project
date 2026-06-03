using System.Threading.Tasks;
using webBackend.Models;
using webBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Iyzipay.Model;
using Iyzipay;
using Iyzipay.Request;
using Microsoft.AspNetCore.Mvc.Rendering;
using OrderItem = webBackend.Models.OrderItem;
using System.Security.Claims;
using System.Linq;

using webBackend.Helpers;
using Microsoft.Data.SqlClient;
namespace webBackend.Controllers;
using System.Data;
using System.Globalization;




[Authorize]
public class OrderController : Controller
{
    private readonly ICartService _cartService;
    private readonly IConfiguration _configuration;
    private readonly AgoraDbContext _context;

    private readonly IEmailService _emailService;

    private readonly ICheckoutService _checkoutService;
    private dynamic cart;

  public OrderController(ICartService cartService, AgoraDbContext context, IConfiguration configuration , IEmailService emailService , ICheckoutService checkoutService)
    {
        _cartService = cartService;
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _checkoutService = checkoutService;
    }

    [Authorize(Policy = "Order.View")]
    public async Task<ActionResult> IndexAsync(string email, int? statusId, DateTime? startDate, DateTime? endDate, int page = 1)
    {
        int pageSize = 10;
        
        // Tarih Validasyonu
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            ModelState.AddModelError("DateError", "Başlangıç tarihi bitiş tarihinden sonra olamaz.");
            endDate = null; 
        }

        var query = _context.Orders
            .Include(o => o.Status) 
            .AsQueryable();

        // Filtreleme
        if (!string.IsNullOrEmpty(email))
            query = query.Where(o => o.Email.Contains(email));

        if (statusId.HasValue)
            query = query.Where(o => o.StatusId == statusId);

        if (startDate.HasValue)
            query = query.Where(o => o.SiparisTarihi >= startDate.Value);

        if (endDate.HasValue)
        {
            var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.SiparisTarihi <= end);
        }

        var totalItems = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.SiparisTarihi)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Statuses = await _context.OrderStatuses.ToListAsync();
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.CurrentPage = page;
        
        // Filtreleri View'a geri gönder
        ViewBag.FilterEmail = email;
        ViewBag.FilterStatusId = statusId;
        ViewBag.FilterStartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.FilterEndDate = endDate?.ToString("yyyy-MM-dd");

        return View(orders);
    }

    [Authorize]
public async Task<ActionResult> Details(int id, DateTime? startDate, DateTime? endDate, string range)
{
    // ====================================================================
    // 1. FİLTRE PARAMETRELERİNİ KORUMA (Geri Dön Butonu İçin)
    // ====================================================================
    var filter = OrderDateFilterHelper.Resolve(startDate, endDate, range);
    if (!string.IsNullOrEmpty(filter.error))
    {
        TempData["ErrorMessage"] = filter.error;
        return RedirectToAction(nameof(OrderList));
    }

    startDate = filter.start;
    endDate = filter.end;

    if (startDate.HasValue && endDate.HasValue && startDate > endDate)
    {
        TempData["ErrorMessage"] = "Geçersiz tarih aralığı tespit edildi.";
        return RedirectToAction(nameof(OrderList));
    }

    if (startDate > DateTime.Today || endDate > DateTime.Today)
    {
        TempData["ErrorMessage"] = "Gelecek tarih filtrelemesi engellendi.";
        return RedirectToAction(nameof(OrderList));
    }

    // Filtre değerlerini ViewBag ile View'a taşıyoruz (Ölü kod olan toplu sorgu kaldırıldı)
    ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
    ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
    ViewBag.Range = range;

    // ====================================================================
    // 2. SİPARİŞ DETAYINI ÇEKME
    // ====================================================================
    var order = await _context.Orders
        .Include(o => o.Status)
        .Include(o => o.OrderItems).ThenInclude(i => i.Urun)
        .FirstOrDefaultAsync(i => i.Id == id);

    if (order == null) return NotFound();

    // ====================================================================
    // 3. GÜVENLİK VE YETKİ KONTROLÜ
    // ====================================================================
    bool isAdmin = User.IsInRole("Admin");

    if (!isAdmin)
    {
        string currentUsername = User.Identity?.Name;
        
        // Eşleşme hatasını önlemek için hem Username hem de Email kontrol ediliyor
        bool isOwner = (!string.IsNullOrEmpty(order.Username) && order.Username.Equals(currentUsername, StringComparison.OrdinalIgnoreCase)) ||
                       (!string.IsNullOrEmpty(order.Email) && order.Email.Trim().Equals(currentUsername?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!isOwner)
        {
            TempData["ErrorMessage"] = "Bu siparişi görüntüleme yetkiniz bulunmamaktadır.";
            return RedirectToAction(nameof(OrderList));
        }
    }

    // ====================================================================
    // 4. ARAYÜZ (LAYOUT) VE ADMİN AYARLARI
    // ====================================================================
    ViewBag.IsAdmin = isAdmin;
    ViewBag.SelectedLayout = isAdmin ? "~/Views/Shared/_AdminLayout.cshtml" : "~/Views/Order/Details.cshtml";

    if (isAdmin)
    {
        // Admin statü değiştirebilsin diye tüm listeyi çekiyoruz
        ViewBag.AllStatuses = await _context.OrderStatuses.ToListAsync();
    }

    return View(order);
}

    [HttpGet]
    public async Task<ActionResult> Checkout()
    {   
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = User.Identity?.Name; 
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return RedirectToAction("Login", "Account");

        // Sepeti getir
        var cart = await _cartService.GetCart(username);
        if (cart == null || !cart.CartItems.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        // Kullanıcının varsayılan adresini getir
        var defaultAddress = await _context.UserAddresses
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);

        // Model oluşturma (AdSoyad kaldırıldı, Ad ve Soyad eklendi)
        var model = new OrderCreateModel
        {
            Ad = defaultAddress?.FirstName ?? "", // Varsa varsayılan adresten getir
            Soyad = defaultAddress?.LastName ?? "", 
            Email = username ?? "", // Genelde UserName email olduğu için doldurabiliriz
            DefaultAddress = defaultAddress,
            UseDefaultAddress = defaultAddress != null,
            Telefon = defaultAddress?.Phone ?? "",
            Sehir = defaultAddress?.City ?? "",
            AdresSatiri = defaultAddress?.AddressDetail ?? "",
            PostaKodu = defaultAddress?.ZipCode ?? ""
        };

        // 1. Şehir Listesi
        var illerList = await _context.TblIls.OrderBy(x => x.IlAdi).ToListAsync();
        ViewBag.Iller = new SelectList(illerList, "IlAdi", "IlAdi");

        // 2. Kargo Firmaları
        var carrierList = await _context.Carriers
            .Where(x => x.IsActive == true)
            .OrderBy(x => x.CarrierName)
            .ToListAsync();

        ViewBag.Carriers = new SelectList(carrierList, "Id", "CarrierName");
        ViewBag.Cart = cart;

        return View(model);
    }

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(OrderCreateModel model)
    {
        var username = User.Identity?.Name!;
        var cart = await _cartService.GetCart(username);

        if (cart == null || !cart.CartItems.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        // --- FORM DOĞRULAMA (Validation) BÖLÜMÜ ---
        if (model.UseDefaultAddress)
        {
            ModelState.Remove("Ad");
            ModelState.Remove("Soyad");
            ModelState.Remove("Email");
            ModelState.Remove("Sehir");
            ModelState.Remove("Telefon");
            ModelState.Remove("AdresSatiri");
            ModelState.Remove("PostaKodu");
            ModelState.Remove("Ilce");
        }

        if (!string.IsNullOrEmpty(model.CartNumber))
        {
            model.CartNumber = model.CartNumber.Replace(" ", "").Trim(); 
            ModelState.Remove("CartNumber");
            if (model.CartNumber.Length < 16)
            {
                ModelState.AddModelError("CartNumber", "Kart numarası eksik veya hatalı.");
            }
        }

        
        if (ModelState.IsValid)
        {
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out int userId);

            // Bütün ağır yükü CheckoutService üstleniyor
            var result = await _checkoutService.ProcessCheckoutAsync(model, cart, userId, username, userIp);

            if (result.IsSuccess)
            {
                // İşlem başarılıysa doğrudan onay sayfasına git
                return RedirectToAction("Completed", new { orderId = result.OrderId });
            }

            // Başarısızsa servisten gelen hatayı ekrana basmak üzere ModelState'e ekle
            ModelState.AddModelError("", result.ErrorMessage ?? "Ödeme işlemi sırasında bir hata oluştu.");
        }

        // --- HATA VARSA VEYA FORM EKSİKSE EKRANI TEKRAR YÜKLE ---
        var illerList = await _context.TblIls.OrderBy(x => x.IlAdi).ToListAsync();
        var carriers = await _context.Carriers.Where(x => x.IsActive == true).ToListAsync();
        
        ViewBag.Iller = new SelectList(illerList, "IlAdi", "IlAdi", model.Sehir);
        ViewBag.Carriers = new SelectList(carriers, "Id", "CarrierName", model.SelectedCarrierId);
        ViewBag.Cart = cart;

        return View(model);
    }
    
    
    
    
    
    // Tekrarlayan view hazırlık kodunu izole etmek için yardımcı metot
    private async Task<ActionResult> PrepareCheckoutView(OrderCreateModel model, Cart? cart)
    {
        var illerList = await _context.TblIls.OrderBy(x => x.IlAdi).ToListAsync();
        var ilcelerList = await _context.TblIlces.OrderBy(x => x.IlceAdi).ToListAsync();
        var carriers = await _context.Carriers.Where(x => x.IsActive == true).ToListAsync();
        
        ViewBag.Iller = new SelectList(illerList, "IlAdi", "IlAdi", model.Sehir);
        ViewBag.Ilceler = new SelectList(ilcelerList , "IlceAdi" , "IlceAdi" , model.Ilce);
        ViewBag.Carriers = new SelectList(carriers, "Id", "CarrierName", model.SelectedCarrierId);
        ViewBag.Cart = cart;
        
        return View(model);
    }

    // Ortak View Hazırlama Metodu (Kod Tekrarını Engeller)
    private async Task<ActionResult> PrepareAndReturnView(OrderCreateModel model, Cart cart)
    {
        var illerList = await _context.TblIls.OrderBy(x => x.IlAdi).ToListAsync();
        var carriers = await _context.Carriers.Where(x => x.IsActive == true).ToListAsync();
        
        ViewBag.Iller = new SelectList(illerList, "IlAdi", "IlAdi", model.Sehir);
        ViewBag.Carriers = new SelectList(carriers, "Id", "CarrierName", model.SelectedCarrierId);
        ViewBag.Cart = cart;
        
        return View(model);
    }
    
    [HttpGet]
    public async Task<JsonResult> GetDistricts(string cityName)
    {
        var city = await _context.TblIls.FirstOrDefaultAsync(x => x.IlAdi == cityName);
        if (city == null) return Json(new List<object>());
        
        var districts = await _context.TblIlces
            .Where(x => x.IlId == city.Id)
            .Select(x => new { id = x.IlceAdi, text = x.IlceAdi })
            .ToListAsync();
            
        return Json(districts);
    }


[HttpGet]
public async Task<JsonResult> GetShippingPrice(int carrierId, string? cityName, string? districtName)
{
    var username = User.Identity?.Name;
    var cart = await _cartService.GetCart(username!);

    if (cart == null) return Json(new { success = false, message = "Sepet bulunamadı." });

    // 1. Dinamik Şehir ve İlçe ID'lerini bulalım (SP'ye doğru parametreleri göndermek için)
    int cityId = 0;
    int districtId = 0;

    if (!string.IsNullOrEmpty(cityName))
    {
        var city = await _context.TblIls.FirstOrDefaultAsync(x => x.IlAdi == cityName);
        if (city != null)
        {
            cityId = city.Id;
            if (!string.IsNullOrEmpty(districtName))
            {
                var district = await _context.TblIlces.FirstOrDefaultAsync(x => x.IlId == city.Id && x.IlceAdi == districtName);
                if (district != null) districtId = district.Id;
            }
        }
    }

    // 2. Kârlılık ve Kargo Hesaplayan Yeni Stored Procedure Parametrelerini Hazırlıyoruz
    var pCartId = new SqlParameter("@CartId", SqlDbType.Int) { Value = cart.CartId };
    var pDistrictId = new SqlParameter("@DistrictId", SqlDbType.Int) { Value = districtId };
    var pCarrierId = new SqlParameter("@SelectedCarrierId", SqlDbType.Int) { Value = carrierId };
    
    // OUTPUT Parametresi: SP içerisinden hesaplanıp dönecek nihai kargo fiyatı (0 veya gerçek maliyet)
    var pOutFinalPrice = new SqlParameter("@FinalShippingPrice", SqlDbType.Decimal) 
    { 
        Direction = ParameterDirection.Output,
        Precision = 18, 
        Scale = 2 
    };

    // OUTPUT Parametresi: SP'den dönecek bedava kargo bayrağı (BIT -> bool)
    var pOutIsFreeShipping = new SqlParameter("@IsFreeShipping", SqlDbType.Bit)
    {
        Direction = ParameterDirection.Output
    };

    // 3. Yeni Kârlılık SP'sini Tetikliyoruz
    // (Not: SP'niz @DistrictId bekliyor, CityId'yi SP içinde kullanmadığınız için doğrudan uygun parametrelerle eşleştirdik)
    await _context.Database.ExecuteSqlRawAsync(
        "EXEC [dbo].[sp_CalculateShippingProfitability] @CartId, @DistrictId, @SelectedCarrierId, @FinalShippingPrice OUTPUT, @IsFreeShipping OUTPUT",
        pCartId, pDistrictId, pCarrierId, pOutFinalPrice, pOutIsFreeShipping);

    // 4. SP Çıktı değerlerini güvenli bir şekilde C# değişkenlerine cast ediyoruz
    decimal finalShippingPrice = (pOutFinalPrice.Value != DBNull.Value) ? (decimal)pOutFinalPrice.Value : 0;
    bool isFreeShipping = (pOutIsFreeShipping.Value != DBNull.Value) && (bool)pOutIsFreeShipping.Value;

    
    return Json(new { 
        success = true, 
        isFreeShipping = isFreeShipping,     
        shippingPrice = finalShippingPrice,   // Kârlılık sağlandıysa 0, sağlanmadıysa gerçek maliyet
        totalPrice = (decimal)cart.Toplam + finalShippingPrice 
    });
}
    
    
    
   public async Task<ActionResult> Completed(int orderId)
    {
        var username = User.Identity?.Name;

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.Username == username);

        if (order == null)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewBag.ShippingInfo = await _context.OrderShippingDetails
            .FirstOrDefaultAsync(s => s.OrderId == orderId);

        return View("Completed", orderId);
    }

    private Options GetIyzipayOptions()
    {
        return new Options
        {
            ApiKey = _configuration["PaymentAPI:APIKey"],
            SecretKey = _configuration["PaymentAPI:SecretKey"],
            BaseUrl = "https://sandbox-api.iyzipay.com" // Canlı ortam için: https://api.iyzipay.com
        };
    }

    // ==========================================
    // 1. SİPARİŞ LİSTELEME
    // ==========================================
    public async Task<ActionResult> OrderList(DateTime? startDate, DateTime? endDate, string range)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

        var filter = OrderDateFilterHelper.Resolve(startDate, endDate, range);

        if (!string.IsNullOrEmpty(filter.error))
        {
            TempData["ErrorMessage"] = filter.error;
            return RedirectToAction(nameof(OrderList));
        }

        startDate = filter.start;
        endDate = filter.end;

        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
        {
            TempData["ErrorMessage"] = "Geçersiz tarih aralığı tespit edildi.";
            return RedirectToAction(nameof(OrderList));
        }

        if (startDate > DateTime.Today || endDate > DateTime.Today)
        {
            TempData["ErrorMessage"] = "Gelecek tarih filtrelemesi engellendi.";
            return RedirectToAction(nameof(OrderList));
        }

        var query = _context.Orders
            .Include(i => i.OrderItems) 
            .Include(i => i.Status)     
            .Where(i => i.Username == username);

        if (startDate.HasValue)
            query = query.Where(o => o.SiparisTarihi >= startDate.Value);
        
        if (endDate.HasValue)
        {
            var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.SiparisTarihi <= end);
        }

        var orders = await query
            .OrderByDescending(i => i.SiparisTarihi)
            .ToListAsync();

        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
        ViewBag.Range = range;

        return View(orders);
    }

    // ==========================================
    // 2. ADMİN PANELİ STATÜ GÜNCELLEME (İptal ve İade Dahil)
    // ==========================================
        [HttpPost]
        [Authorize(Policy = "Order.Edit")]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> UpdateStatus(int orderId, int newStatusId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            
            if (order == null) return NotFound();

            // HATA ÇÖZÜMÜ: order.StatusId null gelme ihtimaline karşı güvenli hale getirildi.
            int oldStatusId = order.StatusId ?? 0;

            // Eğer statü değişmiyorsa işlem yapma
            if (oldStatusId == newStatusId) return RedirectToAction("Index");

            // Güvenli veritabanı stratejisi başlatıyoruz
            var strategy = _context.Database.CreateExecutionStrategy();
            
            try
            {
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    
                    // --- SENARYO A: ADMİN TARAFINDAN İADE ONAYLAMA (7 -> 8 Geçişi) ---
                    if (oldStatusId == 7 && newStatusId == 8)
                    {
                        // Ücretsiz sipariş değilse ve PaymentId varsa Iyzico İade Tetikle
                        if (!string.IsNullOrEmpty(order.PaymentId) && order.PaymentId != "FREE")
                        {
                            // DB'de kırılım (TransactionId) olmadığı için ŞİMDİLİK Cancel ile tam iade yapıyoruz
                            var request = new CreateCancelRequest
                            {
                                Locale = Locale.TR.ToString(),
                                ConversationId = Guid.NewGuid().ToString(),
                                PaymentId = order.PaymentId, // Alt kırılımlara gerek kalmadan ana ödemeyi iade/iptal eder
                                Ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
                            };

                            // HATA ÇÖZÜMÜ: await eklendi
                            Cancel result = await Cancel.Create(request, GetIyzipayOptions());

                            if (result.Status != Status.SUCCESS.ToString())
                            {
                                TempData["ErrorMessage"] = "Iyzico İade Hatası: " + result.ErrorMessage;
                                return RedirectToAction("Index");
                            }
                        }

                        // Ödeme iadesi başarılı ise stokları geri yükle
                        await StoklariGeriYukleAsync(order);
                        TempData["SuccessMessage"] = $"#{orderId} nolu siparişin iadesi onaylandı, ücret karta aktarıldı ve stoklar geri yüklendi.";
                    }

                    // --- SENARYO B: ADMİN TARAFINDAN SİPARİŞ İPTALİ (1 ile 4 arası -> 9 Geçişi) ---
                    else if (oldStatusId <= 4 && newStatusId == 9)
                    {
                        if (!string.IsNullOrEmpty(order.PaymentId) && order.PaymentId != "FREE")
                        {
                            var request = new CreateCancelRequest
                            {
                                Locale = Locale.TR.ToString(),
                                ConversationId = Guid.NewGuid().ToString(),
                                PaymentId = order.PaymentId,
                                Ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
                            };

                            // HATA ÇÖZÜMÜ: await eklendi
                            Cancel result = await Cancel.Create(request, GetIyzipayOptions());

                            if (result.Status != Status.SUCCESS.ToString())
                            {
                                TempData["ErrorMessage"] = "Iyzico İptal Hatası: " + result.ErrorMessage;
                                return RedirectToAction("Index");
                            }
                        }

                        // Ödeme iptali başarılı ise stokları geri yükle
                        await StoklariGeriYukleAsync(order);
                        TempData["SuccessMessage"] = $"#{orderId} nolu sipariş admin tarafından iptal edildi, ücret iade edildi ve stoklar güncellendi.";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = $"#{orderId} nolu sipariş durumu başarıyla güncellendi.";
                    }

                    // Statüyü güncelle ve kaydet
                    order.StatusId = newStatusId;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return RedirectToAction("Index");
                });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Sipariş güncellenirken sistemsel bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

    // ==========================================
    // 3. KULLANICI TARAFINDAN İADE TALEBİ (Sadece 6. Statüdeyken)
    // ==========================================
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UserRequestReturn(int orderId)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return NotFound();

        // Güvenlik Kontrolü (Username bazlı eşitleme yapıldı)
        string currentUsername = User.Identity?.Name;
        if (!string.Equals(order.Username, currentUsername, StringComparison.OrdinalIgnoreCase) && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // Sadece Teslim Edildi (6) durumundaysa İade Talep Edildi (7) olabilir
        if (order.StatusId == 6)
        {
            order.StatusId = 7; 
            await _context.SaveChangesAsync();
            TempData["Message"] = "İade talebiniz başarıyla oluşturuldu. Depo kontrolü sonrası onaylanacaktır.";
        }
        else
        {
            TempData["Error"] = "Bu siparişin mevcut durumundan ötürü iade talebi oluşturulamaz.";
        }

        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    // ==========================================
    // 4. KULLANICI TARAFINDAN SİPARİŞ İPTALİ (2 ile 4 Statüleri Arası)
    // ==========================================
    [HttpPost]
[Authorize]
public async Task<IActionResult> CancelOrder(int orderId)
{
    var order = await _context.Orders
        .Include(o => o.OrderItems)
        .FirstOrDefaultAsync(o => o.Id == orderId);

    if (order == null) return NotFound();

    // Güvenlik Kontrolü (Username bazlı eşitleme yapıldı)
    string currentUsername = User.Identity?.Name;
    if (!string.Equals(order.Username, currentUsername, StringComparison.OrdinalIgnoreCase) && !User.IsInRole("Admin"))
    {
        return Forbid();
    }

    // Kullanıcı 2 (Onaylandı) ile 4 (Hazırlanıyor / Depoda) arasındaki statülerde iptal edebilir
    if (order.StatusId >= 2 && order.StatusId <= 4) 
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Iyzico İptal İşlemi
                if (!string.IsNullOrEmpty(order.PaymentId) && order.PaymentId != "FREE")
                {
                    CreateCancelRequest request = new CreateCancelRequest
                    {
                        Locale = Locale.TR.ToString(),
                        ConversationId = Guid.NewGuid().ToString(),
                        PaymentId = order.PaymentId,
                        Ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
                    };

                    // HATA BURADAYDI: await eklendi
                    Cancel result = await Cancel.Create(request, GetIyzipayOptions());

                    if (result.Status != Status.SUCCESS.ToString())
                    {
                        TempData["Error"] = "Iyzico İptal Hatası: " + result.ErrorMessage;
                        return RedirectToAction(nameof(Details), new { id = orderId });
                    }
                }

                // Statüyü İptal Edildi (9) yap ve Stokları SP ile geri yükle
                order.StatusId = 9; 
                await StoklariGeriYukleAsync(order);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Message"] = "Siparişiniz başarıyla iptal edildi, ücret iadeniz yapıldı ve stoklar güncellendi.";
                return RedirectToAction(nameof(Details), new { id = orderId });
            });
        }
        catch (Exception ex)
        {
            TempData["Error"] = "İptal işlemi sırasında teknik bir hata oluştu: " + ex.Message;
            return RedirectToAction(nameof(Details), new { id = orderId });
        }
    }
    
    TempData["Error"] = "Kargoya verilen veya tamamlanan siparişler iptal edilemez. İade sürecini başlatmanız gerekir.";
    return RedirectToAction(nameof(Details), new { id = orderId });
}

    // ==========================================
    // YARDIMCI METOT: STOKLARI GÜVENLİ TETİKLEME
    // ==========================================
    private async Task StoklariGeriYukleAsync(Order order)
    {
        foreach (var item in order.OrderItems)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_SiparisIptalIcinStokIade @p0, @p1", item.UrunId, item.Miktar);
        }
    }


}





internal class ProcessPaymentResult
{
    public string? Status { get; internal set; }
    public string? ErrorMessage { get; internal set; }
    public string? PaymentId { get; internal set; } 
    public string? ConversationId { get; set; }
    public List<string> TransactionIds { get; internal set; } = new List<string>();

    public static implicit operator ProcessPaymentResult(Payment v)
    {
        var result = new ProcessPaymentResult
        {
            Status = v.Status,
            ErrorMessage = v.Status != "success" ? v.ErrorMessage : null,
            PaymentId = v.PaymentId,
            ConversationId = v.ConversationId
        };

        // Modelindeki PaymentItems listesine erişiyoruz
        if (v.Status == "success" && v.PaymentItems != null)
        {
            result.TransactionIds = v.PaymentItems
                                      .Select(x => x.PaymentTransactionId)
                                      .ToList();
        }

        return result;
    }
}