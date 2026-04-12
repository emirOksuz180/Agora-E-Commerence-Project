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

    [Authorize(Policy = "Order.View")]
    public async Task<ActionResult> Details(int id)
    {
        // Debug için: Buraya bir breakpoint koyup id geliyor mu bakabilirsin.
        var order = await _context.Orders
                    .Include(i => i.OrderItems)
                    .ThenInclude(i => i.Urun)
                    .FirstOrDefaultAsync(i => i.Id == id);

        if (order == null) return Content("<div class='text-danger'>Sipariş bulunamadı.</div>");

        // View isminin doğruluğundan emin ol: _OrderTablePartial.cshtml
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

        if (cart.CartItems.Count == 0)
        {
            ModelState.AddModelError("", "Sepetinizde ürün yok");
        }

        ViewBag.Cart = cart;

        var order = new Order
        {
            AdSoyad = model.AdSoyad,
            Telefon = model.Telefon,
            AdresSatiri = model.AdresSatiri,
            PostaKodu = model.PostaKodu,
            Sehir = model.Sehir,
            SiparisNotu = model.SiparisNotu ?? "",
            SiparisTarihi = DateTime.Now,
            Username = username,
            ToplamFiyat = (double)cart.Toplam // Önce fiyata emin oluyoruz
        };

        // Validasyon temizliği (Ücretsiz ürünler için)
        if (order.ToplamFiyat == 0)
        {
            ModelState.ClearValidationState("ToplamFiyat");
            ModelState.MarkFieldValid("ToplamFiyat");
            var itemFiyatKeys = ModelState.Keys.Where(k => k.Contains("Fiyat")).ToList();
            foreach (var key in itemFiyatKeys)
            {
                ModelState.ClearValidationState(key);
                ModelState.MarkFieldValid(key);
            }
        }

        if (ModelState.IsValid)
        {
            ProcessPaymentResult payment;
            if (order.ToplamFiyat == 0)
            {
                payment = new ProcessPaymentResult { Status = "success" };
            }
            else
            {
                payment = await ProcessPayment(model, cart);
            }

            if (payment.Status == "success")
            {
                // KRİTİK DÜZELTME: Önce Siparişi Kaydet ki ID oluşsun
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // KRİTİK DÜZELTME: Kalemleri şimdi tek tek ve doğru fiyatla ekliyoruz
                foreach (var item in cart.CartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id, 
                        UrunId = item.UrunId,
                        Miktar = item.Miktar,
                        // Eğer ürün null ise fiyat 0 kalmasın diye kontrol ekliyoruz
                        Fiyat = (double)(item.Urun?.Price ?? 0) 
                    };
                    _context.OrderItems.Add(orderItem);
                }

                await _context.SaveChangesAsync();

                // 4. Sepeti Temizle
                await _cartService.ClearCart();

                try 
                {
                    var orderListUrl = Url.Action("OrderList", "Order", null, Request.Scheme);
                    string mailSubject = $"Sipariş Onayı - #{order.Id}";
                    string mailBody = $@"
                        <div style='font-family: sans-serif; background-color: #f8f9fa; padding: 20px;'>
                            <div style='max-width: 600px; margin: 0 auto; background: white; border-radius: 15px; overflow: hidden; border: 1px solid #dee2e6;'>
                                <div style='background: linear-gradient(135deg, #6610f2, #8b5cf6); padding: 25px; text-align: center; color: white;'>
                                    <h1 style='margin: 0;'>Agora E-Commerce</h1>
                                </div>
                                <div style='padding: 25px;'>
                                    <h2 style='color: #6610f2;'>Merhaba {model.AdSoyad},</h2>
                                    <p>Siparişiniz (<strong>#{order.Id}</strong>) başarıyla alındı ve onaylandı.</p>
                                    <p style='font-size: 18px;'>Toplam Tutar: <strong>{(order.ToplamFiyat == 0 ? "Ücretsiz" : order.ToplamFiyat.ToString("N2") + " ₺")}</strong></p>
                                    <div style='text-align: center; margin-top: 25px;'>
                                        <a href='{orderListUrl}' style='background: #6610f2; color: white; padding: 12px 25px; text-decoration: none; border-radius: 8px; font-weight: bold;'>Siparişlerimi Görüntüle</a>
                                    </div>
                                </div>
                            </div>
                        </div>";

                    await _emailService.SendEmailAsync(username, mailSubject, mailBody);
                }
                catch { }

                return RedirectToAction("Completed", new { orderId = order.Id });
            }
            else
            {
                ModelState.AddModelError("", payment.ErrorMessage ?? "Ödeme işlemi sırasında bir hata oluştu.");
            }
        }

        var illerList = await _context.TblIls.OrderBy(x => x.IlAdi).ToListAsync();
        ViewBag.Iller = new SelectList(illerList, "IlAdi", "IlAdi");
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
            basketItems.Add(new BasketItem
            {
                Id = item.UrunId.ToString(),
                Name = item.Urun.ProductName,
                Category1 = "General",
                ItemType = BasketItemType.PHYSICAL.ToString(),
                Price = item.Urun.Price.ToString("F2").Replace(",", ".")
            });
        }
        request.BasketItems = basketItems;

        return await Payment.Create(request, options);
    }
}

internal class ProcessPaymentResult
{
  public string ?Status { get; internal set; }
  public string? ErrorMessage { get; internal set; }

  public static implicit operator ProcessPaymentResult(Payment v)
  {
        return new ProcessPaymentResult
        {
            // IsSuccess yerine Status kontrolü yapıyoruz
            Status = v.Status == "success" ? "success" : "failed"
        };
  }
}