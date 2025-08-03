using Microsoft.EntityFrameworkCore;

namespace StoriArendaPro.Models
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Добавляем сервисы в контейнер.
            builder.Services.AddControllersWithViews();

            // Настройка DbContext
            builder.Services.AddDbContext<StoriArendaProContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSql")));

            //Порт: Render использует переменную $PORT
            //builder.WebHost.UseUrls("http://0.0.0.0:" + Environment.GetEnvironmentVariable("PORT") ?? "5000");

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
