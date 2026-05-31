using webBackend.Models; // Kendi namespace'ine göre ayarla

namespace webBackend.Services
{
    public interface ICheckoutService
    {
        Task<CheckoutResult> ProcessCheckoutAsync(OrderCreateModel model, Cart cart, int userId, string username, string userIp);
    }
}