using System.ComponentModel.DataAnnotations.Schema;

namespace webBackend.Models;

public partial class Order
{
    [NotMapped]
    public double AraToplam => OrderItems != null ? OrderItems.Sum(x => x.Miktar * x.Fiyat) : 0;

    [NotMapped]
    public double Toplam => AraToplam; // Veya kargo dahil hesaplaman varsa buraya ekle
}