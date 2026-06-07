using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using webBackend.Models;
using Iyzipay.Model;
using webBackend.Services;
using webBackend.Controllers;
using Iyzipay.Request;
using Microsoft.Extensions.Configuration;
using System.Globalization;

public class CheckoutService : ICheckoutService
{
    private readonly AgoraDbContext _context;
    private readonly ICartService _cartService;
    private readonly IConfiguration _configuration;

    public CheckoutService(AgoraDbContext context, ICartService cartService, IConfiguration configuration)
    {
        _context = context;
        _cartService = cartService;
        _configuration = configuration;
    }

    public async Task<CheckoutResult> ProcessCheckoutAsync(OrderCreateModel model, string username, int userId)
    {
        var cart = await _cartService.GetCart(username);
        if (cart == null || !cart.CartItems.Any())
            return new CheckoutResult { IsSuccess = false, ErrorMessage = "Sepetiniz boş." };

        return await ProcessCheckoutAsync(model, cart, userId, username, "127.0.0.1");
    }

    public async Task<CheckoutResult> ProcessCheckoutAsync(OrderCreateModel model, Cart cart, int userId, string username, string userIp)
{
    try
    {
        // 1. ADRES ÇÖZÜMLEME: Controller'dan gelen veriyi doğrudan kullanıyoruz.
        string finalAd = model.Ad;
        string finalSoyad = model.Soyad;
        string finalTelefon = model.Telefon;
        string finalSehir = model.Sehir;
        string finalAdres = model.AdresSatiri;
        string finalZip = !string.IsNullOrEmpty(model.PostaKodu) ? model.PostaKodu : "34000";
        string finalIlce = model.Ilce; // Formdan gelen net ilçe bilgisi

        // Güvenlik: Adres verilerinin eksiksiz olduğundan emin oluyoruz.
        if (string.IsNullOrEmpty(finalAd) || string.IsNullOrEmpty(finalAdres) || string.IsNullOrEmpty(finalSehir) || string.IsNullOrEmpty(finalIlce))
        {
            return new CheckoutResult { IsSuccess = false, ErrorMessage = "Teslimat adresi bilgileri eksik (İl/İlçe/Adres)." };
        }

        // 2. KARGO HESAPLAMA (Doğrulanmış verilerle)
        var city = await _context.TblIls.FirstOrDefaultAsync(x => x.IlAdi == finalSehir);
        if (city == null) return new CheckoutResult { IsSuccess = false, ErrorMessage = "Seçilen şehir sistemde doğrulanamadı." };

        // İlçeyi direkt isimle ve şehir ID'si ile eşleştiriyoruz
        var district = await _context.TblIlces.FirstOrDefaultAsync(x => x.IlceAdi == finalIlce && x.IlId == city.Id);
        
        // Eğer ilçe veritabanında yoksa, siparişin yanlış kargo hesabıyla oluşmasını engellemek için durduruyoruz.
        if (district == null) return new CheckoutResult { IsSuccess = false, ErrorMessage = "Seçilen ilçe sistemde doğrulanamadı." };

        var pCartId = new SqlParameter("@CartId", cart.CartId);
        var pDistrictId = new SqlParameter("@DistrictId", district.Id);
        var pCarrierId = new SqlParameter("@SelectedCarrierId", model.SelectedCarrierId);
        var pOutPrice = new SqlParameter("@FinalShippingPrice", SqlDbType.Decimal) { Direction = ParameterDirection.Output, Precision = 18, Scale = 2 };
        var pIsFree = new SqlParameter("@IsFreeShipping", SqlDbType.Bit) { Direction = ParameterDirection.Output };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC [dbo].[sp_CalculateShippingProfitability] @CartId, @DistrictId, @SelectedCarrierId, @FinalShippingPrice OUTPUT, @IsFreeShipping OUTPUT",
            pCartId, pDistrictId, pCarrierId, pOutPrice, pIsFree);

        decimal finalKargoUcreti = (pOutPrice.Value != DBNull.Value) ? Convert.ToDecimal(pOutPrice.Value) : 75;
        if (finalKargoUcreti < 0) return new CheckoutResult { IsSuccess = false, ErrorMessage = "Kargo hizmeti verilememektedir." };

        double genelToplam = (double)cart.Toplam + (double)finalKargoUcreti;

        // 3. IYZICO ÖDEME SÜRECİ
        ProcessPaymentResult payment = (genelToplam <= 0) 
            ? new ProcessPaymentResult { Status = "success", PaymentId = "FREE", ConversationId = "FREE" }
            : await ProcessPaymentInternalAsync(model, cart, finalAd, finalSoyad, finalTelefon, finalSehir, finalAdres, finalZip, finalKargoUcreti, userIp, username);

        if (payment == null || payment.Status != "success")
            return new CheckoutResult { IsSuccess = false, ErrorMessage = payment?.ErrorMessage ?? "Ödeme hatası oluştu." };

        // 4. İŞLEM (Sipariş Kaydı + Stok Düşme)
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    Username = username,
                    Ad = finalAd,
                    Soyad = finalSoyad,
                    Telefon = finalTelefon,
                    Email = model.Email ?? username,
                    Sehir = finalSehir,
                    AdresSatiri = finalAdres,
                    PostaKodu = finalZip,
                    ToplamFiyat = genelToplam,
                    SiparisTarihi = DateTime.Now,
                    StatusId = 2,
                    PaymentId = payment.PaymentId,
                    ConversationId = payment.ConversationId,
                    SiparisNotu = model.SiparisNotu
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _context.OrderShippingDetails.Add(new OrderShippingDetail
                {
                    OrderId = order.Id,
                    CarrierId = model.SelectedCarrierId,
                    ShippingPrice = finalKargoUcreti,
                    CreatedAt = DateTime.Now
                });

                foreach (var cartItem in cart.CartItems)
                {
                    var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == cartItem.UrunId);

                    // 1. Stok Düşme SP Çağrısı
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC sp_SiparisIcinStokDus @ProductId = {0}, @SiparisMiktari = {1}", 
                        cartItem.UrunId, cartItem.Miktar);

                    // 2. OrderItem Ekleme
                    _context.OrderItems.Add(new webBackend.Models.OrderItem
                    {
                        OrderId = order.Id,
                        UrunId = cartItem.UrunId,
                        Miktar = cartItem.Miktar,
                        ProductNameSnapshot = product?.ProductName ?? "Ürün Silinmiş",
                        ProductImageSnapshot = product?.ImageUrl ?? "/img/no-image.jpg",
                        PriceAtOrder = product?.Price ?? 0,
                        ProductCodeSnapshot = product?.ProductId.ToString() ?? "0",
                        Fiyat = (double)(product?.Price ?? 0)
                    });

                    // 3. Stok Hareketi Loglama (StockMovement)
                    // Önce ilgili ürünün stok kaydını bulup ID'sini alıyoruz
                    var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.ProductId == cartItem.UrunId);
                    
                    if (stock != null)
                    {
                        _context.StockMovements.Add(new StockMovement
                        {
                            StockId = stock.StockId,
                            MovementType = "Sipariş",
                            QuantityChange = -cartItem.Miktar, 
                            RelatedReferenceId = order.Id,
                            MovementDate = DateTime.Now,
                            PerformedBy = username 
                        });
                    }
                }
                await _context.SaveChangesAsync();
                await _cartService.ClearCart();
                await transaction.CommitAsync();

                return new CheckoutResult { IsSuccess = true, OrderId = order.Id };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
    catch (Exception ex)
    {
        return new CheckoutResult { IsSuccess = false, ErrorMessage = "İşlem sırasında hata oluştu: " + ex.Message };
    }
}

    private async Task<ProcessPaymentResult> ProcessPaymentInternalAsync(
        OrderCreateModel model,
        Cart cart,
        string finalAd,
        string finalSoyad,
        string finalTelefon,
        string finalSehir,
        string finalAdres,
        string finalZip,
        decimal finalKargoUcreti,
        string userIp,
        string username)
    {
        Iyzipay.Options options = new Iyzipay.Options
        {
            ApiKey = _configuration["PaymentAPI:APIKey"],
            SecretKey = _configuration["PaymentAPI:SecretKey"],
            BaseUrl = "https://sandbox-api.iyzipay.com"
        };

        double genelToplam = (double)cart.Toplam + (double)finalKargoUcreti;
        string totalFormatted = genelToplam.ToString("F2", CultureInfo.InvariantCulture);

        Iyzipay.Request.CreatePaymentRequest request = new Iyzipay.Request.CreatePaymentRequest
        {
            Locale = Iyzipay.Model.Locale.TR.ToString(),
            ConversationId = Guid.NewGuid().ToString(),
            Price = totalFormatted,
            PaidPrice = totalFormatted,
            Currency = Iyzipay.Model.Currency.TRY.ToString(),
            Installment = 1,
            BasketId = "B" + Guid.NewGuid().ToString().Substring(0, 5),
            PaymentChannel = Iyzipay.Model.PaymentChannel.WEB.ToString(),
            PaymentGroup = Iyzipay.Model.PaymentGroup.PRODUCT.ToString()
        };

        request.PaymentCard = new Iyzipay.Model.PaymentCard
        {
            CardHolderName = model.CartName,
            CardNumber = model.CartNumber,
            ExpireMonth = model.CartExpirationMonth,
            ExpireYear = model.CartExpirationYear,
            Cvc = model.CartCVV,
            RegisterCard = 0
        };

        request.Buyer = new Iyzipay.Model.Buyer
        {
            Id = "BY" + (username ?? "Guest"),
            Name = finalAd,
            Surname = finalSoyad,
            GsmNumber = finalTelefon,
            Email = model.Email ?? "test@test.com",
            IdentityNumber = "11111111111",
            RegistrationAddress = finalAdres,
            Ip = userIp,
            City = finalSehir,
            Country = "Turkey",
            ZipCode = finalZip
        };

        Iyzipay.Model.Address shippingAddress = new Iyzipay.Model.Address
        {
            ContactName = $"{finalAd} {finalSoyad}",
            City = finalSehir,
            Country = "Turkey",
            Description = finalAdres,
            ZipCode = finalZip
        };
        request.ShippingAddress = shippingAddress;
        request.BillingAddress = shippingAddress;

        List<Iyzipay.Model.BasketItem> basketItems = new List<Iyzipay.Model.BasketItem>();

        foreach (var item in cart.CartItems)
        {
            if (item.Urun != null && item.Urun.Price > 0)
            {
                // ✨ DÜZELTME: iyzico'ya satırın TOPLAM fiyatı bildirilir (Birim Fiyat x Adet)
                decimal satirToplamFiyati = (decimal)item.Urun.Price * item.Miktar;

                basketItems.Add(new Iyzipay.Model.BasketItem
                {
                    Id = item.UrunId.ToString(),
                    Name = item.Urun.ProductName,
                    Category1 = "Genel",
                    ItemType = Iyzipay.Model.BasketItemType.PHYSICAL.ToString(),
                    Price = satirToplamFiyati.ToString("F2", CultureInfo.InvariantCulture)
                });
            }
        }

        if (finalKargoUcreti > 0)
        {
            basketItems.Add(new Iyzipay.Model.BasketItem
            {
                Id = "SHIPPING",
                Name = "Kargo Ücreti",
                Category1 = "Lojistik",
                ItemType = Iyzipay.Model.BasketItemType.VIRTUAL.ToString(),
                Price = finalKargoUcreti.ToString("F2", CultureInfo.InvariantCulture)
            });
        }

        request.BasketItems = basketItems;

        Iyzipay.Model.Payment payment = await Task.Run(() => Iyzipay.Model.Payment.Create(request, options));

        return new ProcessPaymentResult
        {
            Status = payment.Status,
            ErrorMessage = payment.Status != "success" ? $"Hata: {payment.ErrorMessage} | Kod: {payment.ErrorCode}" : null,
            ErrorCode = payment.ErrorCode,
            PaymentId = payment.PaymentId,
            ConversationId = payment.ConversationId
        };
    }

    public Task<CheckoutResult> ProcessCheckoutAsync(OrderCreateModel model, string username, int userId, string userIp)
    {
        throw new NotImplementedException();
    }
}