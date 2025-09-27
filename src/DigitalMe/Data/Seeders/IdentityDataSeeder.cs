using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Data.Seeders;

/// <summary>
/// Seeds demo IdentityUser accounts for authentication testing
/// </summary>
public static class IdentityDataSeeder
{
    /// <summary>
    /// Seeds demo Identity users for production auth testing
    /// </summary>
    public static async Task SeedDemoIdentityUsersAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("🔐 Starting Identity users seeding...");

        try
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Create basic roles if they don't exist
            await CreateRoleIfNotExistsAsync(roleManager, "Admin", logger);
            await CreateRoleIfNotExistsAsync(roleManager, "User", logger);

            // Seed demo@digitalme.ai user
            await CreateUserIfNotExistsAsync(
                userManager,
                logger,
                email: "demo@digitalme.ai",
                password: "Ivan2024!",
                roles: new[] { "Admin", "User" });


            logger.LogInformation("✅ Identity users seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error during Identity users seeding");
            throw;
        }
    }

    private static async Task CreateRoleIfNotExistsAsync(RoleManager<IdentityRole> roleManager, string roleName, ILogger logger)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var role = new IdentityRole(roleName);
            var result = await roleManager.CreateAsync(role);

            if (result.Succeeded)
            {
                logger.LogInformation("✅ Created role: {RoleName}", roleName);
            }
            else
            {
                logger.LogError("❌ Failed to create role {RoleName}: {Errors}", roleName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogInformation("ℹ️ Role {RoleName} already exists", roleName);
        }
    }

    private static async Task CreateUserIfNotExistsAsync(
        UserManager<IdentityUser> userManager,
        ILogger logger,
        string email,
        string password,
        string[]? roles = null)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            logger.LogInformation("ℹ️ User {Email} already exists", email);
            return;
        }

        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true // Skip email confirmation for demo users
        };

        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            logger.LogInformation("✅ Created user: {Email}", email);

            // Add roles if specified
            if (roles != null && roles.Length > 0)
            {
                var roleResult = await userManager.AddToRolesAsync(user, roles);
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("✅ Added roles {Roles} to user {Email}",
                        string.Join(", ", roles), email);
                }
                else
                {
                    logger.LogWarning("⚠️ Failed to add roles to user {Email}: {Errors}", email,
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }
        else
        {
            logger.LogError("❌ Failed to create user {Email}: {Errors}", email,
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}