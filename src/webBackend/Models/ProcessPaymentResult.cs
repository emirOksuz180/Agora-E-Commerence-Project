public class ProcessPaymentResult
{
    public string Status { get; set; }
    public string ErrorMessage { get; set; }
    public string ErrorCode { get; set; }
    public string PaymentId { get; set; }
    public string ConversationId { get; set; }
}