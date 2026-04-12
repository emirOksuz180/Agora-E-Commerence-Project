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
    public async Task<ActionResult> IndexAsync()
    {
        var orders = await _context.Orders.ToListAsync();
        var productCount = await _context.Products.CountAsync();

        ViewBag.TotalSales = orders.Sum(x => x.ToplamFiyat);
        ViewBag.OrderCount = orders.Count;
        ViewBag.ProductCount = productCount;

        return View(orders);
    }

    [Authorize]
    public async Task<ActionResult> Details(int id)
    {
        
        var order = await _context.Orders
                    .Include(i => i.OrderItems)
                    .ThenInclude(i => i.Urun)
                    .FirstOrDefaultAsync(i => i.Id == id);

        if (order == null) return Content("<div class='text-danger'>Sipariş bulunamadı.</div>");

        
        return PartialView("_OrderTablePartial", order);
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
                // --- SİPARİŞ KAYIT İŞLEMLERİ (Önceki kodun aynısı) ---
                var order = new Order { 
                    AdSoyad = model.AdSoyad, Sehir = model.Sehir, 
                    Username = username, ToplamFiyat = (double)cart.Toplam,
                    SiparisTarihi = DateTime.Now, AdresSatiri = model.AdresSatiri,
                    Telefon = model.Telefon, PostaKodu = model.PostaKodu
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cart.CartItems) {
                    _context.OrderItems.Add(new OrderItem {
                        OrderId = order.Id, UrunId = item.UrunId,
                        Miktar = item.Miktar, Fiyat = (double)(item.Urun?.Price ?? 0)
                    });
                }
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