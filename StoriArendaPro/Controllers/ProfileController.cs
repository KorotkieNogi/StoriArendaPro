// Controllers/ProfileController.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoriArendaPro.Models.Entities;
using StoriArendaPro.Models.ViewModels;
using StoriArendaPro.Services;
using System.Security.Claims;

namespace StoriArendaPro.Controllers
{
    [Authorize] // Для всех аутентифицированных пользователей
    public class ProfileController : Controller
    {
        private readonly StoriArendaProContext _context;
        private readonly IEmailService _emailService;

        public ProfileController(StoriArendaProContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users
                .Include(u => u.PassportVerifications)
                .Include(u => u.RentalOrders)
                .Include(u => u.ShoppingCart)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login", "Auth");
            }

            // Получаем чаты поддержки
            var chats = await _context.SupportChats
                .Include(c => c.User)
                .Include(c => c.Admin)
                .Include(c => c.ChatMessages)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            // Получаем корзину
            var cartItems = await _context.ShoppingCarts
                .Include(c => c.RentalPrice)
                .ThenInclude(rp => rp.Product)
                .ThenInclude(p => p.ProductImages)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            // Получаем заказы
            var rentalOrders = await _context.RentalOrders
                .Include(ro => ro.RentalOrderItems)
                    .ThenInclude(roi => roi.RentalPrice)
                        .ThenInclude(rp => rp.Product)
                            .ThenInclude(p => p.ProductImages)
                .Where(ro => ro.UserId == userId)
                .OrderByDescending(ro => ro.CreatedAt)
                .ToListAsync();

            var model = new UserProfileViewModel
            {
                User = user,
                RentalOrders = rentalOrders,
                SupportChats = chats,
                CartItems = cartItems,
                PassportVerification = user.PassportVerifications.FirstOrDefault(),
                CartTotal = cartItems.Sum(c => c.Subtotal),
                CartItemsCount = cartItems.Count,
                ActiveRentals = rentalOrders.Count(o => o.Status == "active"),
                CompletedRentals = rentalOrders.Count(o => o.Status == "completed"),
                TotalSpent = rentalOrders.Where(o => o.PaymentStatus == "оплачено").Sum(o => o.TotalAmount)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePassportData(PassportDataViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Пожалуйста, заполните все обязательные поля правильно." });
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Проверяем, была ли недавно подана заявка
            var recentVerification = await _context.PassportVerifications
                .Where(p => p.UserId == userId && p.CreatedAt > DateTime.Now.AddHours(-24))
                .FirstOrDefaultAsync();

            if (recentVerification != null)
            {
                return Json(new
                {
                    success = false,
                    message = "Вы уже подавали заявку на проверку. Следующая заявка возможна через 24 часа."
                });
            }

            try
            {
                // Сначала обновляем данные в таблице Users
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.PassportSeria = model.PassportSeria;
                    user.PassportNumber = model.PassportNumber;
                    user.Propiska = model.Propiska;
                    user.PlaceLive = model.PlaceLive;
                    user.UpdatedAt = DateTime.Now;
                }

                // Проверяем, есть ли уже верификация
                var verification = await _context.PassportVerifications
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (verification == null)
                {
                    verification = new PassportVerification
                    {
                        UserId = userId,
                        Status = "ожидает рассмотрения",
                        CreatedAt = DateTime.Now
                    };
                    _context.PassportVerifications.Add(verification);
                }

                // Обновляем данные в таблице верификации
                verification.PassportSeria = model.PassportSeria;
                verification.PassportNumber = model.PassportNumber;
                verification.IssuedBy = model.IssuedBy;
                verification.IssueDate = model.IssueDate;
                verification.Propiska = model.Propiska;
                verification.PlaceLive = model.PlaceLive;
                verification.UpdatedAt = DateTime.Now;

                // Обработка загрузки фотографий
                if (model.PassportPhotoFront != null)
                {
                    var frontPath = await SavePassportPhoto(model.PassportPhotoFront, userId, "front");
                    verification.PassportPhotoFront = frontPath;
                }

                if (model.PassportPhotoBack != null)
                {
                    var backPath = await SavePassportPhoto(model.PassportPhotoBack, userId, "back");
                    verification.PassportPhotoBack = backPath;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Паспортные данные успешно отправлены на проверку." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка при сохранении данных: {ex.Message}" });
            }
        }


        
        private async Task<string> SavePassportPhoto(IFormFile file, int userId, string side)
        {
            try
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "passports");

                // Создаем директорию если не существует
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var fileName = $"passport_{side}_{userId}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"/uploads/passports/{fileName}";
            }
            catch (Exception ex)
            {
                // Логируем ошибку
                Console.WriteLine($"Ошибка сохранения файла: {ex.Message}");
                throw;
            }
        }



        [HttpGet]
        public async Task<IActionResult> CheckPassportVerification()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Сначала проверяем, заполнены ли базовые паспортные данные в таблице Users
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return Json(new
                {
                    hasPassportData = false,
                    isVerified = false,
                    isPending = false,
                    status = "none",
                    requiresVerification = true,
                    message = "Требуется верификация паспортных данных"
                });
            }

            // Проверяем, заполнены ли основные паспортные данные в таблице Users
            var hasBasicPassportData = !string.IsNullOrEmpty(user.PassportSeria) &&
                                      !string.IsNullOrEmpty(user.PassportNumber) &&
                                      !string.IsNullOrEmpty(user.Propiska) &&
                                      !string.IsNullOrEmpty(user.PlaceLive);

            if (!hasBasicPassportData)
            {
                return Json(new
                {
                    hasPassportData = false,
                    isVerified = false,
                    isPending = false,
                    status = "none",
                    requiresVerification = true,
                    message = "Требуется заполнить паспортные данные в профиле"
                });
            }

            // Теперь проверяем таблицу верификации
            var verification = await _context.PassportVerifications
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (verification == null)
            {
                return Json(new
                {
                    hasPassportData = true, // данные есть в Users, но нет верификации
                    isVerified = false,
                    isPending = false,
                    status = "none",
                    requiresVerification = true,
                    message = "Требуется верификация паспортных данных"
                });
            }

            return Json(new
            {
                hasPassportData = true,
                isVerified = verification.Status == "одобрено",
                isPending = verification.Status == "ожидает рассмотрения",
                status = verification.Status,
                requiresVerification = verification.Status != "одобрено",
                message = verification.Status == "ожидает рассмотрения"
                    ? "Ваши паспортные данные находятся на проверке"
                    : verification.Status == "одобрено"
                        ? "Паспорт верифицирован"
                        : "Паспортные данные отклонены"
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] CartItemViewModel model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Проверяем, заполнены ли основные паспортные данные в таблице Users
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || string.IsNullOrEmpty(user.PassportSeria) ||
                string.IsNullOrEmpty(user.PassportNumber) ||
                string.IsNullOrEmpty(user.Propiska) ||
                string.IsNullOrEmpty(user.PlaceLive))
            {
                return Json(new
                {
                    success = false,
                    requiresVerification = true,
                    message = "Требуется заполнить паспортные данные в профиле"
                });
            }

            // Теперь проверяем верификацию
            var verification = await _context.PassportVerifications
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (verification == null || verification.Status != "одобрено")
            {
                return Json(new
                {
                    success = false,
                    requiresVerification = true,
                    message = verification == null
                        ? "Требуется верификация паспортных данных"
                        : "Паспортные данные находятся на проверке"
                });
            }

            // Проверяем доступность товара
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == model.ProductId);

            if (inventory == null || inventory.QuantityForRent < model.Quantity)
            {
                return Json(new
                {
                    success = false,
                    message = "Недостаточно товара в наличии"
                });
            }

            // Проверяем, не добавлен ли уже этот товар в корзину
            var existingCartItem = await _context.ShoppingCarts
                .FirstOrDefaultAsync(c => c.UserId == userId &&
                                         c.RentalPriceId == model.RentalPriceId &&
                                         c.StartDate == model.StartDate &&
                                         c.EndDate == model.EndDate);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += model.Quantity;
                existingCartItem.Subtotal = existingCartItem.Quantity * existingCartItem.UnitPrice;
                existingCartItem.UpdatedAt = DateTime.Now;
            }
            else
            {
                var cartItem = new ShoppingCart
                {
                    UserId = userId,
                    RentalPriceId = model.RentalPriceId,
                    Quantity = model.Quantity,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    RentalType = model.RentalType,
                    UnitPrice = model.UnitPrice,
                    Subtotal = model.Subtotal,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.ShoppingCarts.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Товар успешно добавлен в корзину",
                cartCount = await _context.ShoppingCarts.CountAsync(c => c.UserId == userId)
            });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cartItem = await _context.ShoppingCarts
                .FirstOrDefaultAsync(c => c.CartId == cartId && c.UserId == userId);

            if (cartItem != null)
            {
                _context.ShoppingCarts.Remove(cartItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCartQuantity(int cartId, int quantity)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cartItem = await _context.ShoppingCarts
                .Include(c => c.RentalPrice)
                .ThenInclude(rp => rp.Product)
                .ThenInclude(p => p.Inventories)
                .FirstOrDefaultAsync(c => c.CartId == cartId && c.UserId == userId);

            if (cartItem != null)
            {
                // Проверяем доступность
                var inventory = cartItem.RentalPrice.Product.Inventories.FirstOrDefault();
                if (inventory != null && inventory.QuantityForRent >= quantity)
                {
                    cartItem.Quantity = quantity;
                    cartItem.Subtotal = cartItem.Quantity * cartItem.UnitPrice;
                    cartItem.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { count = 0 });
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var count = await _context.ShoppingCarts
                .Where(c => c.UserId == userId)
                .CountAsync();

            return Json(new { count = count });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCartItem(int cartId, [FromBody] CartUpdateViewModel model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cartItem = await _context.ShoppingCarts
                .Include(c => c.RentalPrice)
                .ThenInclude(rp => rp.Product)
                .ThenInclude(p => p.Inventories)
                .FirstOrDefaultAsync(c => c.CartId == cartId && c.UserId == userId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Товар не найден в корзине" });
            }

            // Проверяем доступность
            var inventory = cartItem.RentalPrice.Product.Inventories.FirstOrDefault();
            if (inventory != null && inventory.QuantityForRent >= model.Quantity)
            {
                cartItem.Quantity = model.Quantity;
                cartItem.StartDate = model.StartDate;
                cartItem.EndDate = model.EndDate;
                cartItem.Subtotal = cartItem.Quantity * cartItem.UnitPrice;
                cartItem.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Корзина обновлена" });
            }

            return Json(new { success = false, message = "Недостаточно товара в наличии" });
        }

        [HttpGet]
        public async Task<IActionResult> GetCartItemDetails(int cartId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cartItem = await _context.ShoppingCarts
                .Include(c => c.RentalPrice)
                .ThenInclude(rp => rp.Product)
                .ThenInclude(p => p.Inventories)
                .FirstOrDefaultAsync(c => c.CartId == cartId && c.UserId == userId);

            if (cartItem == null)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                productId = cartItem.RentalPrice.ProductId,
                rentalPriceId = cartItem.RentalPriceId,
                startDate = cartItem.StartDate?.ToString("yyyy-MM-dd"),
                endDate = cartItem.EndDate?.ToString("yyyy-MM-dd"),
                quantity = cartItem.Quantity,
                maxQuantity = cartItem.RentalPrice.Product.Inventories.FirstOrDefault()?.QuantityForRent ?? 0,
                unitPrice = cartItem.UnitPrice,
                subtotal = cartItem.Subtotal
            });
        }

        // Controllers/ProfileController.cs
        public async Task<IActionResult> Cart()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var cartItems = await _context.ShoppingCarts
                .Include(c => c.RentalPrice)
                .ThenInclude(rp => rp.Product)
                .ThenInclude(p => p.ProductImages)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(cartItems);
        }
    }



}