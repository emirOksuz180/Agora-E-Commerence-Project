namespace webBackend.Models.StoredProcedures
{
    public class Sp_GetAvailableShippingOptions_Result
    {
        public int CarrierId { get; set; }
        public string CarrierName { get; set; }
        public decimal CalculatedShippingPrice { get; set; }
    }
}