using GManufacturingERP.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Operations.Controllers
{
    [Area("Operations")]
    [Authorize(Roles = "OperationsManager")]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Operations can only view inventory stock.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var stocks = await _context.InventoryStocks
                .OrderBy(x => x.MaterialName)
                .ToListAsync();

            return View(stocks);
        }

        // Operations can view stock transaction history.
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var stock = await _context.InventoryStocks
                .Include(x => x.Transactions)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (stock == null)
            {
                return NotFound();
            }

            return View(stock);
        }
    }
}