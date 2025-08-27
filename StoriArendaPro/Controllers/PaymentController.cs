using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoriArendaPro.Models.Entities;
using StoriArendaPro.Services;
using StoriArendaPro.Services.Models;
using System.Security.Claims;

namespace StoriArendaPro.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly StoriArendaProContext _context;
        private readonly IYooKassaClient _yooKassaClient;

        public PaymentController(StoriArendaProContext context, IYooKassaClient yooKassaClient)
        {
            _context = context;
            _yooKassaClient = yooKassaClient;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment(int orderId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var order = await _context.RentalOrders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.RentalOrderId == orderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            // Интеграция с YooKassa
            var payment = await CreateYooKassaPayment(order);

            // Сохраняем информацию о платеже
            var dbPayment = new Payment
            {
                OrderId = orderId,
                OrderType = "rental",
                Amount = order.TotalAmount,
                Currency = "RUB",
                PaymentMethod = "yookassa",
                PaymentStatus = "ожидает",
                TransactionId = payment.Id, // Используем Id из YooKassa
                CreatedAt = DateTime.Now
            };

            _context.Payments.Add(dbPayment);
            await _context.SaveChangesAsync();

            return Json(new { confirmationUrl = payment.Confirmation.ConfirmationUrl });
        }

        private async Task<YooKassaPaymentResponse> CreateYooKassaPayment(RentalOrder order)
        {
            var returnUrl = Url.Action("PaymentSuccess", "Payment", new { orderId = order.RentalOrderId }, Request.Scheme);

            var metadata = new Dictionary<string, string>
            {
                { "orderId", order.RentalOrderId.ToString() },
                { "userId", order.UserId.ToString() }
            };

            return await _yooKassaClient.CreatePaymentAsync(
                order.TotalAmount,
                $"Оплата заказа аренды #{order.RentalOrderId}",
                returnUrl,
                metadata
            );
        }
    }
}