using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace DigitalMe.Data.Seeders
{
    public class IdentityDataSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetService<ILogger<IdentityDataSeeder>>();

            try
            {
                // Create roles
                string[] roles = { "Admin", "User" };
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                        logger?.LogInformation($"Created role: {role}");
                    }
                }

                // Create demo user
                var demoEmail = "demo@digitalme.ai";
                var demoUser = await userManager.FindByEmailAsync(demoEmail);
                if (demoUser == null)
                {
                    demoUser = new IdentityUser
                    {
                        UserName = demoEmail,
                        Email = demoEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(demoUser, "Ivan2024!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(demoUser, "Admin");
                        await userManager.AddToRoleAsync(demoUser, "User");
                        logger?.LogInformation($"Created demo user: {demoEmail}");
                    }
                    else
                    {
                        logger?.LogError($"Failed to create demo user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }

                // Create mr.red.404@gmail.com user
                var mainEmail = "mr.red.404@gmail.com";
                var mainUser = await userManager.FindByEmailAsync(mainEmail);
                if (mainUser == null)
                {
                    mainUser = new IdentityUser
                    {
                        UserName = mainEmail,
                        Email = mainEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(mainUser, "Ivan2024!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(mainUser, "Admin");
                        await userManager.AddToRoleAsync(mainUser, "User");
                        logger?.LogInformation($"Created main user: {mainEmail}");
                    }
                    else
                    {
                        logger?.LogError($"Failed to create main user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }

                logger?.LogInformation("Identity data seeding completed successfully");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error seeding identity data");
                throw;
            }
        }
    }
}