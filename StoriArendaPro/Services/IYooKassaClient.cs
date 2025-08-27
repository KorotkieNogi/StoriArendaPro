using StoriArendaPro.Services.Models;

namespace StoriArendaPro.Services
{
    public interface IYooKassaClient
    {
        Task<YooKassaPaymentResponse> CreatePaymentAsync(decimal amount, string description, string returnUrl, Dictionary<string, string> metadata = null);
        Task<bool> CheckPaymentStatusAsync(string paymentId);
    }
}
