using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class Cart
{
    public int CartId { get; set; }

    public string CustomerId { get; set; } = null!;

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();


    public void AddItem(Product product , int miktar)
    {
        var item = CartItems.Where(i => i.UrunId == product.ProductId).FirstOrDefault();

        if(item == null)
        {
            CartItems.Add(new CartItem {Urun = product , Miktar = miktar });
        }
        else
        {
            item.Miktar += miktar;
        }
    }


    public void DeleteItem(int urunId , int miktar)
    {
        var item = CartItems.Where(i => i.UrunId == urunId).FirstOrDefault();

        if(item != null)
        {
            item.Miktar -= miktar;

            if(item.Miktar == 0)
            {
                CartItems.Remove(item);
            }
        }
    }


    public double araToplam()
    {
       return (double)CartItems.Sum( i => i.Urun.Price * i.Miktar);
    }


    public double Toplam()
    {
        return (double)CartItems.Sum( i => i.Urun.Price * i.Miktar) * 1.2;
    }
}


