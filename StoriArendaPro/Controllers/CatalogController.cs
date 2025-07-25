using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoriArendaPro.Models.Entities;

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
        public async Task<IActionResult> Rent(int? categoryId, bool? showCategories)
        {
            IQueryable<RentalPrice> products = _context.RentalPrices
                .Include(p => p.Product)
                .Include(p => p.Product.Category)
                .Include(p => p.Product.Inventories)
                .Where(p => (bool)p.Product.Category.IsForRent && (bool)p.Product.IsActive);

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.Product.CategoryId == categoryId);
            }

            var categories = await _context.Categories
                .Where(c => (bool)c.IsForRent)
                .ToListAsync();


            ViewBag.ShowCategories = showCategories ?? false; // По умолчанию показываем оборудование

            ViewBag.Categories = categories;
            ViewBag.CurrentCategory = categoryId;

            return View(await products.ToListAsync());
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
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}
