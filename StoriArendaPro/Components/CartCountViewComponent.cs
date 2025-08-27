using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoriArendaPro;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace StoriArendaPro.Components
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly StoriArendaProContext _context;

        public CartCountViewComponent(StoriArendaProContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Content("0");
            }

            // Приводим User к ClaimsPrincipal, чтобы использовать FindFirstValue
            var claimsPrincipal = User as ClaimsPrincipal;
            if (claimsPrincipal == null)
            {
                return Content("0");
            }

            var userIdClaim = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Content("0");
            }

            var count = await _context.ShoppingCarts
                .Where(c => c.UserId == userId)
                .CountAsync();

            return Content(count.ToString());
        }
    }
}