// Controllers/AuthController.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoriArendaPro.Models.Entities;
using StoriArendaPro.Models.ViewModels;
using StoriArendaPro.Services;
using System.Security.Claims;
using System.Linq;
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace StoriArendaPro.Controllers
{
    public class AuthController : Controller
    {
        private readonly StoriArendaProContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthController(StoriArendaProContext context, IEmailService emailService,
                     ILogger<AuthController> logger, UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            var model = new LoginViewModel();
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        // GET: /Auth/Register
        public IActionResult Register()
        {
            var model = new RegisterViewModel { CurrentStep = 1 };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendVerificationCode([FromBody] VerificationRequest request)
        {
            try
            {
                // Нормализуем номер
                var phone = new string(request.Phone?.Where(char.IsDigit).ToArray());

                // Проверяем основные поля
                if (string.IsNullOrEmpty(request.FullName))
                {
                    return Json(new { success = false, message = "Введите ваше имя" });
                }
                if (string.IsNullOrEmpty(phone) || phone.Length < 11)
                {
                    return Json(new { success = false, message = "Введите корректный номер телефона" });
                }
                if (string.IsNullOrEmpty(request.Email))
                {
                    return Json(new { success = false, message = "Введите email" });
                }

                // Проверяем, существует ли пользователь с таким email или телефоном
                var existingUserByEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingUserByEmail != null)
                {
                    return Json(new { success = false, message = "Пользователь с таким email уже существует" });
                }

                var existingUserByPhone = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
                if (existingUserByPhone != null)
                {
                    return Json(new { success = false, message = "Пользователь с таким номером уже существует" });
                }

                // Генерируем код
                var code = new Random().Next(1000, 9999).ToString();

                // Сохраняем код и данные в сессии
                HttpContext.Session.SetString($"VerificationCode_{request.Email}", code);
                HttpContext.Session.SetString($"VerificationCodeExpiry_{request.Email}", DateTime.Now.AddMinutes(10).ToString("O"));
                HttpContext.Session.SetString($"RegisterData_{request.Email}_FullName", request.FullName);
                HttpContext.Session.SetString($"RegisterData_{request.Email}_Phone", phone);

                try
                {
                    // Отправляем email с кодом
                    var emailSubject = "Код подтверждения для СтройАренда+";
                    var emailBody = $@"
                        <h2>Код подтверждения</h2>
                        <p>Здравствуйте, {request.FullName}!</p>
                        <p>Ваш код подтверждения для регистрации в СтройАренда+:</p>
                        <h1 style='color: #007bff; font-size: 2em;'>{code}</h1>
                        <p>Код действителен в течение 10 минут.</p>
                        <p>Если вы не запрашивали этот код, проигнорируйте это письмо.</p>
                    ";

                    _logger.LogInformation("Попытка отправки email на: {Email}", request.Email);
                    await _emailService.SendEmailAsync(request.Email, emailSubject, emailBody);
                    _logger.LogInformation("Email успешно отправлен на: {Email}", request.Email);

                    return Json(new
                    {
                        success = true,
                        message = "Код отправлен на вашу почту!",
                        email = request.Email
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка отправки email на {Email}", request.Email);
                    return Json(new
                    {
                        success = false,
                        message = $"Не удалось отправить код: {ex.Message}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendVerificationCode");
                return Json(new { success = false, message = "Произошла ошибка. Попробуйте позже." });
            }
        }

        public class VerificationRequest
        {
            public string Phone { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
        }

        [HttpPost]
        public IActionResult VerifyCode(string email, string code)
        {
            var storedCode = HttpContext.Session.GetString($"VerificationCode_{email}");
            var expiryStr = HttpContext.Session.GetString($"VerificationCodeExpiry_{email}");

            if (string.IsNullOrEmpty(storedCode) || string.IsNullOrEmpty(expiryStr))
            {
                return Json(new { success = false, message = "Код не найден. Запросите новый." });
            }

            if (!DateTime.TryParse(expiryStr, out var expiry) || DateTime.Now > expiry)
            {
                return Json(new { success = false, message = "Срок действия кода истек. Запросите новый." });
            }

            if (storedCode != code)
            {
                return Json(new { success = false, message = "Неверный код подтверждения." });
            }

            // Помечаем код как верифицированный
            HttpContext.Session.SetString($"CodeVerified_{email}", "true");
            return Json(new { success = true, message = "Код подтвержден!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            _logger.LogInformation("Начало обработки регистрации для: {Email}", model.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Модель не валидна для: {Email}", model.Email);
                model.CurrentStep = 3;
                return View(model);
            }

            // Проверяем верификацию кода
            var isCodeVerified = HttpContext.Session.GetString($"CodeVerified_{model.Email}");
            if (isCodeVerified != "true")
            {
                _logger.LogWarning("Код не верифицирован для: {Email}", model.Email);
                ModelState.AddModelError("", "Сначала подтвердите код из email.");
                model.CurrentStep = 2;
                return View(model);
            }

            // Проверяем совпадение данных
            var savedFullName = HttpContext.Session.GetString($"RegisterData_{model.Email}_FullName");
            var savedPhone = HttpContext.Session.GetString($"RegisterData_{model.Email}_Phone");

            if (model.FullName != savedFullName || model.Phone != savedPhone)
            {
                _logger.LogWarning("Данные не совпадают для: {Email}. Ожидалось: {SavedFullName}/{SavedPhone}, Получено: {FullName}/{Phone}",
                    model.Email, savedFullName, savedPhone, model.FullName, model.Phone);
                ModelState.AddModelError("", "Данные были изменены. Начните регистрацию заново.");
                model.CurrentStep = 1;
                return View(model);
            }

            // Проверяем существование пользователя
            var userExistsByEmail = await _context.Users.AnyAsync(u => u.Email == model.Email);
            if (userExistsByEmail)
            {
                _logger.LogWarning("Пользователь с email уже существует: {Email}", model.Email);
                ModelState.AddModelError("", "Пользователь с таким email уже существует.");
                model.CurrentStep = 1;
                return View(model);
            }

            var userExistsByPhone = await _context.Users.AnyAsync(u => u.PhoneNumber == model.Phone);
            if (userExistsByPhone)
            {
                _logger.LogWarning("Пользователь с телефоном уже существует: {Phone}", model.Phone);
                ModelState.AddModelError("", "Пользователь с таким номером уже существует.");
                model.CurrentStep = 1;
                return View(model);
            }

            try
            {
                _logger.LogInformation("Создание пользователя: {Email}", model.Email);

                // Создаем пользователя
                var user = new User
                {
                    UserName = model.Email,
                    PhoneNumber = model.Phone,
                    FullName = model.FullName,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    IsAdmin = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                //_context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Пользователь создан успешно: {model.Email}, ID: {user.Id}",
                    model.Email, user.Id);

                // Очищаем сессию
                ClearRegistrationSession(model.Email);

                // Автоматический вход
                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Index", "Profile");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Ошибка базы данных при регистрации: {Email}", model.Email);
                ModelState.AddModelError("", "Ошибка при сохранении данных. Попробуйте позже.");
                model.CurrentStep = 1;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Неожиданная ошибка при регистрации: {Email}", model.Email);
                ModelState.AddModelError("", "Произошла непредвиденная ошибка. Попробуйте позже.");
                model.CurrentStep = 1;
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Admin", new { area = "Admin" });
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                ModelState.AddModelError("", "Неверный email или пароль.");
                return View(model);
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            //await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            //return RedirectToAction("Index", "Home");

            // Используем SignInManager для выхода
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        private void ClearRegistrationSession(string email)
        {
            var keysToRemove = new[]
            {
                $"VerificationCode_{email}",
                $"VerificationCodeExpiry_{email}",
                $"CodeVerified_{email}",
                $"RegisterData_{email}_FullName",
                $"RegisterData_{email}_Phone"
            };

            foreach (var key in keysToRemove)
            {
                HttpContext.Session.Remove(key);
            }
        }
    }
}