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
namespace webBackend.Controllers;




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

            ProcessPaymentResult payment;

            if (cart.Toplam <= 0)
            {
                payment = new ProcessPaymentResult { Status = "success" };
            }
            else
            {
                payment = await ProcessPayment(model, cart, finalAd, finalSoyad, finalTelefon, finalSehir, finalAdres, finalZip);
            }

            if (payment.Status == "success")
            {
                // 1. Siparişi Kaydet (Tüm zorunlu alanlar atanmalı - NULL hatasını önler)
                var order = new Order { 
                    Ad = finalAd,
                    Soyad = finalSoyad,
                    Username = username,
                    Email = model.Email ?? username,
                    Telefon = finalTelefon,
                    Sehir = finalSehir,
                    AdresSatiri = finalAdres,
                    PostaKodu = finalZip,
                    ToplamFiyat = (double)cart.Toplam, 
                    SiparisTarihi = DateTime.Now,
                    StatusId = 2 // Onaylandı/Hazırlanıyor
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Burada save yaparak Order ID'yi oluşturuyoruz.

                // 2. Sipariş Kalemlerini (OrderItem) Tek Tek Ekle
                foreach (var cartItem in cart.CartItems)
                {
                    var product = await _context.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ProductId == cartItem.UrunId);

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id, // Az önce kaydedilen Sipariş ID'si
                        UrunId = cartItem.UrunId,
                        Miktar = cartItem.Miktar,
                        
                        // Snapshot: Ürün silinse bile bu bilgiler kalır
                        ProductNameSnapshot = product?.ProductName ?? "Ürün Silinmiş",
                        ProductImageSnapshot = product?.ImageUrl ?? "/img/no-image.jpg",
                        PriceAtOrder = product?.Price ?? 0,
                        ProductCodeSnapshot = product?.ProductId.ToString() ?? "0",
                        Fiyat = (double)(product?.Price ?? 0)
                    };

                    _context.OrderItems.Add(orderItem);
                }

                // 3. Kalemleri Kaydet
                await _context.SaveChangesAsync(); 

                // 4. Sepeti Temizle
                await _cartService.ClearCart();
                return RedirectToAction("Completed", new { orderId = order.Id });
            }

            string customError = payment.ErrorMessage;
            if (customError != null && customError.Contains("basketItemPrice"))
                customError = "Siparişinizdeki fiyat uyumsuzluğu nedeniyle işlem iptal edildi.";
                
            ModelState.AddModelError("", customError ?? "Ödeme hatası oluştu.");
        }

        ReturnView:
        var illerList = await _context.TblIls.OrderBy(x => x.IlAdi).ToListAsync();
        ViewBag.Iller = new SelectList(illerList, "IlAdi", "IlAdi", model.Sehir);
        ViewBag.Cart = cart;
        
        return View(model);
    }
    
    
    
    
    
    public ActionResult Completed(int orderId)
    {
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
        Options options = new Options
        {
            ApiKey = _configuration["PaymentAPI:APIKey"],
            SecretKey = _configuration["PaymentAPI:SecretKey"],
            BaseUrl = "https://sandbox-api.iyzipay.com"
        };

        CreatePaymentRequest request = new CreatePaymentRequest
        {
            Locale = Locale.TR.ToString(),
            ConversationId = Guid.NewGuid().ToString(),
            Price = cart.araToplam.ToString("F2").Replace(",", "."),
            PaidPrice = cart.araToplam.ToString("F2").Replace(",", "."),
            Currency = Currency.TRY.ToString(),
            Installment = 1,
            BasketId = "B" + Guid.NewGuid().ToString().Substring(0, 5),
            PaymentChannel = PaymentChannel.WEB.ToString(),
            PaymentGroup = PaymentGroup.PRODUCT.ToString()
        };

        PaymentCard paymentCard = new PaymentCard
        {
            CardHolderName = model.CartName,
            CardNumber = model.CartNumber,
            ExpireMonth = model.CartExpirationMonth,
            ExpireYear = model.CartExpirationYear,
            Cvc = model.CartCVV,
            RegisterCard = 0
        };
        request.PaymentCard = paymentCard;

        string currentAd, currentSoyad, currentTelefon, currentZip, currentSehir, currentAdres;

        if (model.UseDefaultAddress && model.DefaultAddress != null)
        {
            currentAd = model.DefaultAddress.FirstName;
            currentSoyad = model.DefaultAddress.LastName;
            currentTelefon = model.DefaultAddress.Phone;
            currentZip = model.DefaultAddress.ZipCode ?? "34000"; 
            currentSehir = model.DefaultAddress.City;
            currentAdres = model.DefaultAddress.AddressDetail;
        }
        else
        {
            currentAd = model.Ad;
            currentSoyad = model.Soyad;
            currentTelefon = model.Telefon;
            currentZip = model.PostaKodu;
            currentSehir = model.Sehir;
            currentAdres = model.AdresSatiri;
        }

        // Iyzico Buyer Nesnesi oluşturulurken
        Buyer buyer = new Buyer
        {
            Id = "BY" +  User.Identity?.Name, 
            Name = currentAd,
            Surname = currentSoyad,
            GsmNumber = currentTelefon,
            Email = model.Email,
            IdentityNumber = "11111111111", 
            RegistrationAddress = currentAdres,
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            City = currentSehir,
            Country = "Turkey",
            ZipCode = currentZip
        };
        request.Buyer = buyer;

        Address address = new Address
        {
            
            ContactName = $"{currentAd} {currentSoyad}", 
            City = currentSehir,
            Country = "Turkey",
            Description = currentAdres,
            ZipCode = currentZip
        };
        request.ShippingAddress = address;
        request.BillingAddress = address;

        List<BasketItem> basketItems = new List<BasketItem>();

        foreach (var item in cart.CartItems)
        {
            
            if (item.Urun.Price > 0) 
            {
                BasketItem basketItem = new BasketItem();
                basketItem.Id = item.UrunId.ToString();
                basketItem.Name = item.Urun.ProductName;
                basketItem.Category1 = "Genel";
                basketItem.ItemType = BasketItemType.PHYSICAL.ToString();
                basketItem.Price = item.Urun.Price.ToString().Replace(",", ".");
                basketItems.Add(basketItem);
            }
        }
        request.BasketItems = basketItems;

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