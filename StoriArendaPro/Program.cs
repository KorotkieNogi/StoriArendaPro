using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StoriArendaPro.Models;
using StoriArendaPro.Models.Entities;
using StoriArendaPro.Services;
using CodePackage.YooKassa; // �������� ��� ���������

namespace StoriArendaPro
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ��������� ������� � ���������.
            builder.Services.AddControllersWithViews();


            // ������������ YooKassa
            builder.Services.Configure<YooKassaSettings>(
                builder.Configuration.GetSection("YooKassa"));

            builder.Services.AddScoped<IYooKassaClient, YooKassaClient>(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<YooKassaSettings>>().Value;
                return new YooKassaClient(settings.ShopId, settings.SecretKey);
            });

            // ��������� DbContext ��� PostgreSQL
            builder.Services.AddDbContext<StoriArendaProContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ��������� ������������ SMTP
            builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

            // ������ email
            builder.Services.AddScoped<IEmailService, EmailSmsService>();

            // ��������� HttpClient
            builder.Services.AddHttpClient();

            // �������� ������� Identity ���������
            var identityBuilder = builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
            {
                // ��������� ���������� � ������ ��� ������������
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 1;

                options.User.RequireUniqueEmail = false; // �������� ���������
                options.SignIn.RequireConfirmedEmail = false;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            });

            // ����� ��������� Entity Framework stores
            identityBuilder.AddEntityFrameworkStores<StoriArendaProContext>();
            identityBuilder.AddDefaultTokenProviders();

            // ��������� Cookie ��� Identity
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

            // ������������� ����� � ��������������
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                try
                {
                    var context = services.GetRequiredService<StoriArendaProContext>();
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
                    var userManager = services.GetRequiredService<UserManager<User>>();

                    // �������� ����������� � ��
                    if (!await context.Database.CanConnectAsync())
                    {
                        Console.WriteLine("��� ����������� � ��");
                        return;
                    }

                    // ��������� ��������
                    await context.Database.MigrateAsync();

                    // ������� ���� ���� �� ���
                    string[] roleNames = { "Admin", "User" };

                    foreach (var roleName in roleNames)
                    {
                        if (!await roleManager.RoleExistsAsync(roleName))
                        {
                            await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                            Console.WriteLine($"������� ����: {roleName}");
                        }
                    }

                    // ������� �������������� �� ���������
                    var adminEmail = "vatazhishen06@bk.ru";
                    var adminUser = await userManager.FindByEmailAsync(adminEmail);

                    if (adminUser == null)
                    {
                        adminUser = new User
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            PhoneNumber = "79634447037",
                            FullName = "�������������",
                            IsAdmin = true,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            EmailConfirmed = true,
                            PhoneNumberConfirmed = true,
                            SecurityStamp = Guid.NewGuid().ToString()
                        };

                        // ������� ������� ������������
                        var createResult = await userManager.CreateAsync(adminUser);

                        if (createResult.Succeeded)
                        {
                            // ����� ������������� ������
                            var passwordResult = await userManager.AddPasswordAsync(adminUser, "12873465Tam!");

                            if (passwordResult.Succeeded)
                            {
                                await userManager.AddToRoleAsync(adminUser, "Admin");
                                Console.WriteLine("������������� ������ �������");
                            }
                            else
                            {
                                Console.WriteLine($"������ ������: {string.Join(", ", passwordResult.Errors.Select(e => e.Description))}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"������ ��������: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("������������� ��� ����������");

                        // �������� ��� � ������������ ���� ���� Admin
                        var roles = await userManager.GetRolesAsync(adminUser);
                        if (!roles.Contains("Admin"))
                        {
                            await userManager.AddToRoleAsync(adminUser, "Admin");
                            Console.WriteLine("���� Admin ��������� ������������� ������������");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"������ �������������: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");

                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                        Console.WriteLine($"Inner StackTrace: {ex.InnerException.StackTrace}");
                    }

                    // �������� ����� ��� ������ ������ � �������
                    Console.WriteLine("������� ����� ������� ��� �����������...");
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
            app.UseAuthentication(); // ������ ���� ����� UseAuthorization
            app.UseAuthorization();

            // ���������� ������� ���������:

            // 1. ������� ������� ��� ������� Admin
            app.MapAreaControllerRoute(
                name: "admin",
                areaName: "Admin",
                pattern: "Admin/{controller=Admin}/{action=Index}/{id?}");

            // 2. ����� ������� ��� ���� ��������
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            // 3. ������� �� ��������� (��� �������� �������)
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