using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StoriArendaPro.Models;
using StoriArendaPro.Models.Entities;
using StoriArendaPro.Services;
using CodePackage.YooKassa; // Добавьте эту директиву

namespace StoriArendaPro
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Добавляем сервисы в контейнер.
            builder.Services.AddControllersWithViews();


            // Конфигурация YooKassa
            builder.Services.Configure<YooKassaSettings>(
                builder.Configuration.GetSection("YooKassa"));

            builder.Services.AddScoped<IYooKassaClient, YooKassaClient>(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<YooKassaSettings>>().Value;
                return new YooKassaClient(settings.ShopId, settings.SecretKey);
            });

            // Настройка DbContext для PostgreSQL
            builder.Services.AddDbContext<StoriArendaProContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Добавляем конфигурацию SMTP
            builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

            // сервис email
            builder.Services.AddScoped<IEmailService, EmailSmsService>();

            // Добавляем HttpClient
            builder.Services.AddHttpClient();

            // Добавьте сервисы Identity ПРАВИЛЬНО
            var identityBuilder = builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
            {
                // Упростите требования к паролю для тестирования
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 1;

                options.User.RequireUniqueEmail = false; // временно отключите
                options.SignIn.RequireConfirmedEmail = false;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            });

            // ПОТОМ добавляем Entity Framework stores
            identityBuilder.AddEntityFrameworkStores<StoriArendaProContext>();
            identityBuilder.AddDefaultTokenProviders();

            // Настройка Cookie для Identity
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.AccessDeniedPath = "/Auth/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.HttpOnly = true;
                options.Cookie.Name = "StoriArendaPro.Auth";
            });

            builder.Services.AddSession(options =>
            {
                options.Cookie.Name = "StoriArendaPro.Session";
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Services.AddLogging();

            var app = builder.Build();

            // Инициализация ролей и администратора
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                try
                {
                    var context = services.GetRequiredService<StoriArendaProContext>();
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
                    var userManager = services.GetRequiredService<UserManager<User>>();

                    // Проверка подключения к БД
                    if (!await context.Database.CanConnectAsync())
                    {
                        Console.WriteLine("Нет подключения к БД");
                        return;
                    }

                    // Применяем миграции
                    await context.Database.MigrateAsync();

                    // Создаем роли если их нет
                    string[] roleNames = { "Admin", "User" };

                    foreach (var roleName in roleNames)
                    {
                        if (!await roleManager.RoleExistsAsync(roleName))
                        {
                            await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                            Console.WriteLine($"Создана роль: {roleName}");
                        }
                    }

                    // Создаем администратора по умолчанию
                    var adminEmail = "vatazhishen06@bk.ru";
                    var adminUser = await userManager.FindByEmailAsync(adminEmail);

                    if (adminUser == null)
                    {
                        adminUser = new User
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            PhoneNumber = "79634447037",
                            FullName = "Администратор",
                            IsAdmin = true,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            EmailConfirmed = true,
                            PhoneNumberConfirmed = true,
                            SecurityStamp = Guid.NewGuid().ToString()
                        };

                        // Сначала создаем пользователя
                        var createResult = await userManager.CreateAsync(adminUser);

                        if (createResult.Succeeded)
                        {
                            // Затем устанавливаем пароль
                            var passwordResult = await userManager.AddPasswordAsync(adminUser, "12873465Tam!");

                            if (passwordResult.Succeeded)
                            {
                                await userManager.AddToRoleAsync(adminUser, "Admin");
                                Console.WriteLine("Администратор создан успешно");
                            }
                            else
                            {
                                Console.WriteLine($"Ошибка пароля: {string.Join(", ", passwordResult.Errors.Select(e => e.Description))}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Ошибки создания: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Администратор уже существует");

                        // Убедимся что у пользователя есть роль Admin
                        var roles = await userManager.GetRolesAsync(adminUser);
                        if (!roles.Contains("Admin"))
                        {
                            await userManager.AddToRoleAsync(adminUser, "Admin");
                            Console.WriteLine("Роль Admin добавлена существующему пользователю");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка инициализации: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");

                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                        Console.WriteLine($"Inner StackTrace: {ex.InnerException.StackTrace}");
                    }

                    // Добавьте паузу для чтения ошибок в консоли
                    Console.WriteLine("Нажмите любую клавишу для продолжения...");
                    Console.ReadKey();
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.Lax,
                Secure = CookieSecurePolicy.Always
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors("AllowAll");
            app.UseSession();
            app.UseAuthentication(); // ДОЛЖНО БЫТЬ ПЕРЕД UseAuthorization
            app.UseAuthorization();

            // ПРАВИЛЬНЫЙ ПОРЯДОК МАРШРУТОВ:

            // 1. Сначала маршрут для области Admin
            app.MapAreaControllerRoute(
                name: "admin",
                areaName: "Admin",
                pattern: "Admin/{controller=Admin}/{action=Index}/{id?}");

            // 2. Общий маршрут для всех областей
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            // 3. Маршрут по умолчанию (для основной области)
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "profile",
                pattern: "Profile/{action=Index}/{id?}",
                defaults: new { controller = "Profile" });

            await app.RunAsync();
        }
    }
}