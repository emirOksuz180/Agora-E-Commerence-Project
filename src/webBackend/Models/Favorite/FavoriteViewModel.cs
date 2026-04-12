namespace webBackend.Models
{
    public class FavoriteViewModel
    {
        public int FavoriteId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public DateTime AddedDate { get; set; }

        public virtual Product Product { get; set; } = null!;


        
    }
}