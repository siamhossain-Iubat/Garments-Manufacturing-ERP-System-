using GManufacturingERP.Models.Entities;
using GManufacturingERP.ViewModels;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GManufacturingERP.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // =====================================================
        // LOGIN - GET
        // =====================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }

        // =====================================================
        // LOGIN - POST
        // =====================================================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            // User does not exist
            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");

                return View(model);
            }

            // Email verification check
            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please verify your email before logging in.");

                return View(model);
            }

            // Admin approval check
            if (!user.IsActive ||
                user.AccountStatus != "Approved")
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your account is waiting for Admin approval.");

                return View(model);
            }

            // Password login
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Redirect to requested local URL
                if (!string.IsNullOrEmpty(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Redirect according to role
                return await RedirectToRoleDashboard(user);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your account is temporarily locked. Please try again later.");

                return View(model);
            }

            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password.");

            return View(model);
        }

        // =====================================================
        // LOGOUT
        // =====================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Login",
                "Account");
        }

        // =====================================================
        // REGISTER - GET
        // =====================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // =====================================================
        // REGISTER - POST
        // =====================================================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check duplicate email
            var existingUser =
                await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "An account with this email already exists.");

                return View(model);
            }

            // Create new Buyer
            var buyer = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,

                FullName = model.FullName,
                CompanyName = model.CompanyName,
                Country = model.Country,
                CompanyAddress = model.CompanyAddress,

                UserType = "External",

                // Email verification completed হলেও
                // Admin approval না হওয়া পর্যন্ত Pending থাকবে
                AccountStatus = "PendingApproval",

                // New buyer initially inactive
                IsActive = false,

                // Email initially unverified
                EmailConfirmed = false,

                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(
                buyer,
                model.Password);

            if (result.Succeeded)
            {
                // Assign Buyer role
                var roleResult = await _userManager.AddToRoleAsync(
                    buyer,
                    "Buyer");

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

                // Generate email confirmation token
                var token = await _userManager
                    .GenerateEmailConfirmationTokenAsync(buyer);

                // Generate verification URL
                var confirmationLink = Url.Action(
                    "ConfirmEmail",
                    "Account",
                    new
                    {
                        userId = buyer.Id,
                        token = token
                    },
                    Request.Scheme);

                // Development purpose:
                // Later this link will be sent through email.
                TempData["ConfirmationLink"] = confirmationLink;

                return RedirectToAction(
                    nameof(RegistrationSubmitted));
            }

            // Display Identity validation errors
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }

        // =====================================================
        // REGISTRATION SUBMITTED
        // =====================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult RegistrationSubmitted()
        {
            ViewBag.ConfirmationLink =
                TempData["ConfirmationLink"] as string;

            return View();
        }

        // =====================================================
        // CONFIRM EMAIL
        // =====================================================

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(
            string userId,
            string token)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(token))
            {
                return View("EmailConfirmationError");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ConfirmEmailAsync(
                user,
                token);

            if (result.Succeeded)
            {
                return View("EmailConfirmed");
            }

            return View("EmailConfirmationError");
        }

        // =====================================================
        // ROLE-BASED DASHBOARD REDIRECT
        // =====================================================

        private async Task<IActionResult> RedirectToRoleDashboard(
            ApplicationUser user)
        {
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Admin" });
            }

            if (await _userManager.IsInRoleAsync(user, "SalesBuyerManager"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Sales" });
            }

            if (await _userManager.IsInRoleAsync(
                user,
                "OperationsManager"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Operations" });
            }

            if (await _userManager.IsInRoleAsync(
                user,
                "FinanceLogisticsManager"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Finance" });
            }

            if (await _userManager.IsInRoleAsync(user, "Buyer"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Buyer" });
            }

            return RedirectToAction(
                "Index",
                "Home");
        }
    }
}