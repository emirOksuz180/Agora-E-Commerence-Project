public class PaymentResultDto
{
    public bool IsSuccess { get; set; }
    public string? PaymentId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}