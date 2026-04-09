using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace webBackend.Models;

public partial class Cart
{
    // 1. Toplam ve araToplam Hesaplamaları (View Hatalarını Çözer)
    [NotMapped]
    public double Toplam 
    {
        get 
        {
            // CartItems null ise veya boşsa 0 döndür, aksi halde hesapla
            return CartItems != null 
                ? (double)CartItems.Sum(x => x.Miktar * (x.Urun?.Price ?? 0m)) 
                : 0;
        }
    }

    [NotMapped]
    public double araToplam => Toplam;

    // 2. CartService içindeki hatayı çözen metot (AddItem)
    public void AddItem(Product urun, int miktar)
    {
        // Null kontrolü: CartItems koleksiyonu initialize edilmemişse oluştur
        if (CartItems == null) CartItems = new List<CartItem>();

        var item = CartItems.FirstOrDefault(i => i.UrunId == urun.ProductId);

        if (item == null)
        {
            CartItems.Add(new CartItem
            {
                UrunId = urun.ProductId,
                Miktar = miktar,
                
            });
        }
        else
        {
            item.Miktar += miktar;
        }
    }

    // 3. CartService içindeki hatayı çözen metot (DeleteItem)
    public void DeleteItem(int urunId, int miktar)
    {
        if (CartItems == null) return;

        var item = CartItems.FirstOrDefault(i => i.UrunId == urunId);
        if (item != null)
        {
            if (item.Miktar > miktar)
            {
                item.Miktar -= miktar;
            }
            else
            {
                CartItems.Remove(item);
            }
        }
    }
}