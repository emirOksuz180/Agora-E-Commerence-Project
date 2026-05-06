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




[Authorize]
public class OrderController : Controller
{
    private readonly ICartService _cartService;
    private readonly IConfiguration _configuration;
    private readonly AgoraDbContext _context;

    private readonly IEmailService _emailService;
    private dynamic cart;

  public OrderController(ICartService cartService, AgoraDbContext context, IConfiguration configuration , IEmailService emailService)
    {
        _cartService = cartService;
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
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
    public async Task<ActionResult> Details(int id , DateTime? startDate,DateTime? endDate,string range)
    {

        var filter = OrderDateFilterHelper.Resolve(startDate, endDate, range);
         var username = User.Identity?.Name;
        if (!string.IsNullOrEmpty(filter.error))
        {
            TempData["ErrorMessage"] = filter.error;
            return RedirectToAction(nameof(OrderList));
        }

        startDate = filter.start;
        endDate = filter.end;


        if (startDate.HasValue && endDate.HasValue)
        {
            if (startDate > endDate)
            {
                TempData["ErrorMessage"] = "Geçersiz tarih aralığı tespit edildi.";
                return RedirectToAction(nameof(OrderList));
            }
        }

        if (startDate > DateTime.Today || endDate > DateTime.Today)
        {
            TempData["ErrorMessage"] = "Gelecek tarih filtrelemesi engellendi.";
            return RedirectToAction(nameof(OrderList));
        }

        var query = _context.Orders
            .Include(i => i.OrderItems)
            .ThenInclude(i => i.Urun)
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


        var order = await _context.Orders
            .Include(o => o.Status)
            .Include(o => o.OrderItems).ThenInclude(i => i.Urun)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (order == null) return NotFound();

        bool isAdmin = User.IsInRole("Admin");
   

        if (!isAdmin)
        {
            
            bool isOwner = !string.IsNullOrEmpty(order.Email) && 
                        order.Email.Trim().Equals(User.Identity.Name?.Trim(), StringComparison.OrdinalIgnoreCase);

            if (!isOwner)
            {
                
                TempData["Error"] = "Bu siparişi görüntüleme yetkiniz bulunmamaktadır.";
                return RedirectToAction("Index", "Home");
            }
        }

        // Layout belirleme
        ViewBag.IsAdmin = isAdmin;
        ViewBag.SelectedLayout = isAdmin ? "~/Views/Shared/_AdminLayout.cshtml" : "~/Views/Order/Details.cshtml";

        if (isAdmin)
        {
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
    public async Task<ActionResult> Checkout(OrderCreateModel model)
    {
        var username = User.Identity?.Name!;
        var cart = await _cartService.GetCart(username);

        // Adres doğrulama bypass (Default adres seçiliyse form validation'ı temizle)
        if (model.UseDefaultAddress)
        {
            ModelState.Remove("Ad");
            ModelState.Remove("Soyad");
            ModelState.Remove("Email");
            ModelState.Remove("Sehir");
            ModelState.Remove("Telefon");
            ModelState.Remove("AdresSatiri");
            ModelState.Remove("PostaKodu");
        }

        if (!string.IsNullOrEmpty(model.CartNumber))
            model.CartNumber = model.CartNumber.Replace(" ", "");

        if (ModelState.IsValid)
        {
            string finalAd, finalSoyad, finalTelefon, finalSehir, finalAdres, finalZip;

            if (model.UseDefaultAddress)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdClaim, out int userId);
                
                var defaultAddress = await _context.UserAddresses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);

                if (defaultAddress == null || string.IsNullOrEmpty(defaultAddress.FirstName))
                {
                    ModelState.AddModelError("", "Varsayılan adres bilgileriniz eksik. Lütfen yeni adres girin.");
                    goto ReturnView;
                }

                finalAd = defaultAddress.FirstName;
                finalSoyad = defaultAddress.LastName;
                finalTelefon = defaultAddress.Phone;
                finalSehir = defaultAddress.City;
                finalAdres = defaultAddress.AddressDetail;
                finalZip = defaultAddress.ZipCode ?? "34000";
            }
            else
            {
                finalAd = model.Ad;
                finalSoyad = model.Soyad;
                finalTelefon = model.Telefon;
                finalSehir = model.Sehir;
                finalAdres = model.AdresSatiri;
                finalZip = model.PostaKodu;
            }

            // --- KARGO DOĞRULAMA VE HESAPLAMA (SP İLE) ---
            var city = await _context.TblIls.FirstOrDefaultAsync(x => x.IlAdi == finalSehir);
            // Not: District eşleşmesi için AdresSatiri kullanılmış, 
            // eğer ilçeyi ayrı tutuyorsan burayı o değişkene göre güncellemelisin.
            var district = await _context.TblIlces.FirstOrDefaultAsync(x => x.IlceAdi == finalAdres && x.IlId == city.Id);

            if (city == null)
            {
                ModelState.AddModelError("", "Seçilen şehir sistemde doğrulanamadı.");
                goto ReturnView;
            }

            
            var pCartId = new SqlParameter("@CartId", cart.CartId);
            var pCityId = new SqlParameter("@CityId", city.Id);
            var pDistrictId = new SqlParameter("@DistrictId", district?.Id ?? (object)DBNull.Value);
            var pCarrierId = new SqlParameter("@SelectedCarrierId", model.SelectedCarrierId);
            var pOutPrice = new SqlParameter { 
                ParameterName = "@FinalShippingPrice", 
                SqlDbType = SqlDbType.Decimal, 
                Direction = ParameterDirection.Output,
                Precision = 18, Scale = 2 
            };


            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [dbo].[sp_VerifyAndGetShippingPrice] @CartId, @CityId, @DistrictId, @SelectedCarrierId, @FinalShippingPrice OUTPUT",
                pCartId, pCityId, pDistrictId, pCarrierId, pOutPrice);

            decimal finalKargoUcreti = 0;
            if (pOutPrice.Value != null && pOutPrice.Value != DBNull.Value)
            {
                finalKargoUcreti = Convert.ToDecimal(pOutPrice.Value);
            }
            else
            {
                finalKargoUcreti = 75; 
            }

            if (finalKargoUcreti == -1) {
                ModelState.AddModelError("", "Seçilen kargo firması bu bölgeye hizmet verememektedir.");
                goto ReturnView;
            }

            // Toplam tutarı güncelle (Sepet + Kargo)
            double genelToplam = (double)cart.Toplam + (double)finalKargoUcreti;

            // --- ÖDEME SÜRECİ ---
            ProcessPaymentResult payment;

            if (genelToplam <= 0)
            {
                payment = new ProcessPaymentResult { Status = "success" };
            }
            else
            {
                
                payment = await ProcessPayment(model, cart, finalAd, finalSoyad, finalTelefon, finalSehir, finalAdres, finalZip);
            }

            if (payment.Status == "success")
            {
                // 1. Siparişi Kaydet
                var order = new Order { 
                    Ad = finalAd,
                    Soyad = finalSoyad,
                    Username = username,
                    Email = model.Email ?? username,
                    Telefon = finalTelefon,
                    Sehir = finalSehir,
                    AdresSatiri = finalAdres,
                    PostaKodu = finalZip,
                    ToplamFiyat = genelToplam, 
                    SiparisTarihi = DateTime.Now,
                    StatusId = 2 
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); 


                var shippingDetail = new OrderShippingDetail 
                {
                    OrderId = order.Id, 
                    CarrierId = model.SelectedCarrierId,
                    ShippingPrice = finalKargoUcreti,
                    CreatedAt = DateTime.Now
                };
                _context.OrderShippingDetails.Add(shippingDetail);

                // 2. Sipariş Kalemlerini Ekle
                foreach (var cartItem in cart.CartItems)
                {
                    var product = await _context.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ProductId == cartItem.UrunId);

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id, 
                        UrunId = cartItem.UrunId,
                        Miktar = cartItem.Miktar,
                        ProductNameSnapshot = product?.ProductName ?? "Ürün Silinmiş",
                        ProductImageSnapshot = product?.ImageUrl ?? "/img/no-image.jpg",
                        PriceAtOrder = product?.Price ?? 0,
                        ProductCodeSnapshot = product?.ProductId.ToString() ?? "0",
                        Fiyat = (double)(product?.Price ?? 0)
                    };

                    _context.OrderItems.Add(orderItem);
                }

                await _context.SaveChangesAsync(); 
                await _cartService.ClearCart();
                return RedirectToAction("Completed", new { orderId = order.Id });
            }

            string customError = payment.ErrorMessage;
            if (customError != null && customError.Contains("basketItemPrice"))
                customError = "Fiyat uyumsuzluğu nedeniyle işlem iptal edildi.";
                
            ModelState.AddModelError("", customError ?? "Ödeme hatası oluştu.");
        }

        ReturnView:
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
public async Task<JsonResult> GetShippingPrice(int carrierId)
{
    var username = User.Identity?.Name;
    var cart = await _cartService.GetCart(username!);

    if (cart == null) return Json(new { success = false });

    // Parametreleri tipleriyle birlikte (SqlDbType.Int) açıkça tanımlıyoruz
    var pCartId = new SqlParameter("@CartId", SqlDbType.Int) { Value = cart.CartId };
    var pCityId = new SqlParameter("@CityId", SqlDbType.Int) { Value = 0 }; 
    var pDistrictId = new SqlParameter("@DistrictId", SqlDbType.Int) { Value = 0 };
    var pCarrierId = new SqlParameter("@SelectedCarrierId", SqlDbType.Int) { Value = carrierId };
    
    var pOutPrice = new SqlParameter("@FinalShippingPrice", SqlDbType.Decimal) 
    { 
        Direction = ParameterDirection.Output,
        Precision = 18, 
        Scale = 2 
    };

    // Açıkça tanımladığımız parametreleri ExecuteSqlRawAsync içine veriyoruz
    await _context.Database.ExecuteSqlRawAsync(
        "EXEC [dbo].[sp_VerifyAndGetShippingPrice] @CartId, @CityId, @DistrictId, @SelectedCarrierId, @FinalShippingPrice OUTPUT",
        pCartId, 
        pCityId, 
        pDistrictId, 
        pCarrierId, 
        pOutPrice);

    decimal price = (pOutPrice.Value != DBNull.Value) ? (decimal)pOutPrice.Value : 0;

    return Json(new { 
        success = true, 
        shippingPrice = price, 
        totalPrice = (decimal)cart.Toplam + price 
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

    public async Task<ActionResult> OrderList(DateTime? startDate, DateTime? endDate, string range)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

        // Yardımcı sınıf ile tarihleri çöz
        var filter = OrderDateFilterHelper.Resolve(startDate, endDate, range);

        if (!string.IsNullOrEmpty(filter.error))
        {
            TempData["ErrorMessage"] = filter.error;
            return RedirectToAction(nameof(OrderList));
        }

        startDate = filter.start;
        endDate = filter.end;

        // Tarih Mantık Kontrolleri
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

        // QUERY GÜNCELLEMESİ: .ThenInclude(i => i.Urun) kaldırıldı!
        // Çünkü isim ve resim bilgilerini artık OrderItem içindeki Snapshot'lardan alacağız.
        var query = _context.Orders
            .Include(i => i.OrderItems) // Sadece kalemleri al, Urun tablosuna gitme
            .Include(i => i.Status)     // Durum badge'leri için status lazım
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

    private async Task<Payment> ProcessPayment(OrderCreateModel model, 
    Cart cart, 
    string finalAd, 
    string finalSoyad, 
    string finalTelefon, 
    string finalSehir, 
    string finalAdres, 
    string finalZip)
{
    // 1. İyzipay Seçenekleri (BaseUrl'e dikkat)
    Options options = new Options
    {
        ApiKey = _configuration["PaymentAPI:APIKey"],
        SecretKey = _configuration["PaymentAPI:SecretKey"],
        BaseUrl = "https://sandbox-api.iyzipay.com" 
    };

    // 2. Tutar Hesaplama (Kargo Dahil Genel Toplam)
    // cart.Toplam'ın kargo eklenmiş son hal olduğunu varsayıyoruz
    string totalFormatted = cart.Toplam.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

    CreatePaymentRequest request = new CreatePaymentRequest
    {
        Locale = Locale.TR.ToString(),
        ConversationId = Guid.NewGuid().ToString(),
        Price = totalFormatted,
        PaidPrice = totalFormatted,
        Currency = Currency.TRY.ToString(),
        Installment = 1,
        BasketId = "B" + Guid.NewGuid().ToString().Substring(0, 5),
        PaymentChannel = PaymentChannel.WEB.ToString(),
        PaymentGroup = PaymentGroup.PRODUCT.ToString()
    };

    // 3. Kart Bilgileri
    request.PaymentCard = new PaymentCard
    {
        CardHolderName = model.CartName,
        CardNumber = model.CartNumber,
        ExpireMonth = model.CartExpirationMonth,
        ExpireYear = model.CartExpirationYear,
        Cvc = model.CartCVV,
        RegisterCard = 0
    };

    // 4. Buyer (Alıcı) Bilgileri
    request.Buyer = new Buyer
    {
        Id = "BY" + (User.Identity?.Name ?? "Guest"), 
        Name = finalAd,
        Surname = finalSoyad,
        GsmNumber = finalTelefon,
        Email = model.Email ?? "test@test.com",
        IdentityNumber = "11111111111", 
        RegistrationAddress = finalAdres,
        Ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
        City = finalSehir,
        Country = "Turkey",
        ZipCode = finalZip
    };

    // 5. Adres Bilgileri
    Address shippingAddress = new Address
    {
        ContactName = $"{finalAd} {finalSoyad}", 
        City = finalSehir,
        Country = "Turkey",
        Description = finalAdres,
        ZipCode = finalZip
    };
    request.ShippingAddress = shippingAddress;
    request.BillingAddress = shippingAddress;

    // 6. Sepet Kalemleri (Ürünler + Kargo)
    List<BasketItem> basketItems = new List<BasketItem>();

    foreach (var item in cart.CartItems)
    {
        if (item.Urun.Price > 0) 
        {
            basketItems.Add(new BasketItem
            {
                Id = item.UrunId.ToString(),
                Name = item.Urun.ProductName,
                Category1 = "Genel",
                ItemType = BasketItemType.PHYSICAL.ToString(),
                Price = item.Urun.Price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
            });
        }
    }

    // KRİTİK: Kargo ücretini sepet kalemi olarak ekle (Fiyat dengesi için)
    decimal kargoUcreti = (decimal)(cart.Toplam - cart.araToplam);
    if (kargoUcreti > 0)
    {
        basketItems.Add(new BasketItem
        {
            Id = "SHIPPING",
            Name = "Kargo Ücreti",
            Category1 = "Lojistik",
            ItemType = BasketItemType.VIRTUAL.ToString(),
            Price = kargoUcreti.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
        });
    }

    request.BasketItems = basketItems;

    // 7. Ödeme Oluşturma
    return await Payment.Create(request, options);
}


        [HttpPost]
        [Authorize(Policy = "Order.Edit")]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> UpdateStatus(int orderId, int newStatusId)
        {
            // OrderItems ve Urun bilgilerini de çekmemiz gerekiyor (Stok güncellemesi için)
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Urun)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            
            if (order == null) 
            {
                return NotFound();
            }

            // Mevcut statüyü yedekleyelim (Değişim kontrolü için)
            int ?oldStatusId = order.StatusId;

            // --- STOK YÖNETİMİ MANTIĞI ---

            // 1. İade Tamamlandığında (7 -> 8 geçişi) stokları geri al
            if (oldStatusId == 7 && newStatusId == 8)
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.Urun != null)
                    {
                        item.Urun.Stock += item.Miktar;
                    }
                }
                TempData["SuccessMessage"] = $"#{orderId} nolu siparişin iadesi onaylandı ve stoklar geri yüklendi.";
            }
            // 2. Sipariş İptal Edildiğinde (1-4 arası -> 9 geçişi) stokları geri al
            else if (oldStatusId < 5 && newStatusId == 9)
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.Urun != null)
                    {
                        item.Urun.Stock += item.Miktar;
                    }
                }
                TempData["SuccessMessage"] = $"#{orderId} nolu sipariş iptal edildi ve stoklar güncellendi.";
            }

            // Statü güncelleme
            order.StatusId = newStatusId;
            
            try 
            {
                await _context.SaveChangesAsync();
                
                // Eğer yukarıdaki özel mesajlar dolmadıysa genel başarı mesajını ver
                if (TempData["SuccessMessage"] == null)
                {
                    TempData["SuccessMessage"] = $"#{orderId} nolu siparişin durumu başarıyla güncellendi.";
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Sipariş güncellenirken bir hata oluştu.";
            }

            return RedirectToAction("Index"); 
        }


    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UserRequestReturn(int orderId)
    {
        // Siparişi çek
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return NotFound();

        
        string currentUserEmail = User.Identity.Name;
        if (order.Email != currentUserEmail && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        
        if (order.StatusId == 6)
        {
            order.StatusId = 7; 
            await _context.SaveChangesAsync();
            TempData["Message"] = "İade talebiniz başarıyla oluşturuldu.";
        }
        else
        {
            TempData["Error"] = "Bu sipariş için şu an iade talebi oluşturulamaz.";
        }

        return RedirectToAction(nameof(Details), new { id = orderId });
    }


    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CancelOrder(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return NotFound();

        // Güvenlik kontrolü (Mail uyuşmazsa iptal edemesin)
        if (!User.IsInRole("Admin") && !order.Email.ToLower().Equals(User.Identity.Name.ToLower()))
        {
            return Forbid();
        }

        if (order.StatusId <= 4) // 1,2,3,4 ise iptal edebilir
        {
            order.StatusId = 9; // Cancelled
            
            // Stok iadesi
            foreach (var item in order.OrderItems)
            {
                var urun = await _context.Products.FindAsync(item.UrunId);
                if (urun != null) urun.Stock += item.Miktar;
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Siparişiniz iptal edildi.";
        }
        else
        {
            TempData["Error"] = "Bu aşamada sipariş iptal edilemez.";
        }

        // Details sayfasına ID ile geri dön (Bu sayede Details metodu tekrar çalışır ve Layout yüklenir)
        return RedirectToAction(nameof(Details), new { id = orderId });
    }



}





internal class ProcessPaymentResult
{
    public string? Status { get; internal set; }
    public string? ErrorMessage { get; internal set; } // Bu alanı doldurmamız lazım

    public static implicit operator ProcessPaymentResult(Payment v)
    {
        return new ProcessPaymentResult
        {
            // Iyzico'nun kendi Status değerini (success/failure) direkt alıyoruz
            Status = v.Status, 
            
            // Burası kritik: Eğer hata varsa Iyzico'nun gönderdiği mesajı çekiyoruz
            ErrorMessage = v.Status != "success" ? v.ErrorMessage : null
        };
    }
}