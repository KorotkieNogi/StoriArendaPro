// Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using StoriArendaPro.Models.Entities;
using StoriArendaPro.Models.ViewModels;
using StoriArendaPro.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace StoriArendaPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly StoriArendaProContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AdminController> _logger;

        public AdminController(StoriArendaProContext context, UserManager<User> userManager, IEmailService emailService, IWebHostEnvironment environment, ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _environment = environment;
            _logger = logger;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!User.IsInRole("Admin"))
            {
                context.Result = RedirectToAction("AccessDenied", "Home", new { area = "" });
                return;
            }
            base.OnActionExecuting(context);
        }

        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive == true),
                PendingVerifications = await _context.PassportVerifications
                    .CountAsync(p => p.Status == "ожидает рассмотрения"),
                PendingRequests = await _context.SupportChats
                    .CountAsync(c => c.Status == "открыто"),
                ActiveRentals = await _context.RentalOrders
                    .CountAsync(r => r.Status == "активен"),
                MonthlyRevenue = await _context.RentalOrders
                    .Where(r => r.CreatedAt >= DateTime.Now.AddMonths(-1) && r.PaymentStatus == "оплачено")
                    .SumAsync(r => r.TotalAmount),
                Verifications = await _context.PassportVerifications
                    .Include(p => p.User)
                    .Where(p => p.Status == "ожидает рассмотрения")
                    .OrderBy(p => p.CreatedAt)
                    .Take(10)
                    .ToListAsync(),
                RecentOrders = await _context.RentalOrders
                    .Include(r => r.User)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .ToListAsync(),
                ActiveChats = await _context.SupportChats
                    .Include(c => c.User)
                    .Where(c => c.Status == "открыто")
                    .OrderByDescending(c => c.UpdatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View("~/Areas/Admin/Views/Admin/Index.cshtml", model);
        }

        public async Task<IActionResult> PassportVerifications(string status = "ожидает рассмотрения")
        {
            var verifications = await _context.PassportVerifications
                .Include(p => p.User)
                .Include(p => p.Verifier)
                .Where(p => string.IsNullOrEmpty(status) || p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            ViewBag.StatusOptions = new[] { "все", "ожидает рассмотрения", "approved", "rejected" };

            return View(verifications);
        }

        public async Task<IActionResult> PassportVerificationDetail(int id)
        {
            var verification = await _context.PassportVerifications
                .Include(p => p.User)
                .Include(p => p.Verifier)
                .FirstOrDefaultAsync(p => p.VerificationId == id);

            if (verification == null)
            {
                return NotFound();
            }

            var model = new PassportVerificationReviewViewModel
            {
                VerificationId = verification.VerificationId,
                UserId = verification.UserId,
                UserName = verification.User.FullName,
                UserEmail = verification.User.Email,
                UserPhone = verification.User.PhoneNumber,
                PassportSeria = verification.PassportSeria,
                PassportNumber = verification.PassportNumber,
                IssuedBy = verification.IssuedBy,
                Propiska = verification.Propiska,
                PlaceLive = verification.PlaceLive,
                PassportPhotoFront = verification.PassportPhotoFront,
                PassportPhotoBack = verification.PassportPhotoBack,
                Status = verification.Status,
                AdminNotes = verification.AdminNotes
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyPassport(int verificationId, string status, string adminNotes)
        {
            var verification = await _context.PassportVerifications
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.VerificationId == verificationId);

            if (verification == null)
            {
                return NotFound();
            }

            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            // Заменяем статусы на русские
            if (status == "approved") status = "одобрено";
            else if (status == "rejected") status = "отклонено";

            verification.Status = status;
            verification.AdminNotes = adminNotes;
            verification.VerifiedBy = adminId;
            verification.VerifiedAt = DateTime.Now;
            verification.UpdatedAt = DateTime.Now;

            if (status == "одобрено")
            {
                var user = verification.User;
                user.PassportSeria = verification.PassportSeria;
                user.PassportNumber = verification.PassportNumber;
                user.Propiska = verification.Propiska;
                user.PlaceLive = verification.PlaceLive;
                user.UpdatedAt = DateTime.Now;

                // Отправляем email уведомление об одобрении
                await _emailService.SendEmailAsync(user.Email, "Верификация паспорта одобрена",
                    "Ваши паспортные данные успешно подтверждены. Теперь вы можете арендовать оборудование.");
            }
            else if (status == "отклонено")
            {
                var user = verification.User;
                // Отправляем email уведомление об отклонении с причиной
                await _emailService.SendEmailAsync(user.Email, "Верификация паспорта отклонена",
                    $"Ваши паспортные данные отклонены. Причина: {adminNotes}");
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(PassportVerifications));
        }

        [HttpPost]
        public async Task<IActionResult> BulkVerifyPassports(int[] verificationIds, string status, string adminNotes)
        {
            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            // Заменяем статусы на русские
            if (status == "approved") status = "одобрено";
            else if (status == "rejected") status = "отклонено";

            foreach (var id in verificationIds)
            {
                var verification = await _context.PassportVerifications
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.VerificationId == id);

                if (verification != null)
                {
                    verification.Status = status;
                    verification.AdminNotes = adminNotes;
                    verification.VerifiedBy = adminId;
                    verification.VerifiedAt = DateTime.Now;
                    verification.UpdatedAt = DateTime.Now;

                    if (status == "одобрено")
                    {
                        var user = verification.User;
                        user.PassportSeria = verification.PassportSeria;
                        user.PassportNumber = verification.PassportNumber;
                        user.Propiska = verification.Propiska;
                        user.PlaceLive = verification.PlaceLive;
                        user.UpdatedAt = DateTime.Now;

                        await _emailService.SendEmailAsync(user.Email, "Верификация паспорта одобрена",
                            "Ваши паспортные данные успешно подтверждены. Теперь вы можете арендовать оборудование.");
                    }
                    else if (status == "отклонено")
                    {
                        var user = verification.User;
                        await _emailService.SendEmailAsync(user.Email, "Верификация паспорта отклонена",
                            $"Ваши паспортные данные отклонены. Причина: {adminNotes}");
                    }
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(PassportVerifications));
        }

        [HttpPost]
        [AllowAnonymous] // или [Authorize] в зависимости от ваших требований
        public async Task<IActionResult> CreatePassportVerification(PassportVerification model)
        {
            // Валидация и обработка модели
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound("Пользователь не найден");
            }

            // Создание новой заявки
            var verification = new PassportVerification
            {
                UserId = userId,
                PassportSeria = model.PassportSeria,
                PassportNumber = model.PassportNumber,
                IssuedBy = model.IssuedBy,
                Propiska = model.Propiska,
                PlaceLive = model.PlaceLive,
                PassportPhotoFront = model.PassportPhotoFront,
                PassportPhotoBack = model.PassportPhotoBack,
                Status = "ожидает рассмотрения",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.PassportVerifications.Add(verification);
            await _context.SaveChangesAsync();

            // КОД ДЛЯ ОТПРАВКИ УВЕДОМЛЕНИЯ АДМИНИСТРАТОРАМ
            var adminEmails = await _context.Users
                .Where(u => u.IsAdmin == true)
                .Select(u => u.Email)
                .ToListAsync();

            foreach (var email in adminEmails)
            {
                await _emailService.SendEmailAsync(email, "Новая заявка на проверку паспорта",
                    $"Пользователь {user.FullName} подал заявку на проверку паспортных данных. " +
                    $"Дата подачи: {DateTime.Now:dd.MM.yyyy HH:mm}");
            }

            return Json(new { success = true, message = "Паспортные данные успешно отправлены на проверку." });
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users
                .Include(u => u.PassportVerifications)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            // Статистика администрирования
            ViewBag.VerifiedPassports = await _context.PassportVerifications
                .CountAsync(p => p.VerifiedBy == userId && p.Status == "approved");

            ViewBag.ProcessedOrders = await _context.RentalOrders
                .CountAsync(o => o.UserId == userId);

            ViewBag.SolvedTickets = await _context.SupportChats
                .CountAsync(c => c.AdminId == userId && c.Status == "решено");

            // Получаем роли пользователя
            var userRoles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRoles = userRoles;

            return View(user);
        }


        public class AdminProfileUpdateModel
        {
            [Required(ErrorMessage = "Обязательное поле")]
            public string FullName { get; set; }

            [Required(ErrorMessage = "Обязательное поле")]
            [EmailAddress(ErrorMessage = "Некорректный email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Обязательное поле")]
            [Phone(ErrorMessage = "Некорректный номер телефона")]
            public string Phone { get; set; }

            public string CurrentPassword { get; set; }

            [StringLength(100, ErrorMessage = "Пароль должен содержать минимум {2} символов", MinimumLength = 6)]
            public string NewPassword { get; set; }

            [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
            public string ConfirmPassword { get; set; }

            // Дополнительные поля для администратора
            public string PassportSeria { get; set; }
            public string PassportNumber { get; set; }
            public string Propiska { get; set; }
            public string PlaceLive { get; set; }
        }

        // Добавим в AdminController.cs новые методы

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Users(string search = "", string role = "", string status = "")
        {
            // Базовый запрос
            var query = _context.Users.AsQueryable();

            // Поиск
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search) ||
                    u.PhoneNumber.Contains(search));
            }

            // Фильтр по статусу
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active")
                    query = query.Where(u => u.IsActive == true);
                else if (status == "inactive")
                    query = query.Where(u => u.IsActive == false);
            }

            // Фильтрация по ролям через join
            if (!string.IsNullOrEmpty(role))
            {
                query = query
                    .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                    .Join(_context.Roles, x => x.ur.RoleId, r => r.Id, (x, r) => new { x.u, r })
                    .Where(x => x.r.Name == role)
                    .Select(x => x.u)
                    .Distinct();
            }

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            ViewBag.SearchTerm = search;
            ViewBag.RoleFilter = role;
            ViewBag.StatusFilter = status;
            ViewBag.RoleOptions = await _context.Roles.Select(r => r.Name).ToListAsync();

            return View(users);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UserDetail(int id)
        {
            var user = await _context.Users
                .Include(u => u.PassportVerifications)
                .Include(u => u.RentalOrders)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Получаем роли пользователя через UserManager
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new UserDetailViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive ?? true,
                IsAdmin = user.IsAdmin ?? false,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                PassportSeria = user.PassportSeria,
                PassportNumber = user.PassportNumber,
                Propiska = user.Propiska,
                PlaceLive = user.PlaceLive,
                PassportStatus = user.PassportVerifications
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault()?.Status ?? "не проверен",
                TotalOrders = user.RentalOrders.Count,
                ActiveOrders = user.RentalOrders.Count(o => o.Status == "активен"),
                Roles = userRoles
            };

            // ИЗМЕНЕНИЕ: Получаем только имена ролей
            ViewBag.AllRoles = await _context.Roles.Select(r => r.Name).ToListAsync();

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(UserUpdateViewModel model)
        {
            // Очистить ошибки для необязательных полей
            ModelState.Remove("PassportSeria");
            ModelState.Remove("PassportNumber");
            ModelState.Remove("Propiska");
            ModelState.Remove("PlaceLive");
            ModelState.Remove("SelectedRoles");

            if (!ModelState.IsValid)
            {
                // Если модель невалидна, нужно вернуть UserDetailViewModel
                var user = await _context.Users
                    .Include(u => u.PassportVerifications)
                    .Include(u => u.RentalOrders)
                    .FirstOrDefaultAsync(u => u.Id == model.UserId);

                if (user == null)
                {
                    return NotFound();
                }

                var userRoles = await _userManager.GetRolesAsync(user);

                var detailModel = new UserDetailViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive ?? true,
                    IsAdmin = user.IsAdmin ?? false,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    PassportSeria = user.PassportSeria,
                    PassportNumber = user.PassportNumber,
                    Propiska = user.Propiska,
                    PlaceLive = user.PlaceLive,
                    PassportStatus = user.PassportVerifications
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefault()?.Status ?? "не проверен",
                    TotalOrders = user.RentalOrders.Count,
                    ActiveOrders = user.RentalOrders.Count(o => o.Status == "активен"),
                    Roles = userRoles
                };

                ViewBag.AllRoles = await _context.Roles.Select(r => r.Name).ToListAsync();
                return View("UserUpdate", model); // Используем отдельное представление для ошибок
            }

            var userToUpdate = await _context.Users.FindAsync(model.UserId);
            if (userToUpdate == null)
            {
                return NotFound();
            }

            // Обновляем основные данные
            userToUpdate.FullName = model.FullName;
            userToUpdate.Email = model.Email;
            userToUpdate.PhoneNumber = model.PhoneNumber;
            userToUpdate.IsActive = model.IsActive;
            userToUpdate.UpdatedAt = DateTime.Now;

            // Обновляем паспортные данные (они могут быть null)
            userToUpdate.PassportSeria = model.PassportSeria;
            userToUpdate.PassportNumber = model.PassportNumber;
            userToUpdate.Propiska = model.Propiska;
            userToUpdate.PlaceLive = model.PlaceLive;

            // Обновляем роли (они могут быть null)
            var currentRoles = await _userManager.GetRolesAsync(userToUpdate);
            await _userManager.RemoveFromRolesAsync(userToUpdate, currentRoles);

            if (model.SelectedRoles != null && model.SelectedRoles.Any())
            {
                await _userManager.AddToRolesAsync(userToUpdate, model.SelectedRoles);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Данные пользователя успешно обновлены";
            return RedirectToAction("UserDetail", new { id = model.UserId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Нельзя удалить самого себя
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "Нельзя удалить свой собственный аккаунт";
                return RedirectToAction("Users");
            }

            // Мягкое удаление (деактивация)
            user.IsActive = false;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Пользователь успешно деактивирован";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Пользователь успешно активирован";
            return RedirectToAction("Users");
        }






        // GET: Admin/Products
        public async Task<IActionResult> Products(string search = "", int? categoryId = null, int? typeId = null)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.TypeProduct)
                .Include(p => p.RentalPrices)
                .Include(p => p.Inventories)
                .Include(p => p.ProductImages)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            if (typeId.HasValue)
            {
                query = query.Where(p => p.TypeProductId == typeId);
            }

            var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

            ViewBag.SearchTerm = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.TypeId = typeId;
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.TypeProducts = await _context.TypeProducts.ToListAsync();

            return View(products);
        }

        // GET: Admin/Product/Create
        public async Task<IActionResult> CreateProduct()
        {
            var model = new ProductViewModel
            {
                Categories = await _context.Categories
                    .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                    .ToListAsync(),
                TypeProducts = await _context.TypeProducts
                    .Select(t => new SelectListItem { Value = t.TypeProductId.ToString(), Text = t.Name })
                    .ToListAsync()
            };

            return View(model);
        }

        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100 * 1024 * 1024)]
        public async Task<IActionResult> CreateProduct(ProductViewModel model)
        {
            ModelState.Remove("Images");

            Console.WriteLine($"Начало обработки CreateProduct");
            Console.WriteLine($"Images count: {model.Images?.Count}");

            if (model.Images != null)
            {
                foreach (var img in model.Images)
                {
                    Console.WriteLine($"Image: {img?.FileName}, Size: {img?.Length}, Type: {img?.ContentType}");
                }
            }

            if (!ModelState.IsValid)
            {
                model.Categories = await _context.Categories
                    .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                    .ToListAsync();
                model.TypeProducts = await _context.TypeProducts
                    .Select(t => new SelectListItem { Value = t.TypeProductId.ToString(), Text = t.Name })
                    .ToListAsync();
                return View(model);
            }

            var executionStrategy = _context.Database.CreateExecutionStrategy();
            IActionResult result = View(model); // По умолчанию возвращаем view с ошибкой

            await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                _context.Database.SetCommandTimeout(120);

                try
                {
                    // Генерируем slug
                    string slug = GenerateSlug(model.Name);
                    string uniqueSlug = await GetUniqueSlug(slug);

                    // 1. Создаем продукт
                    var product = new Product
                    {
                        Name = model.Name,
                        Sku = model.Sku,
                        Description = model.Description,
                        ShortDescription = model.ShortDescription,
                        CategoryId = model.CategoryId,
                        TypeProductId = model.TypeProductId,
                        IsActive = model.IsActive,
                        Slug = uniqueSlug,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    // 2. Создаем цены аренды
                    var rentalPrice = new RentalPrice
                    {
                        ProductId = product.ProductId,
                        PricePerDay = model.PricePerDay,
                        Deposit = model.Deposit,
                        MinRentalDays = model.MinRentalDays,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.RentalPrices.Add(rentalPrice);

                    // 3. Создаем инвентарь
                    var inventory = new Inventory
                    {
                        ProductId = product.ProductId,
                        QuantityForRent = model.QuantityForRent,
                        QuantityForSale = model.QuantityForSale,
                        LowStockThreshold = model.LowStockThreshold,
                        UpdatedAt = DateTime.Now
                    };

                    _context.Inventories.Add(inventory);

                    // 4. Обрабатываем изображения
                    if (model.Images != null && model.Images.Any(img => img != null && img.Length > 0))
                    {
                        var uploadsPath = Path.Combine(_environment.WebRootPath, "photoEquipment");
                        if (!Directory.Exists(uploadsPath))
                        {
                            Directory.CreateDirectory(uploadsPath);
                        }

                        int order = 0;
                        foreach (var imageFile in model.Images.Where(img => img != null && img.Length > 0))
                        {
                            try
                            {
                                // Проверка типа файла
                                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                                var extension = Path.GetExtension(imageFile.FileName)?.ToLower();

                                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                                {
                                    continue; // Пропускаем недопустимые файлы
                                }

                                // Генерируем уникальное имя файла
                                var fileName = $"{Guid.NewGuid()}{extension}";
                                var filePath = Path.Combine(uploadsPath, fileName);

                                // Сохраняем файл
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await imageFile.CopyToAsync(stream);
                                }

                                // Создаем запись в БД
                                var productImage = new ProductImage
                                {
                                    ProductId = product.ProductId,
                                    Path = fileName,
                                    Order = order,
                                    IsMain = (order == 0) // Первое изображение - главное
                                };

                                _context.ProductImages.Add(productImage);
                                order++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Ошибка при обработке изображения {FileName}", imageFile?.FileName);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Нет изображений для обработки или они пустые");
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Устанавливаем результат для возврата
                    TempData["SuccessMessage"] = "Товар успешно добавлен";
                    result = RedirectToAction(nameof(Products));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Ошибка при создании товара");

                    
                    var errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage += $" | Inner: {ex.InnerException.Message}";
                        if (ex.InnerException is PostgresException pgEx)
                        {
                            errorMessage += $" | PostgreSQL Error: {pgEx.Message} (Code: {pgEx.SqlState})";
                        }
                    }

                    ModelState.AddModelError("", $"Ошибка при создании товара: {errorMessage}");

                    model.Categories = await _context.Categories
                        .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                        .ToListAsync();
                    model.TypeProducts = await _context.TypeProducts
                        .Select(t => new SelectListItem { Value = t.TypeProductId.ToString(), Text = t.Name })
                        .ToListAsync();

                    result = View(model);
                }
            });

            return result;
        }

        // GET: Admin/Product/Edit/5
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.RentalPrices)
                .Include(p => p.Inventories)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            var rentalPrice = product.RentalPrices.FirstOrDefault();
            var inventory = product.Inventories.FirstOrDefault();

            var model = new ProductViewModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Sku = product.Sku,
                Description = product.Description,
                ShortDescription = product.ShortDescription,
                CategoryId = product.CategoryId,
                TypeProductId = product.TypeProductId,
                PricePerDay = rentalPrice?.PricePerDay ?? 0,
                Deposit = rentalPrice?.Deposit ?? 0,
                MinRentalDays = rentalPrice?.MinRentalDays ?? 0.5m,
                QuantityForRent = inventory?.QuantityForRent ?? 0,
                QuantityForSale = inventory?.QuantityForSale ?? 0,
                LowStockThreshold = inventory?.LowStockThreshold ?? 3,
                IsActive = product.IsActive ?? true,
                ProductImages = product.ProductImages.ToList(),
                Categories = await _context.Categories
                    .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                    .ToListAsync(),
                TypeProducts = await _context.TypeProducts
                    .Select(t => new SelectListItem { Value = t.TypeProductId.ToString(), Text = t.Name })
                    .ToListAsync()
            };

            return View(model);
        }

        // POST: Admin/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, ProductViewModel model)
        {
            ModelState.Remove("Images");

            if (id != model.ProductId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.Categories = await _context.Categories
                    .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                    .ToListAsync();
                model.TypeProducts = await _context.TypeProducts
                    .Select(t => new SelectListItem { Value = t.TypeProductId.ToString(), Text = t.Name })
                    .ToListAsync();
                return View(model);
            }


            var executionStrategy = _context.Database.CreateExecutionStrategy();
            IActionResult result = View(model); // По умолчанию возвращаем view с ошибкой

            await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var product = await _context.Products
                        .Include(p => p.RentalPrices)
                        .Include(p => p.Inventories)
                        .Include(p => p.ProductImages)
                        .FirstOrDefaultAsync(p => p.ProductId == id);

                    if (product == null)
                    {
                        throw new Exception("Товар не найден");
                    }

                    // Генерируем новый slug если название изменилось
                    if (product.Name != model.Name)
                    {
                        string newSlug = GenerateSlug(model.Name);
                        product.Slug = await GetUniqueSlug(newSlug);
                    }

                    // 1. Обновляем продукт
                    product.Name = model.Name;
                    product.Sku = model.Sku;
                    product.Description = model.Description;
                    product.ShortDescription = model.ShortDescription;
                    product.CategoryId = model.CategoryId;
                    product.TypeProductId = model.TypeProductId;
                    product.IsActive = model.IsActive;
                    product.UpdatedAt = DateTime.Now;

                    // 2. Обновляем цены аренды
                    var rentalPrice = product.RentalPrices.FirstOrDefault();
                    if (rentalPrice != null)
                    {
                        rentalPrice.PricePerDay = model.PricePerDay;
                        rentalPrice.Deposit = model.Deposit;
                        rentalPrice.MinRentalDays = model.MinRentalDays;
                        rentalPrice.UpdatedAt = DateTime.Now;
                    }

                    // 3. Обновляем инвентарь
                    var inventory = product.Inventories.FirstOrDefault();
                    if (inventory != null)
                    {
                        inventory.QuantityForRent = model.QuantityForRent;
                        inventory.QuantityForSale = model.QuantityForSale;
                        inventory.LowStockThreshold = model.LowStockThreshold;
                        inventory.UpdatedAt = DateTime.Now;
                    }

                    // 4. Обрабатываем изображения
                    if (model.Images != null && model.Images.Any(img => img != null && img.Length > 0))
                    {
                        var uploadsPath = Path.Combine(_environment.WebRootPath, "photoEquipment");
                        if (!Directory.Exists(uploadsPath))
                        {
                            Directory.CreateDirectory(uploadsPath);
                        }

                        int order = 0;
                        foreach (var imageFile in model.Images.Where(img => img != null && img.Length > 0))
                        {
                            try
                            {
                                // Проверка типа файла
                                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                                var extension = Path.GetExtension(imageFile.FileName)?.ToLower();

                                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                                {
                                    continue; // Пропускаем недопустимые файлы
                                }

                                // Генерируем уникальное имя файла
                                var fileName = $"{Guid.NewGuid()}{extension}";
                                var filePath = Path.Combine(uploadsPath, fileName);

                                // Сохраняем файл
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await imageFile.CopyToAsync(stream);
                                }

                                // Создаем запись в БД
                                var productImage = new ProductImage
                                {
                                    ProductId = product.ProductId,
                                    Path = fileName,
                                    Order = order,
                                    IsMain = (order == 0) // Первое изображение - главное
                                };

                                _context.ProductImages.Add(productImage);
                                order++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Ошибка при обработке изображения {FileName}", imageFile?.FileName);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Нет изображений для обработки или они пустые");
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Устанавливаем результат для возврата
                    TempData["SuccessMessage"] = "Товар успешно обновлен";
                    result = RedirectToAction(nameof(Products));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Ошибка при обновлении товара");

                    ModelState.AddModelError("", $"Ошибка при обновлении товара: {ex.Message}");

                    model.Categories = await _context.Categories
                        .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                        .ToListAsync();
                    model.TypeProducts = await _context.TypeProducts
                        .Select(t => new SelectListItem { Value = t.TypeProductId.ToString(), Text = t.Name })
                        .ToListAsync();

                    result = View(model);
                }
            });

            return result;
        }

        // POST: Admin/Product/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.RentalPrices)
                .Include(p => p.Inventories)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            try
            {
                // Удаляем изображения с диска
                foreach (var image in product.ProductImages)
                {
                    var imagePath = Path.Combine(_environment.WebRootPath, "photoEquipment", image.Path);
                    try
                    {
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }
                    catch (IOException ex)
                    {
                        _logger.LogWarning($"Файл занят другим процессом: {ex.Message}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogWarning($"Нет прав доступа к файлу: {ex.Message}");
                    }
                }

                // Удаляем связанные записи
                _context.RentalPrices.RemoveRange(product.RentalPrices);
                _context.Inventories.RemoveRange(product.Inventories);
                _context.ProductImages.RemoveRange(product.ProductImages);

                // Удаляем сам продукт
                _context.Products.Remove(product);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Товар успешно удален";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при удалении товара: {ex.Message}";
                _logger.LogError(ex, "Ошибка при удалении товара {ProductId}", id);
            }

            return RedirectToAction(nameof(Products));
        }

        // POST: Admin/Product/DeleteImage/5
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int id)
        {
            try
            {
                var image = await _context.ProductImages.FindAsync(id);
                if (image == null)
                {
                    return Json(new { success = false, message = "Изображение не найдено" });
                }

                // Полный путь к файлу изображения
                var imagePath = Path.Combine(_environment.WebRootPath, "photoEquipment", image.Path);

                // Удаляем файл с диска
                try
                {
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                        _logger.LogInformation("Файл изображения удален: {ImagePath}", imagePath);
                    }
                    else
                    {
                        _logger.LogWarning("Файл изображения не найден: {ImagePath}", imagePath);
                    }
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "Не удалось удалить файл изображения (файл занят): {ImagePath}", imagePath);
                    return Json(new { success = false, message = "Файл занят другим процессом" });
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning(ex, "Нет прав доступа для удаления файла: {ImagePath}", imagePath);
                    return Json(new { success = false, message = "Нет прав доступа к файлу" });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка при удалении файла: {ImagePath}", imagePath);
                    return Json(new { success = false, message = $"Ошибка при удалении файла: {ex.Message}" });
                }

                // Проверяем, было ли это главное изображение
                bool wasMainImage = image.IsMain ?? false;
                int productId = image.ProductId ?? 0;

                // Удаляем запись из БД
                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();

                // Если удалили главное изображение, устанавливаем новое главное
                if (wasMainImage && productId > 0)
                {
                    var remainingImages = await _context.ProductImages
                        .Where(img => img.ProductId == productId)
                        .OrderBy(img => img.Order)
                        .ToListAsync();

                    if (remainingImages.Any())
                    {
                        var newMainImage = remainingImages.First();
                        newMainImage.IsMain = true;
                        await _context.SaveChangesAsync();
                    }
                }

                return Json(new { success = true, message = "Изображение успешно удалено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении изображения {ImageId}", id);
                return Json(new { success = false, message = $"Ошибка при удалении изображения: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetMainImage(int productId, int imageId)
        {
            try
            {
                // Находим изображение
                var image = await _context.ProductImages
                    .FirstOrDefaultAsync(img => img.Id == imageId && img.ProductId == productId);

                if (image == null)
                {
                    return Json(new { success = false, message = "Изображение не найдено" });
                }

                // Сначала сбрасываем все флаги главного изображения для этого продукта
                var productImages = await _context.ProductImages
                    .Where(img => img.ProductId == productId)
                    .ToListAsync();

                foreach (var img in productImages)
                {
                    img.IsMain = false;
                }

                // Устанавливаем новое главное изображение
                image.IsMain = true;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Главное изображение установлено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при установке главного изображения для продукта {ProductId}", productId);
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReorderImages(int productId, int[] imageIds)
        {
            try
            {
                var images = await _context.ProductImages
                    .Where(img => img.ProductId == productId)
                    .ToListAsync();

                // Обновляем порядок изображений
                for (int i = 0; i < imageIds.Length; i++)
                {
                    var image = images.FirstOrDefault(img => img.Id == imageIds[i]);
                    if (image != null)
                    {
                        image.Order = i;
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Порядок изображений обновлен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при изменении порядка изображений");
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        // Метод для предпросмотра изображения
        public async Task<IActionResult> GetImagePreview(int id)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image == null || string.IsNullOrEmpty(image.Path))
            {
                return NotFound();
            }

            // Защита от path traversal attacks
            var safeFileName = Path.GetFileName(image.Path);
            if (string.IsNullOrEmpty(safeFileName))
            {
                return NotFound();
            }

            var imagePath = Path.Combine(_environment.WebRootPath, "photoEquipment", safeFileName);

            if (!System.IO.File.Exists(imagePath))
            {
                return NotFound();
            }

            // Определяем MIME type по расширению файла
            var extension = Path.GetExtension(imagePath).ToLower();
            var mimeType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            var imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
            return File(imageBytes, mimeType);
        }




        

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TypeProducts()
        {
            var typeProducts = await _context.TypeProducts
                .OrderBy(t => t.Name)
                .ToListAsync();

            return View(typeProducts);
        }

        // GET: Admin/Category/Create
        [Authorize(Roles = "Admin")]
        public IActionResult CreateCategory()
        {
            return View();
        }

        // POST: Admin/Category/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Генерируем slug
                string slug = GenerateSlug(model.Name);
                string uniqueSlug = await GetUniqueCategorySlug(slug);

                var category = new Category
                {
                    Name = model.Name,
                    Slug = uniqueSlug,
                    Description = model.Description,
                    Icon = model.Icon,
                    IsForRent = model.IsForRent,
                    IsForSale = model.IsForSale,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Категория успешно создана";
                return RedirectToAction(nameof(Categories));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании категории");
                ModelState.AddModelError("", $"Ошибка при создании категории: {ex.Message}");
                return View(model);
            }
        }

        // GET: Admin/TypeProduct/Create
        [Authorize(Roles = "Admin")]
        public IActionResult CreateTypeProduct()
        {
            return View();
        }

        // POST: Admin/TypeProduct/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTypeProduct(TypeProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Генерируем slug
                string slug = GenerateSlug(model.Name);
                string uniqueSlug = await GetUniqueTypeProductSlug(slug);

                var typeProduct = new TypeProduct
                {
                    Name = model.Name,
                    Slug = uniqueSlug,
                    Description = model.Description,
                    Icon = model.Icon,
                    IsForRent = model.IsForRent,
                    IsForSale = model.IsForSale,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.TypeProducts.Add(typeProduct);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Тип оборудования успешно создан";
                return RedirectToAction(nameof(TypeProducts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании типа оборудования");
                ModelState.AddModelError("", $"Ошибка при создании типа оборудования: {ex.Message}");
                return View(model);
            }
        }

        // GET: Admin/Category/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var model = new CategoryViewModel
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = category.Description,
                Icon = category.Icon,
                IsForRent = category.IsForRent ?? true,
                IsForSale = category.IsForSale ?? false
            };

            return View(model);
        }

        // POST: Admin/Category/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, CategoryViewModel model)
        {
            if (id != model.CategoryId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound();
                }

                // Обновляем slug если название изменилось
                if (category.Name != model.Name)
                {
                    string newSlug = GenerateSlug(model.Name);
                    category.Slug = await GetUniqueCategorySlug(newSlug, id);
                }

                category.Name = model.Name;
                category.Description = model.Description;
                category.Icon = model.Icon;
                category.IsForRent = model.IsForRent;
                category.IsForSale = model.IsForSale;
                category.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Категория успешно обновлена";
                return RedirectToAction(nameof(Categories));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении категории");
                ModelState.AddModelError("", $"Ошибка при обновлении категории: {ex.Message}");
                return View(model);
            }
        }

        // GET: Admin/TypeProduct/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditTypeProduct(int id)
        {
            var typeProduct = await _context.TypeProducts.FindAsync(id);
            if (typeProduct == null)
            {
                return NotFound();
            }

            var model = new TypeProductViewModel
            {
                TypeProductId = typeProduct.TypeProductId,
                Name = typeProduct.Name,
                Description = typeProduct.Description,
                Icon = typeProduct.Icon,
                IsForRent = typeProduct.IsForRent ?? true,
                IsForSale = typeProduct.IsForSale ?? false
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTypeProduct(int id, TypeProductViewModel model)
        {
            if (id != model.TypeProductId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var typeProduct = await _context.TypeProducts.FindAsync(id);
                if (typeProduct == null)
                {
                    return NotFound();
                }

                // Обновляем slug если название изменилось
                if (typeProduct.Name != model.Name)
                {
                    string newSlug = GenerateSlug(model.Name);
                    typeProduct.Slug = await GetUniqueTypeProductSlug(newSlug, id);
                }

                typeProduct.Name = model.Name;
                typeProduct.Description = model.Description;
                typeProduct.Icon = model.Icon;
                typeProduct.IsForRent = model.IsForRent;
                typeProduct.IsForSale = model.IsForSale;
                typeProduct.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Тип оборудования успешно обновлен";
                return RedirectToAction(nameof(TypeProducts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении типа оборудования");
                ModelState.AddModelError("", $"Ошибка при обновлении типа оборудования: {ex.Message}");
                return View(model);
            }
        }

        // POST: Admin/Category/Delete/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CategoryId == id);

                if (category == null)
                {
                    return NotFound();
                }

                if (category.Products.Any())
                {
                    TempData["ErrorMessage"] = "Нельзя удалить категорию, так как с ней связаны товары";
                    return RedirectToAction(nameof(Categories));
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Категория успешно удалена";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении категории");
                TempData["ErrorMessage"] = $"Ошибка при удалении категории: {ex.Message}";
            }

            return RedirectToAction(nameof(Categories));
        }

        // POST: Admin/TypeProduct/Delete/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTypeProduct(int id)
        {
            try
            {
                var typeProduct = await _context.TypeProducts
                    .Include(t => t.Products)
                    .FirstOrDefaultAsync(t => t.TypeProductId == id);

                if (typeProduct == null)
                {
                    return NotFound();
                }

                if (typeProduct.Products.Any())
                {
                    TempData["ErrorMessage"] = "Нельзя удалить тип оборудования, так как с ним связаны товары";
                    return RedirectToAction(nameof(TypeProducts));
                }

                _context.TypeProducts.Remove(typeProduct);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Тип оборудования успешно удален";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении типа оборудования");
                TempData["ErrorMessage"] = $"Ошибка при удалении типа оборудования: {ex.Message}";
            }

            return RedirectToAction(nameof(TypeProducts));
        }


        // Вспомогательные методы для генерации уникальных slug
        private async Task<string> GetUniqueCategorySlug(string baseSlug, int? excludeId = null)
        {
            string slug = baseSlug;
            int counter = 1;

            var query = _context.Categories.AsQueryable();
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.CategoryId != excludeId.Value);
            }

            while (await query.AnyAsync(c => c.Slug == slug))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }

        private async Task<string> GetUniqueTypeProductSlug(string baseSlug, int? excludeId = null)
        {
            string slug = baseSlug;
            int counter = 1;

            var query = _context.TypeProducts.AsQueryable();
            if (excludeId.HasValue)
            {
                query = query.Where(t => t.TypeProductId != excludeId.Value);
            }

            while (await query.AnyAsync(t => t.Slug == slug))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }



        // Метод для генерации slug из русского текста
        private string GenerateSlug(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            // Словарь для транслитерации русских букв
            var translitDict = new Dictionary<char, string>
    {
        {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "g"}, {'д', "d"}, {'е', "e"}, {'ё', "yo"},
        {'ж', "zh"}, {'з', "z"}, {'и', "i"}, {'й', "y"}, {'к', "k"}, {'л', "l"}, {'м', "m"},
        {'н', "n"}, {'о', "o"}, {'п', "p"}, {'р', "r"}, {'с', "s"}, {'т', "t"}, {'у', "u"},
        {'ф', "f"}, {'х', "kh"}, {'ц', "ts"}, {'ч', "ch"}, {'ш', "sh"}, {'щ', "sch"}, {'ъ', ""},
        {'ы', "y"}, {'ь', ""}, {'э', "e"}, {'ю', "yu"}, {'я', "ya"},
        {'А', "A"}, {'Б', "B"}, {'В', "V"}, {'Г', "G"}, {'Д', "D"}, {'Е', "E"}, {'Ё', "Yo"},
        {'Ж', "Zh"}, {'З', "Z"}, {'И', "I"}, {'Й', "Y"}, {'К', "K"}, {'Л', "L"}, {'М', "M"},
        {'Н', "N"}, {'О', "O"}, {'П', "P"}, {'Р', "R"}, {'С', "S"}, {'Т', "T"}, {'У', "U"},
        {'Ф', "F"}, {'Х', "Kh"}, {'Ц', "Ts"}, {'Ч', "Ch"}, {'Ш', "Sh"}, {'Щ', "Sch"}, {'Ъ', ""},
        {'Ы', "Y"}, {'Ь', ""}, {'Э', "E"}, {'Ю', "Yu"}, {'Я', "Ya"}
    };

            // Транслитерируем русские буквы
            var result = new StringBuilder();
            foreach (char c in name)
            {
                if (translitDict.TryGetValue(c, out string translit))
                {
                    result.Append(translit);
                }
                else if (char.IsLetterOrDigit(c))
                {
                    result.Append(c);
                }
                else if (c == ' ')
                {
                    result.Append(' ');
                }
            }

            string slug = result.ToString();

            // Убираем пробелы и делаем одну строку
            slug = slug.Replace(" ", "");

            // Убираем все не-ASCII символы, оставляем только буквы, цифры и дефисы
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-zA-Z0-9\-]", "");

            // Если после обработки slug пустой, используем GUID
            if (string.IsNullOrEmpty(slug))
            {
                slug = Guid.NewGuid().ToString("N").Substring(0, 8);
            }

            return slug.ToLower(); // Приводим к нижнему регистру
        }

        // Метод для получения уникального slug
        private async Task<string> GetUniqueSlug(string baseSlug)
        {
            string slug = baseSlug;
            int counter = 1;

            // Проверяем, существует ли уже такой slug
            while (await _context.Products.AnyAsync(p => p.Slug == slug))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }


    }













    // Добавим ViewModel классы в конец файла
    public class UserDetailViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string PassportSeria { get; set; }
        public string PassportNumber { get; set; }
        public string Propiska { get; set; }
        public string PlaceLive { get; set; }
        public string PassportStatus { get; set; }
        public int TotalOrders { get; set; }
        public int ActiveOrders { get; set; }
        public IList<string> Roles { get; set; }
    }

    public class UserUpdateViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        public string PhoneNumber { get; set; }

        public bool IsActive { get; set; }

        // Сделать поля необязательными
        public string? PassportSeria { get; set; }
        public string? PassportNumber { get; set; }
        public string? Propiska { get; set; }
        public string? PlaceLive { get; set; }

        // Сделать роли необязательными
        public List<string>? SelectedRoles { get; set; }
    }


    public class AdminProfileUpdateModel
    {
        [Required(ErrorMessage = "Обязательное поле")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Phone(ErrorMessage = "Некорректный номер телефона")]
        public string Phone { get; set; }

        public string CurrentPassword { get; set; }

        [StringLength(100, ErrorMessage = "Пароль должен содержать минимум {2} символов", MinimumLength = 6)]
        public string NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; }

        // Дополнительные поля для администратора
        public string PassportSeria { get; set; }
        public string PassportNumber { get; set; }
        public string Propiska { get; set; }
        public string PlaceLive { get; set; }
    }


}
