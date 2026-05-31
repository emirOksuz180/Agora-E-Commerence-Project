public class CheckoutResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public int? OrderId { get; set; }
}