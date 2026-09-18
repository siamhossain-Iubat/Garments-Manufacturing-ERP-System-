using GManufacturingERP.Models.Entities;
using GManufacturingERP.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // All internal users
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .Where(x => x.UserType == "Internal")
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var userList = new List<UserManagementViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userList.Add(new UserManagementViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    UserType = user.UserType,
                    AccountStatus = user.AccountStatus,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    Role = roles.FirstOrDefault() ?? "No Role"
                });
            }

            return View(userList);
        }

        // Create user - GET
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new AdminCreateUserViewModel();

            ViewBag.Roles = await GetInternalRolesAsync();

            return View(model);
        }

        // Create user - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AdminCreateUserViewModel model)
        {
            ViewBag.Roles = await GetInternalRolesAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager
                .FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "An account with this email already exists.");

                return View(model);
            }

            var selectedRole = model.SelectedRole;

            var validRoles = new[]
            {
                "Admin",
                "SalesBuyerManager",
                "OperationsManager",
                "FinanceLogisticsManager"
            };

            if (!validRoles.Contains(selectedRole))
            {
                ModelState.AddModelError(
                    "SelectedRole",
                    "Please select a valid role.");

                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FullName = model.FullName,

                UserType = "Internal",
                AccountStatus = "Approved",
                IsActive = true,

                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(
                user,
                model.Password);

            if (result.Succeeded)
            {
                var roleResult = await _userManager
                    .AddToRoleAsync(user, selectedRole);

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return View(model);
                }

                TempData["SuccessMessage"] =
                    "Internal user created successfully.";

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }

        // Activate / Deactivate user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null || user.UserType != "Internal")
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;

            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = user.IsActive
                ? "User activated successfully."
                : "User deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<string>> GetInternalRolesAsync()
        {
            var roles = new List<string>
            {
                "Admin",
                "SalesBuyerManager",
                "OperationsManager",
                "FinanceLogisticsManager"
            };

            var existingRoles = new List<string>();

            foreach (var role in roles)
            {
                if (await _roleManager.RoleExistsAsync(role))
                {
                    existingRoles.Add(role);
                }
            }

            return existingRoles;
        }
    }
}