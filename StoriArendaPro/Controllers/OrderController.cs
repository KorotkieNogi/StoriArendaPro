// Controllers/OrderController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoriArendaPro.Models.Entities;
using StoriArendaPro.Models.ViewModels;
using System.Security.Claims;

namespace StoriArendaPro.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly StoriArendaProContext _context;

        public OrderController(StoriArendaProContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Checkout()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var cartItems = await _context.ShoppingCarts
                .Include(c => c.RentalPrice)
                .ThenInclude(rp => rp.Product)
                .ThenInclude(p => p.Inventories)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Profile");
            }

            // Проверяем доступность товаров
            foreach (var item in cartItems)
            {
                var available = item.RentalPrice.Product.Inventories.FirstOrDefault()?.QuantityForRent ?? 0;
                if (available < item.Quantity)
                {
                    ModelState.AddModelError("", $"Недостаточно товара '{item.RentalPrice.Product.Name}' в наличии");
                    return RedirectToAction("Index", "Profile");
                }
            }

            var model = new PaymentViewModel
            {
                OrderType = "rental",
                Amount = cartItems.Sum(c => c.Subtotal),
                Currency = "RUB"
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Проверяем верификацию паспорта
            var verification = await _context.PassportVerifications
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Status == "approved");

            if (verification == null)
            {
                return Json(new { success = false, message = "Требуется верификация паспортных данных" });
            }

            // Создаем заказ
            var order = new RentalOrder
            {
                UserId = userId,
                TotalAmount = model.Amount,
                PaymentStatus = "ожидает",
                Status = "оформлен",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // Добавляем товары из корзины
            var cartItems = await _context.ShoppingCarts
                .Include(c => c.RentalPrice)
                .ThenInclude(rp => rp.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            foreach (var cartItem in cartItems)
            {
                order.RentalOrderItems.Add(new RentalOrderItem
                {
                    RentalPriceId = cartItem.RentalPriceId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice,
                    Subtotal = cartItem.Subtotal,
                    RentalType = cartItem.RentalType
                });

                // Резервируем товар
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == cartItem.RentalPrice.ProductId);

                if (inventory != null)
                {
                    inventory.ReservedForRent = (inventory.ReservedForRent ?? 0) + cartItem.Quantity;
                    inventory.UpdatedAt = DateTime.Now;
                }
            }

            _context.RentalOrders.Add(order);
            _context.ShoppingCarts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            // Перенаправляем на страницу оплаты
            return RedirectToAction("CreatePayment", "Payment", new { orderId = order.RentalOrderId });
        }

        public IActionResult PaymentSuccess(int orderId)
        {
            return View(new PaymentResultViewModel
            {
                Success = true,
                Message = "Заказ успешно оплачен",
                OrderId = orderId,
                Amount = _context.RentalOrders.Find(orderId)?.TotalAmount ?? 0
            });
        }


        //[HttpPost]
        //public async Task<IActionResult> CreatePayment(PaymentViewModel model)
        //{
        //    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        //    // Создаем платеж в YooKassa
        //    var client = new YooKassaClient(shopId, secretKey);

        //    var payment = await client.CreatePayment(
        //        new CreatePaymentRequest
        //        {
        //            Amount = new Amount { Value = model.Amount, Currency = "RUB" },
        //            Confirmation = new Confirmation { Type = "redirect" },
        //            Description = $"Оплата заказа #{model.OrderId}",
        //            Metadata = new Dictionary<string, string>
        //            {
        //        { "orderId", model.OrderId.ToString() },
        //        { "userId", userId.ToString() }
        //            }
        //        });

        //    // Сохраняем информацию о платеже в БД
        //    var dbPayment = new Payment
        //    {
        //        OrderId = model.OrderId,
        //        OrderType = "rental",
        //        Amount = model.Amount,
        //        Currency = "RUB",
        //        PaymentMethod = "yookassa",
        //        PaymentStatus = "ожидает",
        //        TransactionId = payment.Id,
        //        CreatedAt = DateTime.Now
        //    };

        //    _context.Payments.Add(dbPayment);
        //    await _context.SaveChangesAsync();

        //    return Json(new { confirmationUrl = payment.Confirmation.ConfirmationUrl });
        //}



    }
}