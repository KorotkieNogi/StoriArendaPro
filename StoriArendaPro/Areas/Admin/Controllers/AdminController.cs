// Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using StoriArendaPro.Models.Entities;
using StoriArendaPro.Models.ViewModels;
using StoriArendaPro.Services;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
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

        public AdminController(StoriArendaProContext context, UserManager<User> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
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
