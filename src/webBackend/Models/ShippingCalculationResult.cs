namespace webBackend.Models 
{
    public class ShippingCalculationResult
    {
        public decimal FinalFiyat { get; set; }
        public bool UcretsizKargo { get; set; }
        public bool IsSuccess {get; set;}
        public decimal CartTotal { get; set; }
    }
}