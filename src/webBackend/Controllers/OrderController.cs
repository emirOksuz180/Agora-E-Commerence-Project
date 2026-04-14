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
        var query = _context.Orders
            .Include(o => o.Status) // Yeni eklediğimiz statü tablosunu dahil et
            .AsQueryable();

        // --- Filtreleme Mantığı ---
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

        // --- Pagination (Sayfalama) ---
        var totalItems = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.SiparisTarihi)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // View için gerekli veriler
        ViewBag.Statuses = await _context.OrderStatuses.ToListAsync();
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.CurrentPage = page;
        
        // Dashboard kartları için istatistikler (Filtreden bağımsız genel veriler)
        ViewBag.TotalSales = await _context.Orders.SumAsync(x => x.ToplamFiyat);
        ViewBag.OrderCount = await _context.Orders.CountAsync();
        ViewBag.ProductCount = await _context.Products.CountAsync();

        return View(orders);
    }

    [Authorize]
    public async Task<ActionResult> Details(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Status)
            .Include(o => o.OrderItems).ThenInclude(i => i.Urun)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (order == null) return NotFound();

        bool isAdmin = User.IsInRole("Admin");
        
        // Debug için: Eğer hala sorun yaşıyorsan buradaki değerleri console'a yazdırabilirsin.
        // Console.WriteLine($"DB Email: {order.Email} | Identity Name: {User.Identity.Name}");

        if (!isAdmin)
        {
            // Kullanıcı kendi siparişine mi bakıyor?
            // Identity Name genellikle Email'dir ama bazen Username olabilir. 
            // En sağlıklısı DB'deki email ile tam eşleşme aramak.
            bool isOwner = !string.IsNullOrEmpty(order.Email) && 
                        order.Email.Trim().Equals(User.Identity.Name?.Trim(), StringComparison.OrdinalIgnoreCase);

            if (!isOwner)
            {
                // Buraya düşüyorsa yetki hatası vardır. 
                // Test amaçlı Forbid() yerine ana sayfaya hata mesajıyla yönlendirelim ki ne olduğunu anlayalım.
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

    public async Task<ActionResult> Checkout()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

        var cart = await _cartService.GetCart(username);
        ViewBag.Cart = cart;
        
        var iller = await _context.TblIls.ToListAsync();
        ViewBag.Iller = new SelectList(iller, "Id", "IlAdi");

        

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Checkout(OrderCreateModel model)
    {
        var username = User.Identity?.Name!;
        var cart = await _cartService.GetCart(username);

        // Boşlukları temizle
        if (!string.IsNullOrEmpty(model.CartNumber))
            model.CartNumber = model.CartNumber.Replace(" ", "");


            if (!string.IsNullOrEmpty(model.Sehir))
            {
                model.Sehir = model.Sehir.Trim();
                ModelState.Remove("Sehir"); 
            }

        if (ModelState.IsValid)
        {
            ProcessPaymentResult payment;

            // TOPLAM FİYAT SIFIR İSE DİREKT ONAYLA
            if (cart.Toplam <= 0)
            {
                payment = new ProcessPaymentResult { Status = "success" };
            }
            else
            {
                // Burada ProcessPayment içindeki Iyzico kodlarında 
                // fiyatı 0 olan item'ları listeye eklemediğinden emin ol!
                var iyziResult = await ProcessPayment(model, cart);
                payment = (ProcessPaymentResult)iyziResult;
            }

            if (payment.Status == "success")
            {
                // 1. Ana Sipariş Kaydını Oluşturuyoruz
                var order = new Order { 
                    AdSoyad = model.AdSoyad, 
                    Sehir = model.Sehir, 
                    Username = username, 
                    Email = model.Email ?? username, // Filtreleme için kritik: Modelden gelen email'i kaydet
                    ToplamFiyat = (double)cart.Toplam,
                    SiparisTarihi = DateTime.Now, 
                    AdresSatiri = model.AdresSatiri,
                    Telefon = model.Telefon, 
                    PostaKodu = model.PostaKodu,
                    StatusId = 2 // Ödeme başarılı -> Statü: Onaylandı (Confirmed)
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // OrderId'nin oluşması için önce bunu kaydediyoruz

                // 2. Sipariş Kalemlerini (Ürünleri) Tek Tek Ekliyoruz
                foreach (var item in cart.CartItems) 
                {
                    _context.OrderItems.Add(new OrderItem {
                        OrderId = order.Id, // Az önce oluşan sipariş ID'sini bağlıyoruz
                        UrunId = item.UrunId,
                        Miktar = item.Miktar, 
                        Fiyat = (double)(item.Urun?.Price ?? 0)
                    });
                }

                // 3. Kalemleri ve Sepet Temizliğini Tamamlıyoruz
                await _context.SaveChangesAsync();
                await _cartService.ClearCart();

                return RedirectToAction("Completed", new { orderId = order.Id });
            }

            // Ödeme kuruluşundan gelen hatayı (basketitemprice vb.) Türkçeleştir
            string customError = payment.ErrorMessage;
            if (customError != null && customError.Contains("basketItemPrice"))
                customError = "Siparişinizdeki ücretsiz ürünler nedeniyle ödeme işlemi güncellendi. Lütfen tekrar deneyin.";
                
            ModelState.AddModelError("", customError ?? "Ödeme hatası oluştu.");
        }

        // --- HATA DURUMUNDA SAYFAYI HAZIRLA ---
        var illerList = await _context.TblIls.OrderBy(x => x.IlAdi).ToListAsync();
        // model.Sehir'i burada SelectList'e basmazsan kutu boşa düşer!
        ViewBag.Iller = new SelectList(illerList, "IlAdi", "IlAdi", model.Sehir);
        ViewBag.Cart = cart;
        
        return View(model);
    }
    public ActionResult Completed(int orderId)
    {
        return View("Completed", orderId);
    }

    public async Task<ActionResult> OrderList(DateTime? startDate, DateTime? endDate)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

        var query = _context.Orders
            .Include(i => i.OrderItems)
            .ThenInclude(i => i.Urun)
            .Where(i => i.Username == username);

        // Tarih Filtresi Uygulama
        if (startDate.HasValue)
        {
            query = query.Where(o => o.SiparisTarihi >= startDate.Value);
        }
        
        if (endDate.HasValue)
        {
            // Günü kapsasın diye 23:59:59 yapıyoruz
            var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.SiparisTarihi <= end);
        }

        var orders = await query
            .OrderByDescending(i => i.SiparisTarihi)
            .ToListAsync();

        // View'da tarihleri geri göstermek için
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

        return View(orders);
    }

    private async Task<Payment> ProcessPayment(OrderCreateModel model, Cart cart)
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

        Buyer buyer = new Buyer
        {
            Id = "BY" + User.Identity?.Name,
            Name = model.AdSoyad.Split(' ')[0],
            Surname = model.AdSoyad.Contains(" ") ? model.AdSoyad.Split(' ')[1] : "Customer",
            GsmNumber = model.Telefon,
            Email = "customer@example.com",
            IdentityNumber = "11111111111",
            RegistrationAddress = model.AdresSatiri,
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            City = model.Sehir,
            Country = "Turkey",
            ZipCode = model.PostaKodu
        };
        request.Buyer = buyer;

        Address address = new Address
        {
            ContactName = model.AdSoyad,
            City = model.Sehir,
            Country = "Turkey",
            Description = model.AdresSatiri,
            ZipCode = model.PostaKodu
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
            int oldStatusId = order.StatusId;

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