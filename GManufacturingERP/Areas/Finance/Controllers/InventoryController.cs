using GManufacturingERP.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Finance.Controllers
{
    [Area("Finance")]
    [Authorize(Roles = "FinanceLogisticsManager")]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Finance/Inventory
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var stocks = await _context.InventoryStocks
                .OrderBy(x => x.MaterialName)
                .ToListAsync();

            return View(stocks);
        }

        // GET: Finance/Inventory/Details/5
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