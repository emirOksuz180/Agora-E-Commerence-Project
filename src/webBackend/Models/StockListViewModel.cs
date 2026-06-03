namespace webBackend.Models;
public class StockListViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string LocationBarcode { get; set; }
    public int AvailableQuantity { get; set; }
}