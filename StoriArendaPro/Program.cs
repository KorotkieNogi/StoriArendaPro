using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StoriArendaPro.Models;
using StoriArendaPro.Models.Entities;
using StoriArendaPro.Services;
using CodePackage.YooKassa;
using System.Runtime.InteropServices;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.Extensions.Hosting;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;

namespace StoriArendaPro
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            bool isRunningInDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

            if (isRunningInDocker)
            {
                Console.WriteLine("🚀 Запуск в Docker контейнере");
                Console.WriteLine("📦 Версия .NET: " + Environment.Version);
                Console.WriteLine("🖥️  ОС: " + RuntimeInformation.OSDescription);
            }
            else
            {
                Console.WriteLine("💻 Запуск в обычном режиме");
            }

            var builder = WebApplication.CreateBuilder(args);

            // Автоматический выбор строки подключения
            var connectionString = isRunningInDocker
                ? builder.Configuration.GetConnectionString("DockerConnection")
                : builder.Configuration.GetConnectionString("DefaultConnection");

            Console.WriteLine($"📡 Using connection string: {connectionString}");


            // Добавьте это перед build()
            builder.Services.Configure<StaticFileOptions>(options =>
            {
                options.ContentTypeProvider = new FileExtensionContentTypeProvider
                {
                    Mappings = { [".webmanifest"] = "application/manifest+json" }
                };
            });

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
                options.UseNpgsql(connectionString,  // ← ИСПОЛЬЗУЕМ ПЕРЕМЕННУЮ!
                    options => options.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null)));

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

                options.User.RequireUniqueEmail = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            });

            // Добавляем Entity Framework stores
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

            // Настройка IIS и Kestrel ДО builder.Build()
            builder.Services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
            });

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
            });

            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
            });

            var app = builder.Build();

            // Проверка и создание папки для изображений
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var photoEquipmentPath = Path.Combine(webRootPath, "photoEquipment");

            if (!Directory.Exists(photoEquipmentPath))
            {
                Directory.CreateDirectory(photoEquipmentPath);
                Console.WriteLine($"Создана папка: {photoEquipmentPath}");
            }

            // Проверка прав на запись
            try
            {
                var testFile = Path.Combine(photoEquipmentPath, "test_write.txt");
                await File.WriteAllTextAsync(testFile, "test");
                File.Delete(testFile);
                Console.WriteLine("Права на запись в папку подтверждены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ОШИБКА: Нет прав на запись в папку {photoEquipmentPath}");
                Console.WriteLine($"Ошибка: {ex.Message}");

                // Создаем папку с правами на запись
                try
                {
                    Directory.CreateDirectory(photoEquipmentPath);
                    // Даем права на запись
                    var directoryInfo = new DirectoryInfo(photoEquipmentPath);
                    directoryInfo.Attributes &= ~FileAttributes.ReadOnly;
                    Console.WriteLine("Папка создана с правами на запись");
                }
                catch (Exception createEx)
                {
                    Console.WriteLine($"Не удалось создать папку: {createEx.Message}");
                }
            }

            // Добавьте глобальную обработку исключений
            app.UseExceptionHandler("/Home/Error");
            app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

            // В Development режиме показывайте детали ошибок
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }



            // Применяем миграции с улучшенной логикой подключения
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<StoriArendaProContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                if (isRunningInDocker)
                {
                    Console.WriteLine("⏳ Ожидание запуска PostgreSQL...");

                    // Улучшенная логика ожидания PostgreSQL
                    var maxRetries = 15;
                    var retryCount = 0;
                    var connected = false;

                    while (retryCount < maxRetries && !connected)
                    {
                        try
                        {
                            Console.WriteLine($"🔧 Попытка подключения {retryCount + 1}/{maxRetries}...");

                            if (await db.Database.CanConnectAsync())
                            {
                                connected = true;
                                Console.WriteLine("✅ Подключение к БД успешно");
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            retryCount++;
                            Console.WriteLine($"❌ Попытка {retryCount}/{maxRetries}: {ex.Message}");
                            if (retryCount < maxRetries)
                            {
                                Console.WriteLine("⏳ Повторная попытка через 5 секунд...");
                                await Task.Delay(5000);
                            }
                        }
                    }

                    if (!connected)
                    {
                        Console.WriteLine("❌ Не удалось подключиться к БД после всех попыток");
                        logger.LogError("Не удалось подключиться к БД");
                        // Продолжаем работу, приложение может работать без БД
                    }
                }

                try
                {
                    // Проверяем подключение к БД
                    if (await db.Database.CanConnectAsync())
                    {
                        Console.WriteLine("✅ Подключение к БД успешно");

                        // Применяем миграции
                        await db.Database.MigrateAsync();
                        Console.WriteLine("✅ Миграции применены успешно");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка при применении миграций: {ex.Message}");
                    logger.LogError(ex, "Ошибка при применении миграций");
                }
            }

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

                        var createResult = await userManager.CreateAsync(adminUser);

                        if (createResult.Succeeded)
                        {
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
                }
            }

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.Lax,
                Secure = CookieSecurePolicy.Always
            });



            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Unhandled exception");
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("Internal Server Error");
                }
            });


            app.UseRouting();

            app.UseCors("AllowAll");
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            // Маршрутизация
            app.MapAreaControllerRoute(
                name: "admin",
                areaName: "Admin",
                pattern: "Admin/{controller=Admin}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "profile",
                pattern: "Profile/{action=Index}/{id?}",
                defaults: new { controller = "Profile" });

            app.Urls.Add("http://0.0.0.0:80");
            Console.WriteLine("🚀 Запуск Kestrel на http://0.0.0.0:80");
            await app.RunAsync();
        }
    }
}