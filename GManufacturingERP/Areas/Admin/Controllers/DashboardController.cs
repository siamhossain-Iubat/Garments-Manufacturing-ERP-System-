using GManufacturingERP.Data;
using GManufacturingERP.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(DateTime? selectedDate)
        {
            // Bangladesh Time Zone
            TimeZoneInfo bangladeshTimeZone;

            try
            {
                // Windows timezone ID
                bangladeshTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "Bangladesh Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                // Linux timezone ID
                bangladeshTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "Asia/Dhaka");
            }

            // Current Bangladesh date
            var todayBangladesh =
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    bangladeshTimeZone).Date;

            // Selected date অথবা আজকের date
            var selectedLocalDate =
                selectedDate?.Date ?? todayBangladesh;

            // Bangladesh local date range
            var localStart = DateTime.SpecifyKind(
                selectedLocalDate,
                DateTimeKind.Unspecified);

            var localEnd = DateTime.SpecifyKind(
                selectedLocalDate.AddDays(1),
                DateTimeKind.Unspecified);

            // Bangladesh time থেকে UTC conversion
            var utcStart = TimeZoneInfo.ConvertTimeToUtc(
                localStart,
                bangladeshTimeZone);

            var utcEnd = TimeZoneInfo.ConvertTimeToUtc(
                localEnd,
                bangladeshTimeZone);


            // =====================================================
            // 1. Total Users
            // =====================================================

            var totalUsers = await _userManager.Users
                .CountAsync(x =>
                    x.CreatedAt >= utcStart &&
                    x.CreatedAt < utcEnd);


            // =====================================================
            // 2. Total Buyers
            // =====================================================

            var totalBuyers = await _context.Users
                .CountAsync(x =>
                    x.UserType == "External" &&
                    x.CreatedAt >= utcStart &&
                    x.CreatedAt < utcEnd);


            // =====================================================
            // 3. Pending Buyers
            // =====================================================

            var pendingBuyers = await _context.Users
                .CountAsync(x =>
                    x.UserType == "External" &&
                    x.AccountStatus == "PendingApproval" &&
                    x.CreatedAt >= utcStart &&
                    x.CreatedAt < utcEnd);


            // =====================================================
            // 4. Active Buyers
            // =====================================================

            var activeBuyers = await _context.Users
                .CountAsync(x =>
                    x.UserType == "External" &&
                    x.AccountStatus == "Approved" &&
                    x.IsActive &&
                    x.CreatedAt >= utcStart &&
                    x.CreatedAt < utcEnd);


            // =====================================================
            // 5. Recent Buyer Registrations
            // =====================================================

            var recentBuyers = await _context.Users
                .Where(x =>
                    x.UserType == "External" &&
                    x.CreatedAt >= utcStart &&
                    x.CreatedAt < utcEnd)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync();


            // =====================================================
            // Send Data to View
            // =====================================================

            ViewBag.TotalUsers = totalUsers;

            ViewBag.TotalBuyers = totalBuyers;

            ViewBag.PendingBuyers = pendingBuyers;

            ViewBag.ActiveBuyers = activeBuyers;

            ViewBag.SelectedDate = selectedLocalDate;


            return View(recentBuyers);
        }
    }
}