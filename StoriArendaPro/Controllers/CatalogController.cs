using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoriArendaPro.Models.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace StoriArendaPro.Controllers
{
    public class CatalogController : Controller
    {
        private readonly StoriArendaProContext _context;

        public CatalogController(StoriArendaProContext context)
        {
            _context = context;
        }

        // GET: Catalog/Rent
        public async Task<IActionResult> Rent(int? categoryId, int? typeId, string searchQuery, bool? showCategories, string activeTab)
        {
            IQueryable<RentalPrice> products = _context.RentalPrices
                .Include(p => p.Product)
                .Include(p => p.Product.Category)
                .Include(p => p.Product.TypeProduct)
                .Include(p => p.Product.Inventories)
                .Include(p => p.Product).ThenInclude(p => p.ProductImages)
                .Where(p => (bool)p.Product.Category.IsForRent && (bool)p.Product.IsActive);

            // Определяем, показывать ли категории или оборудование
            bool shouldShowCategories = showCategories ?? (categoryId == null && typeId == null && string.IsNullOrEmpty(searchQuery));

            // Применяем фильтры только если не показываем категории
            if (!shouldShowCategories)
            {
                if (categoryId.HasValue)
                {
                    products = products.Where(p => p.Product.CategoryId == categoryId);
                }

                if (typeId.HasValue)
                {
                    products = products.Where(p => p.Product.TypeProductId == typeId);
                }

                // Поиск по наименованию - должен применяться после всех фильтров
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    // Приводим к нижнему регистру для регистронезависимого поиска
                    searchQuery = searchQuery.ToLower();
                    products = products.Where(rp =>
                        rp.Product.Name.ToLower().Contains(searchQuery) ||
                        (rp.Product.ShortDescription != null && rp.Product.ShortDescription.ToLower().Contains(searchQuery)) ||
                        (rp.Product.Description != null && rp.Product.Description.ToLower().Contains(searchQuery)));
                }
            }

            var categories = await _context.Categories
                .Where(c => (bool)c.IsForRent)
                .ToListAsync();

            var typeKategory = await _context.TypeProducts
                .Where(c => (bool)c.IsForRent)
                .ToListAsync();

            ViewBag.ShowCategories = shouldShowCategories;
            ViewBag.Categories = categories;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CategoriesType = typeKategory;
            ViewBag.CurrentType = typeId;
            ViewBag.SearchQuery = searchQuery;
            ViewBag.ActiveTab = activeTab;

            return View(shouldShowCategories ? new List<RentalPrice>() : await products.ToListAsync());
        }

        // GET: Catalog/ProductDetails/5
        public async Task<IActionResult> ProductDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.RentalPrices
                .Include(p => p.Product.Category)
                .Include(p => p.Product.Inventories)
                .Include(p => p.Product).ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Проверяем паспортные данные для аутентифицированных пользователей
            if (User.Identity.IsAuthenticated && !User.IsInRole("Admin"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _context.Users.FindAsync(userId);

                ViewBag.HasPassportData = !string.IsNullOrEmpty(user.PassportSeria) &&
                                         !string.IsNullOrEmpty(user.PassportNumber) &&
                                         !string.IsNullOrEmpty(user.Propiska) &&
                                         !string.IsNullOrEmpty(user.PlaceLive);
            }

            return View(product);
        }

        // GET: Catalog/Rent

        public IActionResult Sale(int? categoryId, string sortBy, int? minPrice, int? maxPrice)
        {
            // Логика фильтрации и сортировки товаров для продажи
            var products = _context.SalePrices
                .Include(p => p.Product.Category)
                .Include(p => p.Product.Inventories)
                .Include(p => p.Product).ThenInclude(p => p.ProductImages)
                .Where(p => (bool)p.IsOnSale) // Только товары для продажи
                .AsQueryable();

            // Фильтрация по категории
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.Product.CategoryId == categoryId);
            }

            // Фильтрация по цене
            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice);
            }
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice);
            }

            // Сортировка
            products = sortBy switch
            {
                "price_asc" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                /*"popular" => products.OrderByDescending(p => p.Product.),*/ //Тут дописать количество оборудования
                _ => products.OrderBy(p => p.SalePriceId)
            };

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.CurrentCategory = categoryId;
            ViewBag.SortBy = sortBy;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(products.ToList());
        }

        public IActionResult CatalogChoice()
        {
            return View();
        }
    }
}
