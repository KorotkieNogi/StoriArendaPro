using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoriArendaPro.Models;
using StoriArendaPro.Models.Entities;

namespace StoriArendaPro.Controllers
{
    public class HomeController : Controller
    {
        private readonly StoriArendaProContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(StoriArendaProContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Получаем популярные товары для аренды
            var popularRentals = await _context.RentalPrices.AsNoTracking()
                .Include(p => p.Product.Category)
                .Include(p => p.Product).ThenInclude(p => p.ProductImages)
                .Where(p => (bool)p.Product.Category.IsForRent)
                .OrderByDescending(p => p.Product.Inventories.Sum(i => i.QuantityForRent))
                .Take(8)
                .ToListAsync();


            // Получаем категории для аренды
            var rentCategories = await _context.Categories
                .Where(c => (bool)c.IsForRent)
                .ToListAsync();

            // Получаем категории для аренды
            var rentType = await _context.TypeProducts
                .Where(c => (bool)c.IsForRent)
                .ToListAsync();

            ViewBag.PopularRentals = popularRentals;
            ViewBag.RentCategories = rentCategories;
            ViewBag.RentType = rentType;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
