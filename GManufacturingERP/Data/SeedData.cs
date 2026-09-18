using GManufacturingERP.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace GManufacturingERP.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(
            IServiceProvider serviceProvider)
        {
            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles =
            {
                "Admin",
                "SalesBuyerManager",
                "OperationsManager",
                "FinanceLogisticsManager",
                "Buyer"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }

            var adminEmail = "admin@gmanufacturing.com";
            var adminPassword = "Admin@12345";

            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,

                    FullName = "System Administrator",
                    UserType = "Internal",
                    AccountStatus = "Approved",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(
                    admin,
                    adminPassword);

                if (!createResult.Succeeded)
                {
                    throw new Exception(
                        string.Join(
                            "; ",
                            createResult.Errors.Select(x => x.Description)));
                }
            }
            else
            {
                // Existing Admin account-এর information ঠিক রাখা
                admin.EmailConfirmed = true;
                admin.IsActive = true;
                admin.UserType = "Internal";
                admin.AccountStatus = "Approved";

                await userManager.UpdateAsync(admin);

                // Existing password reset করা
                var resetToken = await userManager
                    .GeneratePasswordResetTokenAsync(admin);

                var resetResult = await userManager.ResetPasswordAsync(
                    admin,
                    resetToken,
                    adminPassword);

                if (!resetResult.Succeeded)
                {
                    throw new Exception(
                        string.Join(
                            "; ",
                            resetResult.Errors.Select(x => x.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}